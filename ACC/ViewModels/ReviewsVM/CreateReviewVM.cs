using ACC.ViewModels.WorkflowVM;
using DataLayer.Models;
using DataLayer.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ACC.ViewModels.ReviewsVM
{
    public class CreateReviewVM
    {
        
        public int proId { get; set; }
        public int Id { get; set; }

        [Required(ErrorMessage = "Review name is required")]
        [StringLength(100, ErrorMessage = "Review name cannot exceed 100 characters")]
        [Remote("VerifyUniqueReviewName", "Reviews", AdditionalFields = "proId", ErrorMessage = "A review with this name already exists in this project")]

        public string Name { get; set; }

        [Required(ErrorMessage = "Workflow selection is required")]
        [Display(Name = "Workflow")]
        public int SelectedWorkflowId { get; set; }

        public FinalReviewStatus SelectedFinalReviewStatus { get; set; }

        public List<WorkflowTemplate> WorkflowTemplates { get; set; }
        public List<FinalReviewStatus> FinalReviewStatuses { get; set; }

        public List<FolderWithDocumentsVM>? AllFolders { get; set; }

        public List<int>? SelectedFolderIds { get; set; }

        [Required(ErrorMessage = "Please select at least one document")]
        [Display(Name = "Selected Documents")]
        public List<int>? SelectedDocumentIds { get; set; }
    }
}
