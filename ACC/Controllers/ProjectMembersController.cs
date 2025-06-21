using ACC.Services;
using ACC.ViewModels.MemberVM;
using ACC.ViewModels.MemberVM.MemberVM;
using ACC.ViewModels.ProjectMembersVM;
using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer;
using DataLayer.Models;
using DataLayer.Models.ClassHelper;
using DataLayer.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace ACC.Controllers
{
    public class ProjectMembersController : Controller
    {
        private readonly IProjetcRepository _projectRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserRoleService _userRoleService;
        private readonly ICompanyRepository _companyRepository;

        public ProjectMembersController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            UserRoleService userRoleService,
            ICompanyRepository companyRepository,
            IProjetcRepository projectRepository,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _userRoleService = userRoleService;
            _companyRepository = companyRepository;
            _projectRepository = projectRepository;
        }


        public IActionResult Index(int ProjectId, int page = 1, string search = "", int pageSize = 5)
        {
            // Get all user IDs assigned to this project
            var userIdsInProject = _userRoleService.GetMembers(ProjectId);

            // Build the query
            var query = _userManager.Users
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                .Where(u => userIdsInProject.Contains(u.Id))
                .AsQueryable();

            // Apply search filter (by name or email)
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m =>
                    m.UserName.Contains(search) ||
                    m.Email.Contains(search));
            }

            // Total count after filtering
            var totalItems = query.Count();

            // Paginate results
            var pagedUsers = query
                .OrderBy(m => m.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Map to ViewModel
            var projectMembers = pagedUsers.Select(m => new ProjectMembersVM
            {
                Id = m.Id,
                Name = m.UserName,
                Email = m.Email,
                Status = m.Status,
                Company = m.Company?.Name ?? "No Company",
                AddedOn = m.AddedOn,
                Position = _userRoleService.GetPosition(m.Id, ProjectId)?.Name ?? "Unassigned",
                ProjectAccessLevel = _userRoleService.GetProjectAccessLevel(m.Id, ProjectId)?.Name ?? "None"
            }).ToList();

            // Pass data to View
            ViewBag.Members = _userManager.Users.ToList();
            ViewBag.ProAccessLevelsList = _userRoleService.AllProjectAccessLevels();
            ViewBag.PositionsList = _userRoleService.AllProjectPositions();
            ViewBag.ProjId = ProjectId;
            ViewBag.Id = ProjectId;

            // Pagination values
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.Page = page;
            ViewBag.Search = search;

            return View(projectMembers);
        }



        [HttpPost]
        [HasRoles(GlobalAccessLevels.AccountAdmin, ProjectAccessLevels.ProjectAdmin)]

        public async Task<IActionResult> InsertMember(InsertMemberVM memberFromReq , int ProjectId)
        {
           
            var memebr = _userManager.Users.Where(u=>u.UserName == memberFromReq.Name).FirstOrDefault();

            if (memebr != null)
            {

                var ProjAccessLevel = new ApplicationUserRole()
                {
                    ProjectId = ProjectId,
                    UserId = memebr.Id,
                    RoleId = memberFromReq.ProjectAccessLevelId,

                };
                _userRoleService.Insert(ProjAccessLevel);
                _userRoleService.Save();


                var position = new ApplicationUserRole()
                {
                    ProjectId = ProjectId,
                    UserId = memebr.Id,
                    RoleId = memberFromReq.PositionId,

                };
                _userRoleService.Insert(position);

                _userRoleService.Save();
                
            }
            return RedirectToAction("Index", new { ProjectId = ProjectId });

        }

        [HttpPost]
        [HasRoles(GlobalAccessLevels.AccountAdmin, ProjectAccessLevels.ProjectAdmin)]

        public async Task<IActionResult> Delete(string id, int ProjectId)
        {
            
            var projectMemberRelations = _userRoleService.GetAll().Where(i=>i.ProjectId == ProjectId && i.UserId==id).ToList();
            foreach(var relation in projectMemberRelations)
            {
                _userRoleService.Delete(relation);
            }
            _userRoleService.Save();
            
            return RedirectToAction("Index", new { ProjectId = ProjectId });
        }

        [HttpGet]
        [HasRoles(GlobalAccessLevels.AccountAdmin, ProjectAccessLevels.ProjectAdmin)]

        public IActionResult Details(string id , int ? ProjectId = null)
        {
         
            var member = _userManager.Users.FirstOrDefault(u => u.Id == id);
            if (member != null)
            {
                return Json(new
                {
                    userName = member.UserName,
                    email = member.Email,
                    status = member.Status,
                    company = member.Company?.Name,
                    role = _userRoleService.GetByUserId(id , ProjectId).Role.Name,
                    accessLevels = member.AccessLevel
                });
            }
            return Json(new { error = "Member not found" });
        }

        [HttpPost]
        [HasRoles(GlobalAccessLevels.AccountAdmin, ProjectAccessLevels.ProjectAdmin)]

        public async Task<IActionResult> Update(string id, int ProjectId , [FromBody] UpdateMemberVM member)
        {
            

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {

                var user = await _userManager.FindByIdAsync(id);
                user.CompanyId = member.companyId;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description);
                    return BadRequest(new { success = false, errors });
                }



                var ProjectAccessLevel = _userRoleService.GetAll().FirstOrDefault(i => i.UserId == id && i.ProjectId== ProjectId && i.Role.ProjectAccessLevel == true);
                if (ProjectAccessLevel != null)
                {
                    _userRoleService.Delete(ProjectAccessLevel);
                    _userRoleService.Save();
                }

                var UpdatedProjectAccessLevel = new ApplicationUserRole
                {
                    ProjectId = ProjectAccessLevel.ProjectId,
                    RoleId = member.projectAccessLevelID,
                    UserId = id
                };

                _userRoleService.Insert(UpdatedProjectAccessLevel);
                _userRoleService.Save();



                var Postion = _userRoleService.GetAll().FirstOrDefault(i => i.UserId == id && i.ProjectId == ProjectId && i.Role.ProjectPosition == true);
                if (Postion != null)
                {
                    _userRoleService.Delete(Postion);
                    _userRoleService.Save();
                }

                var UpdatedPosition = new ApplicationUserRole
                {
                    ProjectId = Postion.ProjectId,
                    RoleId = member.positionID,
                    UserId = id
                };

                _userRoleService.Insert(UpdatedPosition);
                _userRoleService.Save();


                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }

        }


        [HttpGet]
        [HasRoles(GlobalAccessLevels.AccountAdmin, ProjectAccessLevels.ProjectAdmin)]

        public IActionResult GetUpdatePartial(string id, int ProjectId)

        {

            if (ModelState.IsValid)
            {
                var member = _userManager.Users.FirstOrDefault(u => u.Id == id);
                if (member == null)
                {
                    return NotFound();
                }


                var insertMemberVM = new InsertMemberVM
                {
                    Email = member.Email,
                    CompanyId = member.CompanyId,
                    currentCompany = _companyRepository.GetById(member.CompanyId ?? 0)?.Name,
                    PositionId = _userRoleService.GetPosition(id , ProjectId).Id,
                    ProjectAccessLevelId = _userRoleService.GetProjectAccessLevel(id , ProjectId).Id,

                };



                ViewBag.Companies = new SelectList(_companyRepository.GetAll(), "Id", "Name");
                ViewBag.PositionsList = new SelectList(_userRoleService.AllProjectPositions(), "Id", "Name");
                ViewBag.ProjectAccessLevelsList = new SelectList(_userRoleService.AllProjectAccessLevels(), "Id", "Name");
                ViewBag.ProjId = ProjectId;

                return PartialView("PartialViews/_UpdateProjectMembersPartialView", insertMemberVM);
            }

          

            return PartialView("PartialViews/_UpdateProjectMembersPartialView", ModelState);
        }
    }
}
