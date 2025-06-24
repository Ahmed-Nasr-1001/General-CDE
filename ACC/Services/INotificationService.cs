using DataLayer.Models;
using DataLayer.Models.Enums.Notification;

namespace ACC.Services
{
    public interface INotificationService
    {
        Task NotifyReviewCreatedAsync(Review review);
        Task NotifyAllWorkflowUsersAsync(Review review);
        Task NotifyUserAsync(string userId, string title, string message, string? actionUrl = null, NotificationType type = NotificationType.General);
        Task<List<Notification>> GetUserNotificationsAsync(string userId);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteNotificationAsync(int notificationId);


        // NEW: Issue notification methods
        Task NotifyIssueCreatedAsync(Issue issue, int ProjectId);
        Task NotifyIssueUpdatedAsync(Issue issue, string updateMessage);
        Task NotifyIssueStatusChangedAsync(Issue issue, string previousStatus);
        Task NotifyIssueCommentAddedAsync(Issue issue, IssueComment comment);

    }
}
