using DataLayer.Models;
using DataLayer.Models.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ACC.ViewModels
{
    public class ProjectIssueVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100, ErrorMessage = "Title must be under 100 characters.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(1000, ErrorMessage = "Description must be under 1000 characters.")]
        [MinLength(10 , ErrorMessage = "Description must be over 10 characters.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public IssueCategory Category { get; set; }

        [Required(ErrorMessage = "Type is required.")]
        public IssueType Type { get; set; }

        [Required(ErrorMessage = "Priority is required.")]
        public IssuePriority Priority { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public IssueStatus Status { get; set; }

        [Required(ErrorMessage = "Project ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Project ID must be greater than 0.")]
        public int ProjectId { get; set; }

        public IFormFile Attachment { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? FilePath { get; set; }

        public string? ScreenshotPath { get; set; }

        public int? FileId { get; set; }

        public int? DocumentId { get; set; }

        public string InitiatorId { get; set; }

        public List<ApplicationUser>? applicationUsers { get; set; }

        [Required(ErrorMessage = "At least one reviewer must be selected.")]
        public List<string>? SelectedReviewerIds { get; set; }
    }
}
