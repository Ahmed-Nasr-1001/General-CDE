using DataLayer.Models.Enums.Notification;

namespace ACC.ViewModels.NotificationVM
{
    public class NotificationVM
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string? ActionUrl { get; set; }
        public NotificationType Type { get; set; }
        public NotificationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? SenderName { get; set; }
        public string? ReviewName { get; set; }
        public string? WorkflowName { get; set; } // NEW: Workflow template name
        public string FormattedDate { get; set; }
        public string TypeDisplayName { get; set; }
    }

    public class NotificationListVM
    {
        public List<NotificationVM> Notifications { get; set; } = new List<NotificationVM>();
        public int UnreadCount { get; set; }
        public int TotalCount { get; set; }
        public string? FilterType { get; set; } // NEW: For filtering notifications
    }

    public class NotificationCountVM
    {
        public int UnreadCount { get; set; }
        public int TotalCount { get; set; }
    }
}
