using DataLayer.Models;
using DataLayer.Models.Enums;
using Microsoft.AspNetCore.Http;
namespace ACC.ViewModels
{
    public class ProjectIssueVM
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public IssueCategory Category { get; set; }
        public IssueType Type { get; set; }
        public IssuePriority Priority { get; set; }
        public IssueStatus Status { get; set; }
        public int ProjectId { get; set; }
        public IFormFile Attachment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? FilePath { get; set; }

        public string? ScreenshotPath { get; set; }
        public int? FileId { get; set; }

        public int? DocumentId { get; set; }

        public string InitiatorId { get; set; }

        public List<ApplicationUser>? applicationUsers { get; set; }
        public List<string>? SelectedReviewerIds { get; set; } 


    }
}