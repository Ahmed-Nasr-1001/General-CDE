using System.Threading.Tasks;
using ACC.Services;
using ACC.ViewModels.Security;
using DataLayer.Models;
using DataLayer.Models.ClassHelper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ACC.Controllers.Security
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly SignInManager<ApplicationUser> SignInManager;
        private readonly UserRoleService _userRoleService;

        public AccountController(UserManager<ApplicationUser> userManager , SignInManager<ApplicationUser> signInManager , UserRoleService userRoleService)
        {
            UserManager = userManager;
            SignInManager = signInManager;
            _userRoleService = userRoleService;
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View("Register");
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterUserVM registerUserVM)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser UserModel = new ApplicationUser();
                UserModel.UserName = registerUserVM.UserName;
                UserModel.PasswordHash = registerUserVM.Password;
                
                IdentityResult Result = await UserManager.CreateAsync(UserModel , registerUserVM.Password);
                
                if (Result.Succeeded)
                {
                    //Create Cookie
                    await SignInManager.SignInAsync(UserModel , false);
                    return RedirectToAction("LogIn", "Account");
                    
                }
                else
                {
                    foreach(var error in Result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        
                    }
                }
            }
          
                return View("Register", registerUserVM);
            
        }

        [HttpGet]
        public IActionResult LogIn()
        {
            return View("LogIn");
        }
        [HttpPost]
        public async Task<IActionResult> LogIn(LogInUserVM logInUserVM)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(logInUserVM.UserName);

                if (user != null)
                {
                    bool isPasswordCorrect = await UserManager.CheckPasswordAsync(user, logInUserVM.Password);
                    if (isPasswordCorrect)
                    {
                        await SignInManager.SignInAsync(user, logInUserVM.RememberMe);

                        bool isAdmin = _userRoleService.GetAll().Any(i => i.UserId == user.Id && i.Role.Name == GlobalAccessLevels.AccountAdmin);

                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError("", "Invalid username or password.");
            }

            return View(logInUserVM);
        }


        public async Task<IActionResult> LogOut()
        {
            await SignInManager.SignOutAsync();
            return RedirectToAction("LogIn", "Account");
        }
    }
}
