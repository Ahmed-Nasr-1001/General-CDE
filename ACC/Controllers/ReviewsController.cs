using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ACC.Services;
using ACC.ViewModels.ReviewsVM;
using BusinessLogic.Repository.RepositoryClasses;
using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer;
using DataLayer.Models;
using DataLayer.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;

namespace ACC.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IWorkFlowStepRepository _workFlowStepRepo;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly FolderService FolderService;
        private readonly ReviewDocumentService ReviewDocumentService;
        private readonly ReviewFolderService ReviewFolderService;
        private readonly WorkflowStepsUsersService WorkflowStepUserService;
        private readonly ReviewStepUsersService ReviewStepUsersService;
        private readonly INotificationService _notificationService;


        public ReviewsController( IDocumentRepository documentRepository ,IReviewRepository reviewRepo, IWorkflowRepository workflowRepo, IWorkFlowStepRepository workFlowStepRepo, FolderService folderService, ReviewDocumentService reviewDocumentService, ReviewFolderService reviewFolderService ,WorkflowStepsUsersService workflowStepsUsersService, ReviewStepUsersService reviewStepUsersService , UserManager<ApplicationUser> userManager, INotificationService notificationService)
        {
            _documentRepository = documentRepository;
            _reviewRepository = reviewRepo;
            _workflowRepository = workflowRepo;
            _workFlowStepRepo = workFlowStepRepo;
            FolderService = folderService;
            ReviewDocumentService = reviewDocumentService;
            ReviewFolderService = reviewFolderService;
            WorkflowStepUserService= workflowStepsUsersService;
            ReviewStepUsersService = reviewStepUsersService;
            UserManager = userManager;
            _notificationService = notificationService;
        }
        public async Task<IActionResult> Index(int id ,string? srchText = null, bool showActive = false ,bool showArchived = false , bool reviewedByMe = false ,bool startedByMe = false , int page = 1, int pageSize = 4)
                     
        {
            if (!showActive && !showArchived && !reviewedByMe && !startedByMe)
            {
                showActive = true;
            }



            var CurrentUser = await UserManager.GetUserAsync(User);

            var query = _reviewRepository.GetCurrentStepUserReviews(CurrentUser.Id, id)
               .Where(r => r.FinalReviewStatus == FinalReviewStatus.Pending && r.InitiatorUserId != CurrentUser.Id).OrderByDescending(r => r.Id);

            if (showArchived)
            {
                query = _reviewRepository.GetAll().Where(r =>r.ProjectId==id && r.FinalReviewStatus != FinalReviewStatus.Pending 
                && r.InitiatorUserId == CurrentUser.Id).OrderByDescending(r => r.Id);
            }
            else if (reviewedByMe)
            {
                var ReviewIdList = ReviewStepUsersService.GetAll()
                       .Where(r => r.UserId == CurrentUser.Id && r.IsApproved != null )
                       .Select(r =>r.ReviewId)
                       .Distinct();

                query = _reviewRepository.GetAll().Where(r => r.ProjectId == id && r.InitiatorUserId != CurrentUser.Id && ReviewIdList.Contains(r.Id)).OrderByDescending(r=>r.Id).OrderByDescending(r => r.Id);


            }
            else if (startedByMe)
            {
                query = _reviewRepository.GetAll().Where(r => r.ProjectId == id 
                && r.FinalReviewStatus == FinalReviewStatus.Pending && r.InitiatorUserId== CurrentUser.Id).OrderByDescending(r => r.Id);

            }


            if (!string.IsNullOrEmpty(srchText))
            {
                query = query.Where(r => r.Name.ToLower().Contains(srchText.ToLower())).OrderByDescending(r => r.Id);
            }

            int totalRecords = query.Count();




            var ReviewListModel = query
                .Select(c => new ReviewVM
                {
                    Id = c.Id,
                    Name = c.Name,
                    WorkflowName = _workflowRepository.GetAll().FirstOrDefault(wf => wf.Id == c.WorkflowTemplateId).Name,
                    FinalReviewStatus = c.FinalReviewStatus.ToString(),
                    CreatedAt = c.CreatedAt.ToString(),
                    Initiator = c.InitiatorUserId,
                    CurrentStepController = c.CurrentStep?.StepOrder
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pass pagination data to the view
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.Id = id;    
            ViewBag.CreateReviewVM = new CreateReviewVM
            {
                proId = id,
                WorkflowTemplates = _workflowRepository.GetAll().ToList(),
                FinalReviewStatuses = Enum.GetValues(typeof(FinalReviewStatus)).Cast<FinalReviewStatus>().ToList(),
                AllFolders = FolderService.GetFolderWithDocumentTree(),
            };

            ViewBag.CurrentUserId = CurrentUser.Id;


            ViewBag.ShowActive = showActive;
            ViewBag.ReviewedByMe = reviewedByMe;
            ViewBag.StartedByMe = startedByMe;
            ViewBag.ShowArchived = showArchived;

            
        

            return View("Index", ReviewListModel);
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveReviewAsync(CreateReviewVM model)
        {
            if (!ModelState.IsValid)
            {
                var allErrors = ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .Select(ms => new
                    {
                        Field = ms.Key,
                        Errors = ms.Value.Errors.Select(e => e.ErrorMessage).ToList()
                    }).ToList();

                // Example: Log them or inspect in debugger
                foreach (var error in allErrors)
                {
                    Console.WriteLine($"Field: {error.Field}");
                    foreach (var err in error.Errors)
                    {
                        Console.WriteLine($"   Error: {err}");
                    }
                }

                return PartialView("PartialViews/_CreateReviewPartialView", model);
            }




            var Templatesteps = _workflowRepository.GetById(model.SelectedWorkflowId).Steps;
                var CurrentUser = await UserManager.GetUserAsync(User);

                    var Review = new Review
                    {
                        ProjectId = model.proId,
                        Name = model.Name,
                        FinalReviewStatus = FinalReviewStatus.Pending,
                        WorkflowTemplate = _workflowRepository.GetById(model.SelectedWorkflowId),
                        WorkflowTemplateId = model.SelectedWorkflowId,
                        CurrentStepId = null,
                        CreatedAt = DateTime.Now,
                        InitiatorUserId = CurrentUser.Id,

                    };
                    _reviewRepository.Insert(Review);
                    _reviewRepository.Save();

                    // Send notifications to assigned reviewers
                    await _notificationService.NotifyReviewCreatedAsync(Review);

                    if (model.SelectedDocumentIds != null)
                    {
                        foreach (var id in model.SelectedDocumentIds)
                        {
                            var ReviewDocument = new ReviewDocument
                            {
                                ReviewId = Review.Id,
                                DocumentId = id,
                                Status = "Pending"
                            };

                            ReviewDocumentService.Insert(ReviewDocument);
                            ReviewDocumentService.Save();


                        }

                    }

                    if (model.SelectedFolderIds != null)
                    {
                        foreach (var id in model.SelectedFolderIds)
                        {
                            var ReviewFolder = new ReviewFolder
                            {
                                ReviewId = Review.Id,
                                FolderId = id,
                            };

                            ReviewFolderService.Insert(ReviewFolder);
                            ReviewFolderService.Save();


                        }

                    }

                    List<int> StepsId = new List<int>();
                    foreach (var item in Review.WorkflowTemplate.Steps)
                    {
                        StepsId.Add(item.Id);
                    }

                    List<WorkflowStepUser> workflowStepUsers = new List<WorkflowStepUser>();
                    foreach (var item in StepsId)
                    {
                        List<WorkflowStepUser> workflowStepUsers2 = WorkflowStepUserService.GetByStepId(item).ToList();
                        foreach (var item2 in workflowStepUsers2)
                        {
                            workflowStepUsers.Add(item2);

                        }

                    }

                    foreach (var item in workflowStepUsers)
                    {
                        var ReviewStepUser = new ReviewStepUser()
                        {
                            ReviewId = Review.Id,
                            StepId = item.StepId,
                            UserId = item.UserId,
                        };
                        ReviewStepUsersService.Insert(ReviewStepUser);
                        ReviewStepUsersService.Save();
                    }


                    return RedirectToAction("Index", new { id = model.proId, startedByMe = true });
                }
            

          
        


        public async Task<IActionResult> StartReview(int Id)
        {
            Review ReviewFromDB = _reviewRepository.GetReviewById(Id);

            ReviewFromDB.CurrentStepId = _workflowRepository.GetById(ReviewFromDB.WorkflowTemplateId).Steps[0].Id;
            ReviewFromDB.FinalReviewStatus = FinalReviewStatus.Pending;

            _reviewRepository.Save();

            return RedirectToAction("Index", new { id = ReviewFromDB.ProjectId , startedByMe = true });
        }


        [HttpPost]
        public ActionResult Submit(List<DocumentUponAction> DocumentList, int ReviewId)
        {
            Review ReviewFromDB = _reviewRepository.GetById(ReviewId);
            foreach (var doc in DocumentList)
            {
                var reviewDocument = ReviewDocumentService.GetAll().FirstOrDefault(i => i.ReviewId == ReviewId && i.DocumentId == doc.Id);
                reviewDocument.Status = doc.State;
                ReviewDocumentService.Update(reviewDocument);
                ReviewDocumentService.Save();
            }

            foreach (var doc in DocumentList)
            {
                if (doc.State == "Pending")
                {
                    TempData["Error"] = "Some documents are still pending!";
                    return RedirectToAction("Details", new { id = ReviewId });

                }
            }

            foreach (var doc in DocumentList)
            {
                if (doc.State == "Rejected")
                {
                    return RedirectToAction("Reject", new { Id = ReviewId });
                }
            }



            return RedirectToAction("Approve", new { Id = ReviewId });

        }

        public async Task<IActionResult> Approve(int Id)
        {
            Review ReviewFromDB = _reviewRepository.GetReviewById(Id);

            int CurrentReviewStep = ReviewFromDB.CurrentStep.StepOrder.Value;
            int NextReviewStep = CurrentReviewStep + 1;
            int StepsCount = _workflowRepository.GetById(ReviewFromDB.WorkflowTemplateId).Steps.Count();
            var StepMultiReviewerOption = ReviewFromDB.CurrentStep.MultiReviewerOptions;
            var StepReviewerType = ReviewFromDB.CurrentStep.ReviewersType;

            var CurrentUser = await UserManager.GetUserAsync(User);
            var CurrntStep = ReviewFromDB.CurrentStep;
            ReviewStepUsersService.Get(CurrentUser.Id, CurrntStep.Id , ReviewFromDB.Id).IsApproved = true;
            ReviewStepUsersService.Save();

            var ReviewDocs = ReviewDocumentService.GetAll().Where(r => r.ReviewId == Id);
            foreach(var doc in ReviewDocs)
            {
                
                    doc.Status = "Rejected";
               
                doc.Status = "Pending";
                ReviewDocumentService.Update(doc);
            }
            ReviewDocumentService.Save();
            bool Advance = true;

            
              
                if (StepReviewerType == (ReviewersType)1) 
                {
                    if(StepMultiReviewerOption == (MultiReviewerOptions)1)
                    {
                       var StepUsers = ReviewStepUsersService.GetByStepId(CurrntStep.Id ,Id );
                       foreach (var item in StepUsers)
                        {
                            if (item.IsApproved != true)
                            {
                                Advance = false;
                                break;
                            }
                        }

                    }
                    else if (StepMultiReviewerOption == (MultiReviewerOptions)2)
                    {
                        var StepUsers = ReviewStepUsersService.GetByStepId(CurrntStep.Id , Id);
                        int counter = 0;

                        foreach(var item in StepUsers)
                        {
                            if(item.IsApproved==true)
                                { counter++; }
                        }

                        if (counter < CurrntStep.MinReviewers)
                        {
                            Advance=false;
                        }
                    }
                    
                }


            if(Advance == true)
            {
                if (NextReviewStep <= StepsCount)
                {

                    ReviewFromDB.CurrentStepId = _workflowRepository.GetById(ReviewFromDB.WorkflowTemplateId).Steps.FirstOrDefault(r => r.StepOrder == NextReviewStep).Id;
                    var FollowingStepUsers = ReviewStepUsersService.GetAll().Where(s => s.StepId == ReviewFromDB.CurrentStepId);

                    foreach (var item in FollowingStepUsers)
                    {
                        item.IsApproved = null;
                    }
                    _reviewRepository.Save();
                }
                else 
                {
                    foreach (var doc in ReviewDocs)
                    {
                        
                        doc.Status = "Approved";
                        ReviewDocumentService.Update(doc);
                    }
                    ReviewDocumentService.Save();

                    var workflow = _workflowRepository.GetById(ReviewFromDB.WorkflowTemplateId);
                    bool copy = workflow.CopyApprovedFiles;
                    ReviewFromDB.CurrentStepId = null;
                    ReviewFromDB.FinalReviewStatus = FinalReviewStatus.Approved;
                    _reviewRepository.Update(ReviewFromDB);
                    _reviewRepository.Save();




                    if (copy == true)
                    {

                        List<ReviewDocument> DocumentList = ReviewFromDB.ReviewDocuments;

                        var CurrentWorkflow = _workflowRepository.GetById(ReviewFromDB.WorkflowTemplateId);
                        int DistId = (int)CurrentWorkflow.DestinationFolderId;

                        foreach (var item in DocumentList)
                        {
                            Document doc = new Document();
                            doc.FolderId = DistId;
                            doc.Name = item.Document.Name;
                            doc.ProjectId = item.Document.ProjectId;
                            doc.CreatedAt = item.Document.CreatedAt;
                            doc.CreatedBy = item.Document.CreatedBy;
                            doc.Versions = new List<DocumentVersion>();
                            doc.FileType = item.Document.FileType;

                            _documentRepository.Insert(doc);
                            _documentRepository.Save();
                        }
                    }
                }
            }
            return RedirectToAction("Index" , new {id = ReviewFromDB.ProjectId , showActive = true});
        }

        public async Task<IActionResult> Reject(int Id)
        {
            Review ReviewFromDB = _reviewRepository.GetById(Id);

            int CurrentReviewStep = (int)_workFlowStepRepo.GetById(ReviewFromDB.CurrentStepId.Value).StepOrder;
            int PreviousReviewStep = CurrentReviewStep - 1;
            int StepsCount = _workflowRepository.GetById(ReviewFromDB.WorkflowTemplateId).Steps.Count();
            var StepMultiReviewerOption = ReviewFromDB.CurrentStep.MultiReviewerOptions;
            var StepReviewerType = ReviewFromDB.CurrentStep.ReviewersType;

            var CurrentUser = await UserManager.GetUserAsync(User);
            var CurrntStep = ReviewFromDB.CurrentStep;
            ReviewStepUsersService.Get(CurrentUser.Id, CurrntStep.Id, ReviewFromDB.Id).IsApproved = false;
            ReviewStepUsersService.Save();


            bool StepBack = true;

            if(StepReviewerType == (ReviewersType)1 && StepMultiReviewerOption == (MultiReviewerOptions)2)
            {
                var StepUsers = ReviewStepUsersService.GetByStepId(CurrntStep.Id, Id);
                int counter = 0;

                foreach (var item in StepUsers)
                {
                    if (item.IsApproved == false)
                    { counter++; }
                }

                

                if (counter != CurrntStep.MinReviewers)
                {
                    StepBack = false;
                }
               
            }

            if(StepBack == true)
            {
                if (PreviousReviewStep > 0)
                {
                    ReviewFromDB.CurrentStepId = _workflowRepository.GetById(ReviewFromDB.WorkflowTemplateId).Steps.FirstOrDefault(r => r.StepOrder == PreviousReviewStep).Id;
                    var PreviousStepUsers = ReviewStepUsersService.GetAll().Where(s => s.StepId == ReviewFromDB.CurrentStepId);

                    foreach (var item in PreviousStepUsers)
                    {
                        item.IsApproved = null;
                    }


                }
                else 
                {
                    ReviewFromDB.CurrentStepId = null;
                    ReviewFromDB.FinalReviewStatus = FinalReviewStatus.Rejected;

                }
            }

           
            _reviewRepository.Save();
            return RedirectToAction("Index", new { id = ReviewFromDB.ProjectId , showActive = true });
        }

        public async Task<IActionResult> Details(int id , bool showActive , bool reviewedByMe , bool startedByMe  , bool showArchived )
        {                                                                     
            var CurrentUser = await UserManager.GetUserAsync(User);            
                                                                              
            var review = _reviewRepository.GetReviewById(id);

            if (review == null)
                return NotFound();

            var reviewDocs = ReviewDocumentService.GetAll().Where(r => r.ReviewId == id);

            foreach(var doc in reviewDocs)
            {
                var rejectedDoc = ReviewDocumentService.WasDocRejectedByThisUser(CurrentUser.Id, doc.ReviewId, doc.DocumentId);
                if (rejectedDoc)
                {
                    doc.Status = "Rejected";
                    ReviewDocumentService.Update(doc);
                }

            }
            ReviewDocumentService.Save();

            var DocumentsListVM = ReviewDocumentService.GetAll().Where(r=>r.ReviewId==id).Select(rd => new DocumentUponAction
            {
                Id = rd.Document.Id,
                Name = rd.Document.Name,
                State = rd.Status
            }).ToList();

            ViewBag.ReviewName = review.Name;
            ViewBag.ReviewId = review.Id;
            ViewBag.CurrentUserId = CurrentUser.Id;
            ViewBag.Initiator = review.InitiatorUserId;
            ViewBag.showActive = showActive;
            ViewBag.reviewedByMe = reviewedByMe;
            ViewBag.startedByMe = startedByMe;
            ViewBag.showArchived =showArchived;


            if (CurrentUser.Id != review.InitiatorUserId && review.CurrentStep != null)
            {
                ViewBag.CurrentStepOrder = review.CurrentStep.StepOrder;
                ViewBag.CanComment = true;
            }
            else
            {
                ViewBag.CanComment = false;
            }


            return View("Details" , DocumentsListVM);
        }

        public async Task<IActionResult> DocumentComments(int documentId , int reviewId , int currentStepOrder)
        {

            var comments = ReviewDocumentService.GetAllComments()
                .Where(c => c.DocumentId == documentId && c.ReviewId==reviewId)  
                .Select(c=>new CommentsVM
                {
                    Comment = c.Comment,
                    ReviewerName = c.Reviewer.UserName,
                    StepOrder = c.StepOrder,
                    CreatedAt = c.CreatedAt.ToString()

                }).ToList();

            var review = _reviewRepository.GetReviewById(reviewId);

            ViewBag.ReviewId = review.Id;
            var CurrentUser = await UserManager.GetUserAsync(User);
            ViewBag.Initiator = review.InitiatorUserId;
            ViewBag.CurrentUserId = CurrentUser.Id;

            ViewBag.DocumentId = documentId;
            ViewBag.StepOrder = currentStepOrder;

            if (CurrentUser.Id != review.InitiatorUserId && review.CurrentStep != null)
            {
                ViewBag.CanComment = true;
            }
            else
            {
                ViewBag.CanComment = false;
            }
            return PartialView("PartialViews/_CommentsPartialView", comments);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int documentId, string comment, int reviewId, int stepOrder)
        {
            var user = await UserManager.GetUserAsync(User);

            var reviewDocComment = new ReviewDocumentComment
            {
                DocumentId = documentId,
                ReviewerId = user.Id,
                ReviewId = reviewId,
                Comment = comment,
                StepOrder = stepOrder
            };

            ReviewDocumentService.InsertComment(reviewDocComment);
            ReviewDocumentService.Save();

            var comments = ReviewDocumentService.GetAllComments()
                .Where(c => c.DocumentId == documentId && c.ReviewId == reviewId)
                .Select(c => new CommentsVM
                {
                    Comment = c.Comment,
                    ReviewerName = c.Reviewer.UserName,
                    StepOrder = c.StepOrder,
                    CreatedAt = c.CreatedAt.ToString()
                }).ToList();

            var CurrentUser = await UserManager.GetUserAsync(User);
            ViewBag.Initiator = _reviewRepository.GetById(reviewId).InitiatorUserId;
            ViewBag.CurrentUserId = CurrentUser.Id;
            var review = _reviewRepository.GetReviewById(reviewId);
            if (CurrentUser.Id != review.InitiatorUserId && review.CurrentStep != null)
            {
                ViewBag.CanComment = true;
            }
            else
            {
                ViewBag.CanComment = false;
            }

            return PartialView("PartialViews/_CommentsPartialView", comments);
        }

        [AcceptVerbs("Get", "Post")]
        public IActionResult VerifyUniqueReviewName(string name, int proId)
        {
            var exists = _reviewRepository.GetAll()
                .Any(r => r.Name.ToLower() == name.ToLower() && r.ProjectId == proId);
            return Json(!exists);
        }

        // Helper method to get the first step ID For Notifications
        private int? GetFirstStepId(int workflowTemplateId)
        {
            // Implement this based on your workflow structure
            // This should return the ID of the first step in the workflow
            var firstStep = _workflowRepository.GetFirstStepByTemplateId(workflowTemplateId);
            return firstStep?.Id;
        }




    }
}

