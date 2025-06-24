using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer.Models;
using DataLayer.Models.Enums.Notification;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ACC.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly WorkflowStepsUsersService _workflowStepsUsersService;
        private readonly IWorkflowRepository _workflowRepository;
        // NEW: Add IssueReviewersService for issue notifications
        private readonly IssueReviewersService _issueReviewersService;

        public NotificationService(
            INotificationRepository notificationRepository,
            UserManager<ApplicationUser> userManager,
            WorkflowStepsUsersService workflowStepsUsersService,
            IWorkflowRepository workflowRepository,
            IssueReviewersService issueReviewersService) // NEW PARAMETER
        {
            _notificationRepository = notificationRepository;
            _userManager = userManager;
            _workflowStepsUsersService = workflowStepsUsersService;
            _workflowRepository = workflowRepository;
            _issueReviewersService = issueReviewersService; // NEW DEPENDENCY
        }

        // Existing review notification methods remain the same...
        public async Task NotifyReviewCreatedAsync(Review review)
        {
            try
            {
                var allWorkflowUsers = await GetAllWorkflowAssignedUsersAsync(review);
                var notifications = new List<Notification>();

                foreach (var user in allWorkflowUsers)
                {
                    if (user.Id == review.InitiatorUserId)
                        continue;

                    var notification = new Notification
                    {
                        Title = "New Review Created - Your Participation Required",
                        Message = $"A new review '{review.Name}' has been created and you are assigned as a reviewer in this workflow. Please check your assigned steps.",
                        RecipientId = user.Id,
                        SenderId = review.InitiatorUserId,
                        ReviewId = review.Id,
                        Type = NotificationType.ReviewCreated,
                        ActionUrl = $"/Reviews/Details/{review.Id}",
                        CreatedAt = DateTime.Now,
                        Status = NotificationStatus.Unread
                    };

                    notifications.Add(notification);
                }

                if (notifications.Any())
                {
                    await _notificationRepository.CreateBulkNotificationsAsync(notifications);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send review creation notifications: {ex.Message}", ex);
            }
        }

        public async Task NotifyAllWorkflowUsersAsync(Review review)
        {
            await NotifyReviewCreatedAsync(review);
        }

        public async Task NotifyUserAsync(string userId, string title, string message, string? actionUrl = null, NotificationType type = NotificationType.General)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                RecipientId = userId,
                ActionUrl = actionUrl,
                Type = type,
                CreatedAt = DateTime.Now,
                Status = NotificationStatus.Unread
            };

            await _notificationRepository.CreateNotificationAsync(notification);
        }

        // NEW: Issue notification methods
        public async Task NotifyIssueCreatedAsync(Issue issue, int ProjectId)
        {
            try
            {
                var assignedReviewers = await GetIssueAssignedReviewersAsync(issue);
                var notifications = new List<Notification>();

                foreach (var reviewer in assignedReviewers)
                {
                    // Skip the initiator to avoid self-notification
                    if (reviewer.Id == issue.InitiatorID)
                        continue;

                    var notification = new Notification
                    {
                        Title = "New Issue Assigned - Review Required",
                        Message = $"A new issue '{issue.Title}' has been created and assigned to you for review. Priority: {issue.Priority}",
                        RecipientId = reviewer.Id,
                        SenderId = issue.InitiatorID,
                        IssueId = issue.Id,
                        Type = NotificationType.IssueCreated,
                        ActionUrl = $"/ProjectIssue/Index/{ProjectId}",
                        CreatedAt = DateTime.Now,
                        Status = NotificationStatus.Unread
                    };

                    notifications.Add(notification);
                }

                if (notifications.Any())
                {
                    await _notificationRepository.CreateBulkNotificationsAsync(notifications);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send issue creation notifications: {ex.Message}", ex);
            }
        }

        public async Task NotifyIssueUpdatedAsync(Issue issue, string updateMessage)
        {
            try
            {
                var assignedReviewers = await GetIssueAssignedReviewersAsync(issue);
                var notifications = new List<Notification>();

                foreach (var reviewer in assignedReviewers)
                {
                    // Skip the person who made the update
                    if (reviewer.Id == issue.InitiatorID)
                        continue;

                    var notification = new Notification
                    {
                        Title = "Issue Updated",
                        Message = $"Issue '{issue.Title}' has been updated. {updateMessage}",
                        RecipientId = reviewer.Id,
                        SenderId = issue.InitiatorID,
                        IssueId = issue.Id,
                        Type = NotificationType.IssueUpdated,
                        ActionUrl = $"/ProjectIssue/Details/{issue.Id}",
                        CreatedAt = DateTime.Now,
                        Status = NotificationStatus.Unread
                    };

                    notifications.Add(notification);
                }

                if (notifications.Any())
                {
                    await _notificationRepository.CreateBulkNotificationsAsync(notifications);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send issue update notifications: {ex.Message}", ex);
            }
        }

        public async Task NotifyIssueStatusChangedAsync(Issue issue, string previousStatus)
        {
            try
            {
                var assignedReviewers = await GetIssueAssignedReviewersAsync(issue);
                var notifications = new List<Notification>();

                foreach (var reviewer in assignedReviewers)
                {
                    var notification = new Notification
                    {
                        Title = "Issue Status Changed",
                        Message = $"Issue '{issue.Title}' status changed from {previousStatus} to {issue.Status}",
                        RecipientId = reviewer.Id,
                        SenderId = issue.InitiatorID,
                        IssueId = issue.Id,
                        Type = NotificationType.IssueStatusChanged,
                        ActionUrl = $"/ProjectIssue/Details/{issue.Id}",
                        CreatedAt = DateTime.Now,
                        Status = NotificationStatus.Unread
                    };

                    notifications.Add(notification);
                }

                if (notifications.Any())
                {
                    await _notificationRepository.CreateBulkNotificationsAsync(notifications);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send issue status change notifications: {ex.Message}", ex);
            }
        }

        public async Task NotifyIssueCommentAddedAsync(Issue issue, IssueComment comment)
        {
            try
            {
                var assignedReviewers = await GetIssueAssignedReviewersAsync(issue);
                var notifications = new List<Notification>();

                foreach (var reviewer in assignedReviewers)
                {
                    // Skip the comment author
                    if (reviewer.Id == comment.AuthorId)
                        continue;

                    var notification = new Notification
                    {
                        Title = "New Comment on Issue",
                        Message = $"A new comment has been added to issue '{issue.Title}' by {comment.Author?.UserName ?? "Unknown"}",
                        RecipientId = reviewer.Id,
                        SenderId = comment.AuthorId,
                        IssueId = issue.Id,
                        Type = NotificationType.IssueCommentAdded,
                        ActionUrl = $"/ProjectIssue/Details/{issue.Id}",
                        CreatedAt = DateTime.Now,
                        Status = NotificationStatus.Unread
                    };

                    notifications.Add(notification);
                }

                if (notifications.Any())
                {
                    await _notificationRepository.CreateBulkNotificationsAsync(notifications);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send issue comment notifications: {ex.Message}", ex);
            }
        }

        // NEW: Helper method to get issue assigned reviewers
        private async Task<List<ApplicationUser>> GetIssueAssignedReviewersAsync(Issue issue)
        {
            try
            {
                var issueReviewers = _issueReviewersService.GetAll()
                    .Where(ir => ir.IssueId == issue.Id)
                    .ToList();

                var reviewers = new List<ApplicationUser>();

                foreach (var issueReviewer in issueReviewers)
                {
                    var user = await _userManager.FindByIdAsync(issueReviewer.ReviewerId);
                    if (user != null)
                    {
                        reviewers.Add(user);
                    }
                }

                return reviewers;
            }
            catch (Exception ex)
            {
                return new List<ApplicationUser>();
            }
        }

        // Existing methods remain the same...
        public async Task<List<Notification>> GetUserNotificationsAsync(string userId)
        {
            return await _notificationRepository.GetUserNotificationsAsync(userId);
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _notificationRepository.GetUnreadCountAsync(userId);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            await _notificationRepository.MarkAsReadAsync(notificationId);
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            await _notificationRepository.MarkAllAsReadAsync(userId);
        }

        public async Task DeleteNotificationAsync(int notificationId)
        {
            var notification = _notificationRepository.GetById(notificationId);
            if (notification != null)
            {
                _notificationRepository.Delete(notification);
                _notificationRepository.Save();
            }
        }

        // Existing workflow helper methods remain the same...
        private async Task<List<ApplicationUser>> GetAllWorkflowAssignedUsersAsync(Review review)
        {
            try
            {
                var workflowTemplate = _workflowRepository.GetById(review.WorkflowTemplateId);

                if (workflowTemplate?.Steps == null || !workflowTemplate.Steps.Any())
                {
                    return new List<ApplicationUser>();
                }

                var allUsers = new List<ApplicationUser>();
                var addedUserIds = new HashSet<string>();

                foreach (var step in workflowTemplate.Steps)
                {
                    var stepUsers = _workflowStepsUsersService.GetByStepId(step.Id);

                    foreach (var stepUser in stepUsers)
                    {
                        if (!addedUserIds.Contains(stepUser.UserId))
                        {
                            var user = await _userManager.FindByIdAsync(stepUser.UserId);
                            if (user != null)
                            {
                                allUsers.Add(user);
                                addedUserIds.Add(stepUser.UserId);
                            }
                        }
                    }
                }

                return allUsers;
            }
            catch (Exception ex)
            {
                return new List<ApplicationUser>();
            }
        }

        private async Task<List<ApplicationUser>> GetCurrentStepAssignedUsersAsync(Review review)
        {
            if (!review.CurrentStepId.HasValue)
                return new List<ApplicationUser>();

            var stepUsers = _workflowStepsUsersService.GetByStepId(review.CurrentStepId.Value);
            var users = new List<ApplicationUser>();

            foreach (var stepUser in stepUsers)
            {
                var user = await _userManager.FindByIdAsync(stepUser.UserId);
                if (user != null)
                {
                    users.Add(user);
                }
            }

            return users;
        }
    }
}
