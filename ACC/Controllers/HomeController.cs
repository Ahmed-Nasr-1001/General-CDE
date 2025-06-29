//using ACC.Services;
//using DataLayer.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using System.Diagnostics;

//namespace ACC.Controllers
//{
//    public class HomeController : Controller
//    {
//        private readonly ILogger<HomeController> _logger;
//        private readonly UserManager<ApplicationUser> _userManager;
//        private readonly UserRoleService _userRoleService;

//        public HomeController(ILogger<HomeController> logger , UserManager<ApplicationUser> userManager , UserRoleService userRoleService)
//        {
//            _logger = logger;
//            _userManager = userManager;
//            _userRoleService = userRoleService;
//        }
//        public async Task<IActionResult> Index()
//        {
//            var CurrentUser = await _userManager.GetUserAsync(User);
//            return View();
//        }

//        public IActionResult Privacy()
//        {
//            return View();
//        }

//        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
//        //public IActionResult Error()
//        //{
//        //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
//        //}
//    }
//}
using Microsoft.EntityFrameworkCore;
using DataLayer.Models;
using DataLayer;
using ACC.ViewModels;
using ACC.ViewModels.DashboardProjectCardVM;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, AppDbContext context)
    {
        _logger = logger;
        _userManager = userManager;
        _context = context;
    }

    //public async Task<IActionResult> Index()
    //{
    //    var currentUser = await _userManager.GetUserAsync(User);
    //    var currentUserId = currentUser.Id;

    //    var projectCards = await _context.ProjectMembers
    //       .Where(pm => pm.MemberId == currentUser.Id)
    //       .Include(pm => pm.Project)
    //           .ThenInclude(p => p.Members)
    //       .Select(pm => new DashboardProjectCardVM
    //       {
    //           ProjectId = pm.Project.Id,
    //           Name = pm.Project.Name,
    //           ProjectNumber = pm.Project.ProjectNumber,
    //           IssueCount = _context.Issues.Count(i => i.ProjectId == pm.ProjectId),
    //           ReviewCount = _context.Reviews.Count(r => r.ProjectId == pm.ProjectId),
    //           CreationDate = pm.Project.CreationDate,
    //           IsArchived = pm.Project.IsArchived,
    //           MembersCount = pm.Project.Members.Count
    //       })
    //       .ToListAsync();



    //    return View(projectCards);
    //}

    public async Task<IActionResult> Index()
    {
        var projectCards = await _context.Projects
            .Include(p => p.Members)
            .Select(p => new DashboardProjectCardVM
            {
                ProjectId = p.Id,
                Name = p.Name,
                ProjectNumber = p.ProjectNumber,
                IssueCount = _context.Issues.Count(i => i.ProjectId == p.Id),
                ReviewCount = _context.Reviews.Count(r => r.ProjectId == p.Id),
                CreationDate = p.CreationDate,
                IsArchived = p.IsArchived,
                MembersCount = p.Members.Count
            })
            .ToListAsync();

        return View(projectCards);
    }


}
