using ACC.ViewModels.IssueCommentVM;
using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ACC.Controllers
{
    [Authorize]
    public class IssueCommentController : Controller
    {
        private readonly IIssueCommentRepository _commentRepo;
        private readonly IIssueNotificationRepository _notificationRepo;
        private readonly IIssueRepository _issueRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public IssueCommentController(
            IIssueCommentRepository commentRepo,
            IIssueNotificationRepository notificationRepo,
            IIssueRepository issueRepo,
            UserManager<ApplicationUser> userManager)
        {
            _commentRepo = commentRepo;
            _notificationRepo = notificationRepo;
            _issueRepo = issueRepo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int issueId)
        {
            var user = await _userManager.GetUserAsync(User);

           
            var unreadNotifications = await _notificationRepo
                .GetUnreadByIssueAndUserAsync(issueId, user.Id);

            foreach (var note in unreadNotifications)
            {
                note.IsRead = true;
            }

            _notificationRepo.Save(); 

            
            var issue = await _issueRepo.GetByIdAsync(issueId);
            ViewBag.ProjectId = issue?.ProjectId;
            ViewBag.IssueId = issueId;

            var comments = await _commentRepo.GetCommentsByIssueIdAsync(issueId);

            var viewModel = comments.Select(c => new IssueCommentVM
            {
                Id = c.Id,
                IssueId = c.IssueId,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                AuthorName = c.Author?.UserName ?? "User"
            }).ToList();

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> AddComment(int issueId, string content)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (string.IsNullOrWhiteSpace(content))
                    return RedirectToAction("Index", new { issueId });

                var comment = new IssueComment
                {
                    IssueId = issueId,
                    AuthorId = user.Id,
                    Content = content,
                    CreatedAt = DateTime.UtcNow
                };

                _commentRepo.Insert(comment);
                _commentRepo.Save();

                // ✅ 1. Get full issue with reviewers and initiator
                var issue = await _issueRepo.GetByIdAsync(issueId); // already includes reviewers

                var recipients = new List<string>();

                // ✅ 2. Add initiator if not the sender
                if (issue.InitiatorID != user.Id)
                    recipients.Add(issue.InitiatorID);

                // ✅ 3. Add all reviewers except the sender
                var reviewerIds = issue.IssueReviwers?
                    .Where(r => r.ReviewerId != user.Id)
                    .Select(r => r.ReviewerId)
                    .Distinct()
                    .ToList();

                if (reviewerIds != null)
                    recipients.AddRange(reviewerIds);

                // ✅ 4. Add notifications
                foreach (var receiverId in recipients.Distinct())
                {
                    var notification = new IssueNotification
                    {
                        ReceiverId = receiverId,
                        IssueId = issueId,
                        Message = $"{user.UserName} added a comment on Issue #{issueId}",
                        IsRead = false
                    };
                    _notificationRepo.Insert(notification);
                }

                _notificationRepo.Save();

                return RedirectToAction("Index", new { issueId });
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while saving the comment.";
                return RedirectToAction("Index", new { issueId });
            }
        }

    }
}
