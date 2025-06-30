using ACC.Services;
using ACC.ViewModels.MemberVM;
using ACC.ViewModels.MemberVM.MemberVM;
using ACC.ViewModels.ProjectMembersVM;
using BusinessLogic.Repository.RepositoryClasses;
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
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace ACC.Controllers
{
    public class ProjectMembersController : Controller
    {
        private readonly IProjetcRepository _projectRepository;
        private readonly IProjectActivityRepository projectActivityRepository;
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
            IProjectActivityRepository projectActivityRepository,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _userRoleService = userRoleService;
            _companyRepository = companyRepository;
            _projectRepository = projectRepository;
            this.projectActivityRepository = projectActivityRepository;
        }


        public IActionResult Index(int ProjectId, int page = 1, string search ="", int pageSize = 10)
        {

            var userIdsInProject = _userRoleService.GetMembers(ProjectId);

            var query = _userManager.Users
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                .Where(u => userIdsInProject.Contains(u.Id))
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m => m.UserName.ToLower().Contains(search.ToLower()));
              
            }

            var totalItems = query.Count();

            var pagedUsers = query
                .OrderBy(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var projectMembers = pagedUsers.Select(m => new ProjectMembersVM
            {
                Id = m.Id,
                FirstName = m.FirstName,
                LastName = m.LastName,
                MobilePhone = m.MobilePhone,
                Name = m.UserName,
                Email = m.UserName,
                Company = m.Company?.Name ?? "No Company",
                AddedOn = m.AddedOn,
                Position = _userRoleService.GetPosition(m.Id, ProjectId).Name,
                ProjectAccessLevel = _userRoleService.GetProjectAccessLevel(m.Id, ProjectId).Name,

            }).ToList();

            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.Page = page;
            ViewBag.Search = search;
            ViewBag.Members = _userManager.Users.ToList();
            ViewBag.ProAccessLevelsList = _userRoleService.AllProjectAccessLevels();
            ViewBag.PositionsList = _userRoleService.AllProjectPositions();
            ViewBag.ProjId = ProjectId;
            ViewBag.Id = ProjectId;

            return View(projectMembers);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasRoles(GlobalAccessLevels.AccountAdmin, projectIdRouteKey: "ProjectId", ProjectAccessLevels.ProjectAdmin)]


        public async Task<IActionResult> InsertMember(InserProjecttMembersVM memberFromReq, int ProjectId)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage).ToList();
                foreach (var err in errors)
                {

                    Console.WriteLine("❌ " + err);
                }
                ViewBag.Members = _userManager.Users.ToList();
                ViewBag.ProAccessLevelsList = _userRoleService.AllProjectAccessLevels();
                ViewBag.PositionsList = _userRoleService.AllProjectPositions();
                ViewBag.ProjId = ProjectId;
                ViewBag.Id = ProjectId;
                return PartialView("PartialViews/_AddProjectMembersPartialView",memberFromReq); 
            }

            var memebr = _userManager.Users.Where(u => u.UserName == memberFromReq.Name).FirstOrDefault();

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

                var currentUser = await _userManager.GetUserAsync(User);
                var ProjectName = _projectRepository.GetById(ProjectId).Name;
                projectActivityRepository.AddNewActivity(currentUser, ProjectId, "Project Member Added", $"Member \"{memebr.UserName}\" added.");
                projectActivityRepository.Save();

            }
            return RedirectToAction("Index", new { ProjectId = ProjectId });

        }

        [HttpPost]
        [HasRoles(GlobalAccessLevels.AccountAdmin, projectIdRouteKey: "ProjectId", ProjectAccessLevels.ProjectAdmin)]

        public async Task<IActionResult> Delete(string id, int ProjectId)
        {

            var projectMemberRelations = _userRoleService.GetAll().Where(i => i.ProjectId == ProjectId && i.UserId == id).ToList();
            foreach (var relation in projectMemberRelations)
            {
                _userRoleService.Delete(relation);
            }
            _userRoleService.Save();
            var member = await _userManager.FindByIdAsync(id);
            var memberUserName = member.UserName;
            var currentUser = await _userManager.GetUserAsync(User);
            var ProjectName = _projectRepository.GetById(ProjectId).Name;
            projectActivityRepository.AddNewActivity(currentUser, ProjectId, "Project Member Removed", $"Member \"{memberUserName}\" removed.");
            projectActivityRepository.Save();

            return RedirectToAction("Index", new { ProjectId = ProjectId });
        }

        [HttpGet]
        [HasRoles(GlobalAccessLevels.AccountAdmin, projectIdRouteKey: "ProjectId", ProjectAccessLevels.ProjectAdmin)]

        public IActionResult Details(string id, int? ProjectId = null)
        {

            var member = _userManager.Users.FirstOrDefault(u => u.Id == id);
            if (member != null)
            {
                return Json(new
                {
                    userName = member.UserName,
                    email = member.Email,
                    company = member.Company?.Name,
                    role = _userRoleService.GetByUserId(id, ProjectId).Role.Name,
                    accessLevels = member.AccessLevel
                });
            }
            return Json(new { error = "Member not found" });
        }

        [HttpPost]
        [HasRoles(GlobalAccessLevels.AccountAdmin, projectIdRouteKey: "ProjectId", ProjectAccessLevels.ProjectAdmin)]

        public async Task<IActionResult> Update(string id, int ProjectId, [FromBody] UpdateMemberVM member)
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



                var ProjectAccessLevel = _userRoleService.GetAll().FirstOrDefault(i => i.UserId == id && i.ProjectId == ProjectId && i.Role.ProjectAccessLevel == true);
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
                var memberUserName = user.UserName;
                var currentUser = await _userManager.GetUserAsync(User);
                var ProjectName = _projectRepository.GetById(ProjectId).Name;
                projectActivityRepository.AddNewActivity(currentUser, ProjectId, "Project Member Updated", $"Member \"{memberUserName}\" updated.");
                projectActivityRepository.Save();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }

        }


        [HttpGet]
        [HasRoles(GlobalAccessLevels.AccountAdmin, projectIdRouteKey: "ProjectId", ProjectAccessLevels.ProjectAdmin)]

        public IActionResult GetUpdatePartial(string id, int ProjectId)

        {


            var member = _userManager.Users.FirstOrDefault(u => u.Id == id);
            if (member == null)
            {
                return NotFound();
            }


            var insertProjectMemberVM = new InserProjecttMembersVM
            {
                FirstName = member.FirstName,
                LastName = member.LastName,
                MobilePhone = member.MobilePhone,
                Email = member.UserName,
                CompanyId = member.CompanyId,
                currentCompany = _companyRepository.GetById(member.CompanyId ?? 0)?.Name,
                PositionId = _userRoleService.GetPosition(id, ProjectId).Id,
                ProjectAccessLevelId = _userRoleService.GetProjectAccessLevel(id, ProjectId).Id,

            };



            ViewBag.Companies = new SelectList(_companyRepository.GetAll(), "Id", "Name");
            ViewBag.PositionsList = new SelectList(_userRoleService.AllProjectPositions(), "Id", "Name");
            ViewBag.ProjectAccessLevelsList = new SelectList(_userRoleService.AllProjectAccessLevels(), "Id", "Name");
            ViewBag.ProjId = ProjectId;

            return PartialView("PartialViews/_UpdateProjectMembersPartialView", insertProjectMemberVM);


        }
        [AcceptVerbs("Get", "Post")]
        public IActionResult CheckName(string Name, int ProjId)
        {
            var user = _userManager.Users.Where(u => u.UserName.ToLower() == Name.ToLower()).FirstOrDefault();
            string userId = user.Id;

            var isUserExistedInProject = _userRoleService.GetAll().Any(i => i.ProjectId == ProjId && i.UserId == userId);

            if (isUserExistedInProject == false)
            {
                return Json(true);
            }
            else
            {
                return Json(false);
            }
        }
    }
}
