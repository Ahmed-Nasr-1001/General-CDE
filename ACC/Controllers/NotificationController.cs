using ACC.Services;
using ACC.ViewModels.NotificationVM;
using DataLayer.Models;
using DataLayer.Models.Enums.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ACC.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        public static int Id;

        public NotificationController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? id)
        {
            Id = id ?? 0; // Set the static Id variable to the provided id or 0 if null
            var userId = _userManager.GetUserId(User);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);

            var viewModel = new NotificationListVM
            {
                Notifications = notifications.Select(n => new NotificationVM
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    ActionUrl = n.ActionUrl,
                    Type = n.Type,
                    Status = n.Status,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt,
                    SenderName = n.Sender?.UserName,
                    ReviewName = n.Review?.Name,
                    FormattedDate = FormatNotificationDate(n.CreatedAt),
                    TypeDisplayName = GetTypeDisplayName(n.Type)
                }).ToList(),
                UnreadCount = unreadCount,
                TotalCount = notifications.Count
            };
            ViewBag.Id = id;
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = _userManager.GetUserId(User);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);

            var notificationVMs = notifications.Take(10).Select(n => new NotificationVM
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                ActionUrl = n.ActionUrl,
                Status = n.Status,
                CreatedAt = n.CreatedAt,
                SenderName = n.Sender?.UserName,
                FormattedDate = FormatNotificationDate(n.CreatedAt),
                TypeDisplayName = GetTypeDisplayName(n.Type)
            }).ToList();
            ViewBag.Id = Id;

            return Json(new
            {
                notifications = notificationVMs,
                unreadCount = unreadCount
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _userManager.GetUserId(User);
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
            ViewBag.Id = Id;

            return Json(new { unreadCount });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // ADDED for AJAX compatibility
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // ADDED for AJAX compatibility
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                await _notificationService.MarkAllAsReadAsync(userId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // ADDED for AJAX compatibility
        public async Task<IActionResult> DeleteNotification(int id)
        {
            try
            {
                await _notificationService.DeleteNotificationAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string FormatNotificationDate(DateTime date)
        {
            var timeSpan = DateTime.Now - date;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";

            return date.ToString("MMM dd, yyyy");
        }

        // In the GetTypeDisplayName method, update to include issue types:

        private string GetTypeDisplayName(NotificationType type)
        {
            return type switch
            {
                NotificationType.ReviewCreated => "Review Created",
                NotificationType.ReviewApproved => "Review Approved",
                NotificationType.ReviewRejected => "Review Rejected",
                NotificationType.ReviewCompleted => "Review Completed",
                // NEW: Issue notification display names
                NotificationType.IssueCreated => "Issue Created",
                NotificationType.IssueAssigned => "Issue Assigned",
                NotificationType.IssueUpdated => "Issue Updated",
                NotificationType.IssueStatusChanged => "Issue Status Changed",
                NotificationType.IssueCommentAdded => "Comment Added",
                _ => "Notification"
            };
        }

    }
}
