using DataLayer.Models;
using DataLayer.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ACC.ViewModels.WorkflowVM
{
    public class WorkflowTemplateViewModel
    {

        public int proId { get; set; }
        public int? Id { get; set; }
        [Required(ErrorMessage = "Name is required")]

        [Remote("IsNameUnique", "Workflow", AdditionalFields = "proId", ErrorMessage = "Name already exists for this project")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Description is required")]

        public string Description { get; set; }

        public List<ProjectReviewersVM> Reviewers { get; set; } = new List<ProjectReviewersVM>();

        public string? SelectedAppUserId { get; set; }


        public List<FolderVM>? AllFolders { get; set; }

        
        

        public List<WorkflowStepInputViewModel> Steps { get; set; } = new List<WorkflowStepInputViewModel>();



        public List<ReviewersType>? ReviewersType { get; set; } = Enum.GetValues(typeof(ReviewersType)).Cast<ReviewersType>().ToList();

        public List<ApplicationRole> ProjectPositions { get; set; } = new List<ApplicationRole>();




        public int? SelectedDistFolderId { get; set; }

        public bool CopyApprovedFiles { get; set; } 

    }

}
