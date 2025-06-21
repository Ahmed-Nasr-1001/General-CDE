using System.ComponentModel.DataAnnotations;
using DataLayer.Models.Enums;

namespace ACC.ViewModels.WorkflowVM
{
    public class WorkflowStepInputViewModel : IValidatableObject
    {
        public int? StepOrder { get; set; }

        public int TimeAllowedInDays { get; set; }
        public List<string> AssignedUsersIds { get; set; } = new();

        public ReviewersType? SelectedReviewersType { get; set; }

        public string selectedPositionId { get; set; }

        public string? SelectedOption { get; set; }

        public int? MinReviewers { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var nonEmptyUsers = AssignedUsersIds?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();

           

            if (SelectedReviewersType == null)
            {
                yield return new ValidationResult("Reviewer type is required", new[] { nameof(SelectedReviewersType) });
            }

            if (SelectedReviewersType == ReviewersType.Single)
            {
                if (nonEmptyUsers == null || !nonEmptyUsers.Any())
                    yield return new ValidationResult("You must select one reviewer for Single type", new[] { nameof(AssignedUsersIds) });
            }

            else if (SelectedReviewersType == ReviewersType.Multiple)
            {
                if (nonEmptyUsers == null || !nonEmptyUsers.Any())
                    yield return new ValidationResult("You must select at least one reviewer for Multiple type", new[] { nameof(AssignedUsersIds) });


                bool parsed = SelectedOption != null;
                bool hasMin = MinReviewers.HasValue && MinReviewers.Value > 0;

                if (!parsed && !hasMin)
                    yield return new ValidationResult("You must specify either a minimum number of reviewers or select an option", new[] { nameof(SelectedOption), nameof(MinReviewers) });

                if (SelectedOption == "A minimum number of key reviewers must review this step" && !hasMin)
                    yield return new ValidationResult("Please enter the minimum number of reviewers", new[] { nameof(MinReviewers) });
            }
        }
    }
}
