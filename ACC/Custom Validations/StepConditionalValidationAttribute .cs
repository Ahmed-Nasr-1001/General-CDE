using System.ComponentModel.DataAnnotations;
using ACC.ViewModels.WorkflowVM;
public class StepConditionalValidationAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var step = (WorkflowStepInputViewModel)validationContext.ObjectInstance;

        // ✅ لازم reviewer type يتحدد
        if (step.SelectedReviewersType == null)
            return new ValidationResult("Reviewer type is required");

        // ✅ Check for Single
        if (step.SelectedReviewersType == DataLayer.Models.Enums.ReviewersType.Single)
        {
            if (step.AssignedUsersIds == null || !step.AssignedUsersIds.Any())
                return new ValidationResult("You must select one reviewer for Single type");

            // Advanced options not required
            return ValidationResult.Success;
        }

        // ✅ Check for Multiple
        if (step.SelectedReviewersType == DataLayer.Models.Enums.ReviewersType.Multiple)
        {
            if (step.AssignedUsersIds == null || !step.AssignedUsersIds.Any())
                return new ValidationResult("You must select at least one reviewer for Multiple type");

            // Either MinReviewers or SelectedOption should be present
            var hasMin = step.MinReviewers.HasValue && step.MinReviewers.Value > 0;
            var hasOption = !string.IsNullOrWhiteSpace(step.SelectedOption);

            if (!hasMin && !hasOption)
                return new ValidationResult("You must specify either a minimum number of reviewers or select an option");

            if (step.SelectedOption?.ToLower() == "min" && !hasMin)
                return new ValidationResult("Please enter the minimum number of reviewers");

            return ValidationResult.Success;
        }

        return ValidationResult.Success;
    }
}
