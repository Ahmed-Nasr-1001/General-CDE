using ACC.ViewModels.RoleVM;
using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;


namespace ACC.Controllers
{
    public class RoleController : Controller
    {
        private readonly RoleManager<ApplicationRole> _roleManamger;

        public RoleController(RoleManager<ApplicationRole> roleManamger)
        {
            _roleManamger = roleManamger;
        }
        public IActionResult Index(string searchTerm = null)
        {
            var Roles = _roleManamger.Roles.Where(r=>r.ProjectPosition == true);


            var roleViewModel = Roles.Select(x => new RoleViewModel
            {
                RoleName = x.Name
            }).ToList();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                roleViewModel = roleViewModel.Where(i => i.RoleName.ToLower().Contains(searchTerm.ToLower())).ToList();
            }

            ViewBag.SearchTerm = searchTerm;
            return View(roleViewModel);



        }


        [HttpPost]
        public async Task<IActionResult> AddRole(string roleName)
        {
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                var existingRole = await _roleManamger.FindByNameAsync(roleName);
                if (existingRole == null)
                {
                    var newRole = new ApplicationRole { Name = roleName , ProjectPosition = true };
                    var result = await _roleManamger.CreateAsync(newRole);

                    if (result.Succeeded)
                    {
                        return Json(new { success = true, message = "Role added successfully", role = newRole.Name });
                    }

                    return Json(new { success = false, message = "Failed to add role", errors = result.Errors });
                }
                return Json(new { success = false, message = "Role already exists" });
            }
            return Json(new { success = false, message = "Invalid role name" });
        }


        [HttpPost]
        public async Task<IActionResult> DeleteRole(string roleName)
        {
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                var roleToDelete = await _roleManamger.FindByNameAsync(roleName);
                if (roleToDelete != null)
                {
                    var result = await _roleManamger.DeleteAsync(roleToDelete);
                    if (result.Succeeded)
                        return Json(new { success = true, message = "Role deleted successfully" });

                    return Json(new { success = false, message = "Failed to delete role", errors = result.Errors });
                }
                return Json(new { success = false, message = "Role not found" });
            }
            return Json(new { success = false, message = "Invalid role name" });
        }



    }
}
