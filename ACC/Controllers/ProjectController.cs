using ACC.ViewModels.ProjectVMs;
using BusinessLogic.Repository.RepositoryClasses;
using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer;
using DataLayer.Models;
using DataLayer.Models.Enums;
using Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;

namespace ACC.Controllers
{
    public class ProjectController : Controller
    {
        private readonly IProjetcRepository projectRepo;
        private readonly IProjectActivityRepository projectActivityRepo;
        private readonly IFolderRepository _folderRepository;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _context;
        public ProjectController(AppDbContext context, IProjetcRepository ProjectRepo ,IProjectActivityRepository projectActivityRepo , IFolderRepository folderRepository, UserManager<ApplicationUser> userManager)

        {
            _context = context;
            projectRepo = ProjectRepo;
            this.projectActivityRepo = projectActivityRepo;
            _folderRepository = folderRepository;
            this.userManager = userManager;
        }
     

        #region Index DisplayData Action
        public IActionResult Index(string srchText, int Page = 1, int Pagesize = 5, bool showArchived = false)
        {
            var Currencies = new SelectList(Enum.GetValues(typeof(Currency)).Cast<Currency>());

            var ProjectTypes = Enum.GetValues(typeof(ProjectType)).Cast<ProjectType>()
                .Select(pt => new
                {
                    Value = pt.ToString(),
                    DisplayName = Enum_Helper.GetDescription(pt)
                })
                .ToList();

            ViewBag.Currencies = Currencies;
            ViewBag.ProjectTypes = new SelectList(ProjectTypes, "Value", "DisplayName");

            // Get paginated projects with showArchived filter
            var projects = projectRepo.GetPaginatedProjects(Page, Pagesize, srchText, showArchived);

            // If no projects are found, return an empty list
            if (projects == null || !projects.Any())
            {
                return View(new List<DisplayProjectsVM>());
            }

            // Map the projects to the ViewModel
            List<DisplayProjectsVM> displayProject = projects.Select(p => new DisplayProjectsVM
            {
                id = p.Id,
                Name = p.Name,
                ProjectNumber = p.ProjectNumber,
                ProjectType = p.ProjectType,
                ProjectValue = p.ProjectValue,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                CreationDate = p.CreationDate
            }).ToList();

            // Calculate pagination details
            int ProjectCount = projectRepo.GetProjectsCount(srchText, showArchived);
            int TotalPages = (int)Math.Ceiling((double)ProjectCount / Pagesize);

            // Pass pagination details to the view
            ViewBag.CurrentPage = Page;
            ViewBag.TotalPages = TotalPages; 
            ViewBag.TotalItems = ProjectCount;
            ViewBag.ShowArchived = showArchived;

            return View("Index", displayProject);
        }

        #endregion



        #region Create Project Action 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProject(AddProjectVM projectFromRequest)
        {
            if (ModelState.IsValid)
            {
                Project newProject = new Project
                {

                    Name = projectFromRequest.Name,
                    ProjectNumber = projectFromRequest.ProjectNumber,
                    ProjectType = projectFromRequest.ProjectType,
                    ProjectValue = projectFromRequest.ProjectValue,
                    StartDate = projectFromRequest.StartDate,
                    EndDate = projectFromRequest.EndDate,
                    CreationDate = DateTime.Now,
                    Currency = projectFromRequest.Currency,
                    Address = projectFromRequest.Address,
                    Latitude = projectFromRequest.Latitude,
                    Longitude = projectFromRequest.Longitude
                };

                projectRepo.Insert(newProject);
                projectRepo.Save();
                var currentUser = await userManager.GetUserAsync(User);
                projectActivityRepo.AddNewActivity(currentUser, newProject.Id , "Project Added", $"Project \"{newProject.Name}\" archived.");
                projectActivityRepo.Save();


                // Create default folders if none exist
                var workInProgress = new Folder
                {
                    Name = "Work In Progress",
                    ProjectId = newProject.Id,
                    SubFolders = new List<Folder>(),
                    Documents = new List<Document>(),
                    CreatedBy = "System"
                };
                var shared = new Folder
                {
                    Name = "Shared",
                    ProjectId = newProject.Id,
                    SubFolders = new List<Folder>(),
                    Documents = new List<Document>(),
                    CreatedBy = "System"
                };
                var published = new Folder
                {
                    Name = "Published",
                    ProjectId = newProject.Id,
                    SubFolders = new List<Folder>(),
                    Documents = new List<Document>(),
                    CreatedBy = "System"
                };
                var archive = new Folder
                {
                    Name = "Archive",
                    ProjectId = newProject.Id,
                    SubFolders = new List<Folder>(),
                    Documents = new List<Document>(),
                    CreatedBy = "System"
                };
                var initialFolders = new List<Folder> { workInProgress, shared, published, archive };

                // Insert default folders
                foreach (var folder in initialFolders)
                {
                    _folderRepository.Insert(folder);
                }
                _folderRepository.Save(); // Use async SaveAsync for consistency

                return Json(new { success = true });
            }

            // If ModelState is invalid, return validation errors as JSON
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return Json(new { success = false, errors = errors });
        }

        #endregion

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var deletedProject = projectRepo.GetById(id);
            if (deletedProject == null)
            {
                return NotFound(); // Return 404 if project isn't found
            }

            var activities = projectActivityRepo.GetAll();
            for(int i = 0; i < activities.Count; i++)
            {
                if(activities[i].Id == id)
                {
                    projectActivityRepo.Delete(activities[i]);
                }
            }
            projectRepo.Delete(deletedProject);
            projectRepo.Save();
         

            return Json(new { success = true });  // Return JSON response for AJAX
        }

        //[HttpPost]
        public async Task<IActionResult> Archive(int id)
        {
            var project = projectRepo.GetById(id);
            if (project == null)
            {
                return NotFound();
            }
            project.IsArchived = true;
            projectRepo.Save();
            var currentUser = await userManager.GetUserAsync(User);
            projectActivityRepo.AddNewActivity(currentUser, project.Id, "Project Archived", $"Project \"{project.Name}\" archived.");
            projectActivityRepo.Save();
            return Ok();
        }

        public async Task<IActionResult> Restore(int id)
        {
            var project = projectRepo.GetById(id);
            if (project == null)
            {
                return NotFound();
            }
            project.IsArchived = false;
            projectRepo.Save();
            var currentUser = await userManager.GetUserAsync(User);
            projectActivityRepo.AddNewActivity(currentUser, project.Id, "Project Restored", $"Project \"{project.Name}\" restored.");
            projectActivityRepo.Save();
            return Ok();
        }

    }
}
