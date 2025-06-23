using ACC.Services;
using ACC.ViewModels.WorkflowVM;
using BusinessLogic.Repository.RepositoryClasses;
using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer.Models;
using DataLayer.Models.ClassHelper;
using DataLayer.Models.Enums;
using Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace ACC.Controllers
{
    public class WorkflowController : Controller
    {
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IWorkFlowStepRepository _workFlowStepRepository;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly WorkflowStepsUsersService _workflowStepsUsersService;
        private readonly ReviewStepUsersService _reviewStepUsersService;
        private readonly FolderService _folderService;
        private readonly UserRoleService _userRoleService;

        public WorkflowController(IWorkflowRepository workflowRepository, IReviewRepository reviewRepository , IWorkFlowStepRepository workFlowStepRepository, UserManager<ApplicationUser> userManager , WorkflowStepsUsersService workflowStepsUsersService, ReviewStepUsersService reviewStepUsersService , FolderService folderService , UserRoleService userRoleService)
        {
            _workflowRepository = workflowRepository;
            _reviewRepository = reviewRepository;
            _workFlowStepRepository = workFlowStepRepository;
            UserManager = userManager;
            _workflowStepsUsersService = workflowStepsUsersService;
            _reviewStepUsersService = reviewStepUsersService;
            _folderService = folderService;
            _userRoleService = userRoleService;
        }

        public IActionResult Index(int id ,string search = null, int page = 1, int pageSize = 4)
        {

            var query = _workflowRepository.GetAllWithSteps(id);

            if (query == null)
            {
                throw new InvalidOperationException("The workflow repository returned null. Make sure it's returning a valid collection.");
            }

            if (!string.IsNullOrEmpty(search))
            {
                query.Where(i=>i.Name.ToLower().Contains(search.ToLower()));
            }

            int totalRecords = query.Count();

            var WorkflowTemplates = query
                .Select(c => new WorkflowTemplateViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Steps = c.Steps.Select(s => new WorkflowStepInputViewModel
                    {
                        StepOrder = s.StepOrder,
                        TimeAllowedInDays = s.TimeAllowed
                    }).ToList()
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.Id = id;

            return View("Index", WorkflowTemplates);
        }

        public IActionResult NewWorkflow(int stepCount , int proId)
        {
            var vm = new WorkflowTemplateViewModel();
            vm.ReviewersType = Enum.GetValues(typeof(ReviewersType)).Cast<ReviewersType>().ToList();
            vm.AllFolders = _folderService.GetFolderTree();
            vm.ProjectPositions = _userRoleService.AllProjectPositions();
            var ReviewersList = _userRoleService.GetAll().Where(i => i.ProjectId == proId && i.Role.ProjectPosition == true).ToList();
            
            foreach(var item in ReviewersList)
            {
                ProjectReviewersVM projectReviewersVM = new ProjectReviewersVM()
                {
                    UserId = item.UserId,
                    RoleId = item.RoleId,
                    UserName = item.User.UserName,
                    RoleName = item.Role.Name
                };
                vm.Reviewers.Add(projectReviewersVM);
            }
            
            
           


            for (int i = 0; i < stepCount; i++)
            {
                vm.Steps.Add(new WorkflowStepInputViewModel());
            }

            ViewBag.MultiReviwerOptions = Enum_Helper.GetEnumSelectListWithDisplayNames<MultiReviewerOptions>();
            ViewBag.Id = proId;

            return View("NewWorkflow", vm);
        }



        [HttpPost]
        public async Task<IActionResult> CreateTemplate(WorkflowTemplateViewModel vm)
        {
           

            if (!ModelState.IsValid)
            {
                
                vm.ReviewersType = Enum.GetValues(typeof(ReviewersType)).Cast<ReviewersType>().ToList();
                vm.AllFolders = _folderService.GetFolderTree();
                vm.ProjectPositions = _userRoleService.AllProjectPositions();

                var ReviewersList = _userRoleService.GetAll()
                    .Where(i => i.ProjectId == vm.proId && i.Role.ProjectPosition == true)
                    .ToList();

                vm.Reviewers = ReviewersList.Select(item => new ProjectReviewersVM
                {
                    UserId = item.UserId,
                    RoleId = item.RoleId,
                    UserName = item.User.UserName,
                    RoleName = item.Role.Name
                }).ToList();

                ViewBag.MultiReviwerOptions = Enum_Helper.GetEnumSelectListWithDisplayNames<MultiReviewerOptions>();
                ViewBag.Id = vm.proId;

                return View("NewWorkflow", vm);
            }

            var template = new WorkflowTemplate
            {

                ProjectId = vm.proId,
                Name = vm.Name,
                Description = vm.Description,
                StepCount = vm.Steps.Count,
                CopyApprovedFiles = vm.CopyApprovedFiles,
                DestinationFolderId = vm.SelectedDistFolderId

            };

            foreach (var step in vm.Steps)
            {



                var stepTemplate = new WorkflowStepTemplate
                {
                    StepOrder = step.StepOrder,
                    TimeAllowed = step.TimeAllowedInDays,
                    selectedPositionId =step.selectedPositionId,
                    ReviewersType = step.SelectedReviewersType,
                    MinReviewers = step.MinReviewers,

                };


                if (step.SelectedOption == "Every key reviewer must review this step")
                {
                    stepTemplate.MultiReviewerOptions = MultiReviewerOptions.EveryOne;
                }
                else 
                {
                    stepTemplate.MultiReviewerOptions = MultiReviewerOptions.MinimumNumber;
                }

                template.Steps.Add(stepTemplate);
            }

            _workflowRepository.Insert(template);
            _workflowRepository.Save();

            var savedSteps = template.Steps.OrderBy(s => s.StepOrder).ToList();

            for (int i = 0; i < vm.Steps.Count; i++)
            {
                var step = vm.Steps[i];
                var savedStep = savedSteps[i];

                foreach (var userId in step.AssignedUsersIds)
                {
                    var user = await UserManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        WorkflowStepUser workflowStepUser = new WorkflowStepUser()
                        {
                            StepId = savedStep.Id,
                            UserId = user.Id,
                        };

                        _workflowStepsUsersService.Insert(workflowStepUser);
                    }
                }
            }



            _workflowStepsUsersService.Save();


            return RedirectToAction("Index", new { id = vm.proId });
        }

        [HttpGet]
        public async Task<IActionResult> WorkflowDetails(int id)
        {
            var workflowFromDB = _workflowRepository.GetById(id);


            if (workflowFromDB == null)
            {
                return NotFound();
            }

            

            var vm = new DetailsVm
            {
                Id = workflowFromDB.Id,
                proId = workflowFromDB.ProjectId,
                Name = workflowFromDB.Name,
                Description = workflowFromDB.Description,
                CopyApprovedFiles = workflowFromDB.CopyApprovedFiles,
                SelectedDistFolderName = workflowFromDB.DestinationFolderId.HasValue
                                         ? _folderService.FolderName(workflowFromDB.DestinationFolderId.Value)
                                         : null,

                ReviewersType = Enum.GetValues(typeof(ReviewersType)).Cast<ReviewersType>().ToList(),
                AllFolders = _folderService.GetFolderTree(),
                ProjectPositions = _userRoleService.AllProjectPositions(),
                Reviewers = _userRoleService.GetAll().Where(i => i.ProjectId == workflowFromDB.ProjectId).Select(i => new ProjectReviewersVM
                {
                    UserId = i.UserId,
                    RoleId = i.RoleId,
                    UserName = i.User.UserName,
                    RoleName = i.Role.Name
                }).ToList(),






            };

            var stepTemplates = workflowFromDB.Steps.OrderBy(s => s.StepOrder).ToList();
            foreach (var stepTemplate in stepTemplates)
            {
                var assignedUsers = _workflowStepsUsersService.GetStepUsers(stepTemplate.Id);


                var stepVM = new StepDetails
                {
                    StepOrder = stepTemplate.StepOrder,
                    TimeAllowedInDays = stepTemplate.TimeAllowed,
                    SelectedReviewersType = stepTemplate.ReviewersType.ToString(),
                    selectedPosition = stepTemplate.selectedPositionId,
                    MinReviewers = stepTemplate.MinReviewers,
                    SelectedOption = stepTemplate.MultiReviewerOptions == MultiReviewerOptions.EveryOne
                                     ? "Every key reviewer must review this step"
                                     : "Minimum number of reviewers",
                    
                };
                foreach (var assignedUser in assignedUsers)
                {
                    var user = await UserManager.FindByIdAsync(assignedUser);
                    var name = user.UserName;
                    stepVM.AssignedUsersNames.Add(name);
                }

                vm.Steps.Add(stepVM);
            }



            ViewBag.MultiReviwerOptions = Enum_Helper.GetEnumSelectListWithDisplayNames<MultiReviewerOptions>();
            ViewBag.Id = workflowFromDB.ProjectId;
            ViewBag.wfId = id;

            return View("WorkflowDetails", vm);
        }
        

       [HttpGet]
       public async Task<IActionResult> EditWorkflow(int id)
        {
            var workflowFromDB = _workflowRepository.GetById(id);


            if (workflowFromDB == null)
            {
                return NotFound();
            }

            if (workflowFromDB.Reviews != null && workflowFromDB.Reviews.Any())
            {
                TempData["ErrorMessage"] = "Can not edit this workflow because it is already in use.";
                return RedirectToAction("Index", new { id = workflowFromDB.ProjectId });

            }

            var vm = new EditWorkflowVM
            {
                Id= workflowFromDB.Id,
                proId = workflowFromDB.ProjectId,
                Name = workflowFromDB.Name,
                Description = workflowFromDB.Description,
                CopyApprovedFiles = workflowFromDB.CopyApprovedFiles,
                SelectedDistFolderId = workflowFromDB.DestinationFolderId,
                ReviewersType = Enum.GetValues(typeof(ReviewersType)).Cast<ReviewersType>().ToList(),
                AllFolders = _folderService.GetFolderTree(),
                ProjectPositions = _userRoleService.AllProjectPositions(),
                Reviewers = _userRoleService.GetAll().Where(i => i.ProjectId == workflowFromDB.ProjectId).Select(i => new ProjectReviewersVM
                {
                    UserId = i.UserId,
                    RoleId = i.RoleId,
                    UserName = i.User.UserName,
                    RoleName = i.Role.Name
                }).ToList(),




              

            };

            var stepTemplates = workflowFromDB.Steps.OrderBy(s => s.StepOrder).ToList();
            foreach (var stepTemplate in stepTemplates)
            {
                var assignedUsers = _workflowStepsUsersService.GetByStepId(stepTemplate.Id);
                var stepVM = new WorkflowStepInputViewModel
                {
                    StepOrder = stepTemplate.StepOrder,
                    TimeAllowedInDays = stepTemplate.TimeAllowed,
                    SelectedReviewersType = stepTemplate.ReviewersType,
                    selectedPositionId = stepTemplate.selectedPositionId,
                    MinReviewers = stepTemplate.MinReviewers,
                    SelectedOption = stepTemplate.MultiReviewerOptions == MultiReviewerOptions.EveryOne
                                     ? "Every key reviewer must review this step"
                                     : "Minimum number of reviewers",
                };

                vm.Steps.Add(stepVM);
            }

            ViewBag.MultiReviwerOptions = Enum_Helper.GetEnumSelectListWithDisplayNames<MultiReviewerOptions>();
            ViewBag.Id = workflowFromDB.ProjectId;
            ViewBag.wfId = id;

            return View("EditWorkflow", vm);
        }

        [HttpPost]
        public async Task<IActionResult> SaveEdit(EditWorkflowVM vm , int workflowId)
        {
           
            if (!ModelState.IsValid)
            {
                vm.ReviewersType = Enum.GetValues(typeof(ReviewersType)).Cast<ReviewersType>().ToList();
                vm.AllFolders = _folderService.GetFolderTree();
                vm.ProjectPositions = _userRoleService.AllProjectPositions();
                vm.Reviewers = _userRoleService.GetAll().Where(i => i.ProjectId == vm.proId).Select(i => new ProjectReviewersVM
                {
                    UserId = i.UserId,
                    RoleId = i.RoleId,
                    UserName = i.User.UserName,
                    RoleName = i.Role.Name
                }).ToList();

                ViewBag.MultiReviwerOptions = Enum_Helper.GetEnumSelectListWithDisplayNames<MultiReviewerOptions>();
                ViewBag.Id = vm.proId;
                ViewBag.wfId = workflowId;

                return View("EditWorkflow", vm);
            }

            var template = _workflowRepository.GetById(workflowId);

            if (template == null)
                return NotFound();

         
            template.Name = vm.Name;
            template.Description = vm.Description;
            template.CopyApprovedFiles = vm.CopyApprovedFiles;
            if (vm.SelectedDistFolderId != null)
            {
                template.DestinationFolderId = vm.SelectedDistFolderId;
            }

            _workflowRepository.Update(template);
            _workflowRepository.Save();

            var StepsFromVM = vm.Steps.OrderBy(s => s.StepOrder).ToList(); 
            var stepsFromDB = template.Steps.OrderBy(s => s.StepOrder).ToList();

            for (int i=0; i< template.Steps.Count; i++)
            {
                var StepFromVM = StepsFromVM[i];
                stepsFromDB[i].StepOrder = StepFromVM.StepOrder;
                stepsFromDB[i].TimeAllowed = StepFromVM.TimeAllowedInDays;
                stepsFromDB[i].ReviewersType = StepFromVM.SelectedReviewersType;
                stepsFromDB[i].selectedPositionId = StepFromVM.selectedPositionId;
                stepsFromDB[i].MinReviewers = StepFromVM.MinReviewers;

                _workFlowStepRepository.Update(stepsFromDB[i]);
                _workFlowStepRepository.Save();



            }

        

            for (int i = 0; i < vm.Steps.Count; i++)
            {
                var step = StepsFromVM[i];
                var savedStep = stepsFromDB[i];


                var OldStepUsers = _workflowStepsUsersService.GetByStepId(savedStep.Id);

                foreach (var item in OldStepUsers)
                {

                    _workflowStepsUsersService.Delete(item);

                }

                _workflowStepsUsersService.Save();
                savedStep.workflowStepUsers.Clear();
                foreach (var userId in step.AssignedUsersIds)
                {
                    var user = await UserManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        WorkflowStepUser workflowStepUser = new WorkflowStepUser()
                        {
                            StepId = savedStep.Id,
                            UserId = user.Id,
                        };
                        _workflowStepsUsersService.Insert(workflowStepUser);
                        savedStep.workflowStepUsers.Add(workflowStepUser);
                    }
                }
            }

            

            _workflowStepsUsersService.Save();

            bool hasReviews = _reviewRepository.GetAll().Any(i => i.WorkflowTemplateId == template.Id);
            var reviews = _reviewRepository.GetAll().Where(i => i.WorkflowTemplateId == template.Id);
            if (hasReviews== false)
            {

                  foreach (var review in reviews)
                  {
                 
                 
                      for (int i = 0; i < vm.Steps.Count; i++)
                      {
                          var step = StepsFromVM[i];
                          var savedStep = stepsFromDB[i];
                 
                          var OldStepUsers = _reviewStepUsersService.GetByStepId(savedStep.Id , review.Id);
                 
                          foreach (var item in OldStepUsers)
                          {
                 
                              _reviewStepUsersService.Delete(item);
                 
                          }
                          _reviewStepUsersService.Save();
                          foreach (var userId in step.AssignedUsersIds)
                          {
                              var user = await UserManager.FindByIdAsync(userId);
                              if (user != null)
                              {
                                  ReviewStepUser ReviewStepUser = new ReviewStepUser()
                                  {
                                      StepId = savedStep.Id,
                                      UserId = user.Id,
                                      ReviewId = review.Id
                                  };
                 
                                  _reviewStepUsersService.Insert(ReviewStepUser);
                              }
                          }
                      }
                 
                  }

            }

            _workflowRepository.Save();

            return RedirectToAction("Index", new { id = vm.proId });
        }

        public IActionResult Delete (int id)
        {
            var workflow = _workflowRepository.GetById(id);
            int proId = workflow.ProjectId;
            bool hasReviews = _reviewRepository.GetAll().Any(i => i.WorkflowTemplateId == id);
            if (hasReviews == true)
            {
                TempData["ErrorMessage"] = "Can not delete this workflow because it is already in use.";
                return RedirectToAction("Index", new { id = proId });
            }

           

            var steps = _workFlowStepRepository.GetAll().Where(i => i.WorkflowTemplateId == workflow.Id);
            foreach(var item in steps)
            {
                var stepusers = _workflowStepsUsersService.GetAll().Where(i => i.StepId == item.Id);
                foreach (var element in stepusers)
                {
                    _workflowStepsUsersService.Delete(element);
                }
                _workflowStepsUsersService.Save();
                _workFlowStepRepository.Delete(item);
            }
            _workFlowStepRepository.Save();

            _workflowRepository.Delete(workflow);
            _workflowRepository.Save();

            return RedirectToAction("Index", new { id = proId });

        }

        [AcceptVerbs("Get", "Post")]
        public IActionResult IsNameUnique(string name, int proId)
        {
            var exists = _workflowRepository.GetAll()
                .Any(w => w.Name.ToLower() == name.ToLower() && w.ProjectId == proId);

            return Json(!exists); 
        }

        [AcceptVerbs("Get", "Post")]
        public IActionResult IsNameAvailable(string name, int Id, int proId)
       {
            var exists = _workflowRepository.GetAll()
                .Any(w => w.Id != Id && w.Name.ToLower() == name.ToLower() && w.ProjectId == proId);
            if (exists==true)
            {
            return Json(false);

            }
            return Json(true);
        }



    }
}
