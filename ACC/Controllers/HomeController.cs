using ACC.ViewModels;
using ACC.ViewModels.DashboardProjectCardVM;
using DataLayer;
using DataLayer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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

 

    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var currentUserId = currentUser.Id;
        var projectCards = await _context.Projects
            .Include(p=>p.UserRoles).Include(p => p.Members)
            .Select(p => new DashboardProjectCardVM
            {
                ProjectId = p.Id,
                Name = p.Name,
                ProjectNumber = p.ProjectNumber,
                IssueCount = _context.Issues.Count(i => i.ProjectId == p.Id),
                ReviewCount = _context.Reviews.Count(r => r.ProjectId == p.Id),
                MembersCount = _userManager.Users
                              .Include(u => u.UserRoles)
                              .Count(u => u.UserRoles.Any(ur => ur.ProjectId == p.Id)),

                CreationDate = p.CreationDate,
                IsArchived = p.IsArchived,
            })
            .ToListAsync();

        return View(projectCards);
    }


}
