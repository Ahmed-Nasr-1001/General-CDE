using ACC.ViewModels.ProjectCompanyVM;
using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer.Models;
using DataLayer.Models.ClassHelper;
using DataLayer.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ACC.Controllers.ProjectDetailsController
{
    public class ProjectCompanyController : Controller
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IProjectActivityRepository _projectActivityRepository;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IProjetcRepository projetcRepository;

        //hello
        public ProjectCompanyController(ICompanyRepository companyRepository, IProjectActivityRepository projectActivityRepository , UserManager<ApplicationUser> userManager , IProjetcRepository projetcRepository)
        {
            _companyRepository = companyRepository;
            _projectActivityRepository = projectActivityRepository;
            this.userManager = userManager;
            this.projetcRepository = projetcRepository;
        }

        // GET: /ProjectCompany/Index/{id}
        public IActionResult Index(int id, int page = 1, int pageSize = 4, string searchTerm = null)
        {

            var companies = _companyRepository.GetCompaniesInEacProjectWithPrpjectId(id);

            // Apply search filter if searchTerm is provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                companies = companies.Where(c =>
                    c.Name.ToLower().Contains(searchTerm) ||
                    (c.Website != null && c.Website.ToLower().Contains(searchTerm)) ||
                    c.CompanyType.ToString().ToLower().Contains(searchTerm)
                ).ToList();
            }

            int totalRecords = companies.Count();

            var companyList = companies
                .Select(c => new ProjectCompanyVM
                {
                    Id = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    Description = c.Description,
                    Website = c.Website,
                    PhoneNumber = c.PhoneNumber,
                    SelectedCountry = c.Country,
                    SelectedCompanyType = c.CompanyType,
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Id = id;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.SearchTerm = searchTerm; // Pass searchTerm back to the view for persistence

            return View(companyList);
        }

        // GET: /ProjectCompany/GetCompaniesForProject/{id}
        [HttpGet]

        public IActionResult GetCompaniesForProject(int id)
        {
            var allCompanies = _companyRepository.GetAll();
            var assignedCompanies = _companyRepository.GetCompaniesInEacProjectWithPrpjectId(id).Select(c => c.Id).ToList();

            var companiesForSelection = allCompanies.Select(c => new
            {
                Id = c.Id,
                Name = assignedCompanies.Contains(c.Id) ? $"{c.Name} (Already in project)" : c.Name,
                Disabled = assignedCompanies.Contains(c.Id)
            }).ToList();

            return Json(companiesForSelection);
        }

        // POST: /ProjectCompany/SaveNew
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasRoles(GlobalAccessLevels.AccountAdmin , projectIdRouteKey:"id", ProjectAccessLevels.ProjectAdmin)]

        public async Task<IActionResult> SaveNew(int id, [Bind("Name,Address,Description,Website,PhoneNumber,SelectedCountry,SelectedCompanyType")] ProjectCompanyVM model, string selectedCompanyId)
        {
            //Console.WriteLine($"selectedCompanyId: {selectedCompanyId}");
            //Console.WriteLine($"model.Name: {model.Name}");
            //Console.WriteLine($"model.PhoneNumber: {model.PhoneNumber}");
            //Console.WriteLine($"model.SelectedCountry: {model.SelectedCountry}");
            //Console.WriteLine($"model.SelectedCompanyType: {model.SelectedCompanyType}");

            if (int.TryParse(selectedCompanyId, out int companyId) && companyId != 0)
            {
                var company = _companyRepository.GetById(companyId);
                if (company == null)
                {
                    return Json(new { success = false, message = "Company not found." });
                }
                _companyRepository.AddCompanyToProject(company.Id, id);

                var currentUser = await userManager.GetUserAsync(User);
                var projectName = projetcRepository.GetById(id).Name;
                _projectActivityRepository.AddNewActivity(currentUser, id, "Project Company Added",
                $"Company \"{company.Name}\" added.");
                return Json(new { success = true, redirect = Url.Action("Index", new { id }) });
            }
            else
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    //Console.WriteLine("ModelState Errors: " + string.Join(", ", errors));
                    return Json(new { success = false, errors });
                }

                try
                {
                    var company = new Company
                    {
                        Name = model.Name,
                        Address = model.Address,
                        Description = model.Description,
                        Website = model.Website,
                        PhoneNumber = model.PhoneNumber,
                        Country = model.SelectedCountry.Value,
                        CompanyType = model.SelectedCompanyType.Value
                    };
                    //Console.WriteLine("Inserting company...");
                    _companyRepository.Insert(company);
                    //Console.WriteLine("Saving changes...");
                    _companyRepository.Save();
                    //Console.WriteLine("Adding company to project...");
                    _companyRepository.AddCompanyToProject(company.Id, id);
                    //Console.WriteLine("Adding activity...");
                
                    return Json(new { success = true, redirect = Url.Action("Index", new { id }) });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
                }
            }
        }

        // POST: /ProjectCompany/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasRoles(GlobalAccessLevels.AccountAdmin, projectIdRouteKey: "projectId", ProjectAccessLevels.ProjectAdmin)]


        public async Task<IActionResult> Delete(int? id, int projectId)
        {
            if (id == null)
            {
                return NotFound();
            }
            var company = _companyRepository.GetById(id.Value);
            if (company == null)
            {
                return NotFound();
            }
            _companyRepository.RemoveCompanyFromProject(id.Value, projectId);
            TempData["SuccessMessage"] = "Company removed from project successfully.";
            var currentUser = await userManager.GetUserAsync(User);
            var projectName = projetcRepository.GetById(projectId).Name;
            _projectActivityRepository.AddNewActivity(currentUser, projectId, "Project Company Removed",
            $"Company \"{company.Name}\" removed.");
            return RedirectToAction("Index", new { id = projectId });
        }

        // GET: /ProjectCompany/Details/{id}
        public IActionResult Details(int id)
        {
            var company = _companyRepository.GetById(id);
            if (company == null)
            {
                return NotFound();
            }
            return Json(new
            {
                name = company.Name,
                phoneNumber = company.PhoneNumber,
                website = company.Website,
                address = company.Address,
                description = company.Description,
                selectedCountry = company.Country.ToString(),
                selectedCompanyType = company.CompanyType.ToString(),
                companyTypes = Enum.GetValues(typeof(CompanyType))
                    .Cast<CompanyType>()
                    .Select(c => new { Value = c.ToString(), Text = c.ToString() })
                    .ToList(),
                countries = Enum.GetValues(typeof(Country))
                    .Cast<Country>()
                    .Select(c => new { Value = c.ToString(), Text = c.ToString() })
                    .ToList()
            });
        }

        // POST: /ProjectCompany/UpdateCompany
        [HttpPost]
        [ValidateAntiForgeryToken]


        public IActionResult UpdateCompany(int id,  [FromBody] UpdatedProjectCompanyVM model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data." });
            }
            var company = _companyRepository.GetById(id);
            if (company == null)
            {
                return NotFound();
            }
            try
            {
                company.Name = model.Name;
                company.Address = model.Address;
                company.Description = model.Description;
                company.Website = model.Website;
                company.PhoneNumber = model.PhoneNumber;
                company.CompanyType = (CompanyType)Enum.Parse(typeof(CompanyType), model.SelectedCompanyType);
                company.Country = (Country)Enum.Parse(typeof(Country), model.SelectedCountry);
                _companyRepository.Update(company);
                _companyRepository.Save();
                return Json(new { success = true, message = "Company updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        public IActionResult CheckName(string name)
        {
            Company company = _companyRepository.GetCompanyByName(name);
            return Json(company == null);
        }

        public IActionResult CheckWebsite(string website)
        {
            Company company = _companyRepository.GetCompanyByEmail(website);
            return Json(company == null);
        }
    }
}