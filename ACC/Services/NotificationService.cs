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
        private readonly IWorkflowRepository _workflowRepository; // NEW DEPENDENCY

        public NotificationService(
            INotificationRepository notificationRepository,
            UserManager<ApplicationUser> userManager,
            WorkflowStepsUsersService workflowStepsUsersService,
            IWorkflowRepository workflowRepository) // NEW PARAMETER
        {
            _notificationRepository = notificationRepository;
            _userManager = userManager;
            _workflowStepsUsersService = workflowStepsUsersService;
            _workflowRepository = workflowRepository;
        }

        // UPDATED: Now notifies ALL workflow users, not just current step
        public async Task NotifyReviewCreatedAsync(Review review)
        {
            try
            {
                // Get ALL users assigned to ALL steps of the workflow
                var allWorkflowUsers = await GetAllWorkflowAssignedUsersAsync(review);

                var notifications = new List<Notification>();

                foreach (var user in allWorkflowUsers)
                {
                    // Skip the initiator to avoid self-notification
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
                // Log the exception
                throw new Exception($"Failed to send review creation notifications: {ex.Message}", ex);
            }
        }

        // NEW METHOD: Alternative notification method for all workflow users
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

        // COMPLETELY REWRITTEN: Now gets users from ALL workflow steps
        private async Task<List<ApplicationUser>> GetAllWorkflowAssignedUsersAsync(Review review)
        {
            try
            {
                // Get the complete workflow template with all its steps
                var workflowTemplate = _workflowRepository.GetById(review.WorkflowTemplateId);

                if (workflowTemplate?.Steps == null || !workflowTemplate.Steps.Any())
                {
                    return new List<ApplicationUser>();
                }

                var allUsers = new List<ApplicationUser>();
                var addedUserIds = new HashSet<string>(); // Prevent duplicates

                // Iterate through ALL steps in the workflow
                foreach (var step in workflowTemplate.Steps)
                {
                    // Get all users assigned to this specific step
                    var stepUsers = _workflowStepsUsersService.GetByStepId(step.Id);

                    foreach (var stepUser in stepUsers)
                    {
                        // Only add if we haven't already added this user
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
                // Log the exception
                return new List<ApplicationUser>();
            }
        }

        // ALTERNATIVE METHOD: Get users from current step only (for step-specific notifications)
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
