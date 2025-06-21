using ACC.ViewModels.MemberVM;
using ACC.ViewModels.MemberVM.MemberVM;
using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer.Models;
using DataLayer.Models.ClassHelper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ACC.Services;
using Microsoft.AspNetCore.Authorization;


namespace ACC.Controllers
{
    public class MemberController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserRoleService _userRoleService;
        private readonly ICompanyRepository _companyRepository;

        public MemberController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            UserRoleService userRoleService,
            ICompanyRepository companyRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _userRoleService = userRoleService;
            _companyRepository = companyRepository;
        }
        public IActionResult Index(int page = 1, string search = "", int pageSize = 10)
        {
            var query = _userManager.Users
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsQueryable();
        
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m => m.UserName.Contains(search));
            }
        
            var totalItems = query.Count();
        
            var users = query
                .OrderBy(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        
            var members = users.Select(m => new MemberVM
            {
                Id = m.Id,
                Name = m.UserName,
                Company = m.Company?.Name ?? "No Company",
                GlobalAccessLevel = _userRoleService.GetGlobalAccessLevel(m.Id).Name,
                AddedOn = m.AddedOn
            }).ToList();
        
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    data = members,
                    totalItems,
                    currentPage = page,
                    pageSize
                });
            }
        
            ViewBag.Companies = _companyRepository.GetAll();
            ViewBag.GlobalAccessLevelsList = _userRoleService.AllGlobalAccessLevels();
            return View(members);
        }


        [HttpPost]
        public async Task<IActionResult> InsertMember(InsertMemberVM memberFromReq)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Companies = _companyRepository.GetAll();
                ViewBag.GlobalAccessLevelsList = _userRoleService.AllGlobalAccessLevels();
                return PartialView("PartialViews/_AddMemberPartialView", memberFromReq);
            }
           
                

                var user = new ApplicationUser
                {
                    FirstName = memberFromReq.FirstName,
                    LastName = memberFromReq.LastName,
                    MobilePhone = memberFromReq.MobilePhone,
                    UserName = memberFromReq.Email,
                    CompanyId = memberFromReq.CompanyId,
                };

               
              

                var result = await _userManager.CreateAsync(user, memberFromReq.Password);

                if (result.Succeeded)
                {
                   

                    var userRole = new ApplicationUserRole
                    {
                        ProjectId = null,
                        RoleId = memberFromReq.GlobalAccessLevelId,
                        UserId = user.Id
                    };

                    _userRoleService.Insert(userRole);
                    _userRoleService.Save();

                    TempData["SuccessMessage"] = "Member added successfully!";
                    return RedirectToAction("Index", "Member");

            }

            foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            



            ViewBag.Companies = _companyRepository.GetAll();
            ViewBag.GlobalAccessLevelsList = _userRoleService.AllGlobalAccessLevels();
            return PartialView("PartialViews/_AddMemberPartialView", memberFromReq);
        }


        [HttpPost]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            var member = await _userManager.FindByIdAsync(id);
            if (member != null)
            {
                var result = await _userManager.DeleteAsync(member);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Member deleted successfully!";
                    return Ok();
                }

               
            }

            TempData["ErrorMessage"] = "Failed to delete member!";
            return NotFound();
        }

        [HttpGet]
        public IActionResult Details(string id)
        {
            var member = _userManager.Users.Include(u=>u.UserRoles).ThenInclude(ur=>ur.Role).FirstOrDefault(u => u.Id == id);
            if (member != null)
            {
                var GlobalAccessLevel = _userRoleService.GetGlobalAccessLevel(id);

                return Json(new
                {
                    userName = member.UserName,
                    email = member.Email,
                    company = member.Company?.Name,
                    GlobalAccessLevelId = GlobalAccessLevel.Id,
                });
            }

            return Json(new { error = "Member not found" });
        }

        [HttpPost]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateMemberVM member)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var mmbr = await _userManager.FindByIdAsync(id);
                if (mmbr == null)
                    return NotFound();

                mmbr.UserName = member.email;
                mmbr.CompanyId = member.companyId;

               

                var result = await _userManager.UpdateAsync(mmbr);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description);
                    return BadRequest(new { success = false, errors });
                }

                

                if (result.Succeeded)
                {
                    var mmbrRole = _userRoleService.GetAll().FirstOrDefault(i => i.UserId == id && i.Role.GloblaAccesLevel == true);
                    if (mmbrRole != null )
                    {
                        _userRoleService.Delete(mmbrRole);
                        _userRoleService.Save();
                    }

                    var userRole = new ApplicationUserRole
                    {
                        ProjectId = mmbrRole.ProjectId,
                        RoleId = member.globalAccessLevelID,
                        UserId = id
                    };

                    _userRoleService.Insert(userRole);
                    _userRoleService.Save();

                
                }



                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public IActionResult GetUpdatePartial(string id)
        {
            var member = _userManager.Users.FirstOrDefault(u => u.Id == id);
            if (member == null)
                return NotFound();



            var insertMemberVM = new InsertMemberVM
            {
                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.UserName,
                CompanyId = member.CompanyId,
                MobilePhone = member.MobilePhone,
                currentCompany = _companyRepository.GetById(member.CompanyId ?? 0)?.Name,
                GlobalAccessLevelId = _userRoleService.GetGlobalAccessLevel(id).Name

            };

            ViewBag.Companies = new SelectList(_companyRepository.GetAll(), "Id", "Name");
            ViewBag.GlobalAccessLevelsList = new SelectList(_userRoleService.AllGlobalAccessLevels(), "Id", "Name");

            return PartialView("PartialViews/_UpdateMemberPartialView", insertMemberVM);
        }

     


    }
}