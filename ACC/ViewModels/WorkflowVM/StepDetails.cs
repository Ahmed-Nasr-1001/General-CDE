using DataLayer.Models.Enums;

namespace ACC.ViewModels.WorkflowVM
{
    public class StepDetails
    {
        public int? StepOrder { get; set; }

        public int? TimeAllowedInDays { get; set; }
        public List<string>? AssignedUsersNames { get; set; } = new();

        public string? SelectedReviewersType { get; set; }

        public string selectedPosition { get; set; }

        public string? SelectedOption { get; set; }

        public int? MinReviewers { get; set; }
    }
}
