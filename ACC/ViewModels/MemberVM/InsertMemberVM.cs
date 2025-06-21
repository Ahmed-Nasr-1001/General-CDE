using DataLayer.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ACC.ViewModels.MemberVM
{
    public class InsertMemberVM
    {
        public string? Name { get; set; }  // Optional display name

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Company is required")]
        public int? CompanyId { get; set; }
        [Required(ErrorMessage = "Global access level is required")]
        public string? GlobalAccessLevelId { get; set; }

        public string? PositionId { get; set; }

        public string? ProjectAccessLevelId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name must be less than 100 characters")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name must be less than 100 characters")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Mobile phone is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string? MobilePhone { get; set; }

        public string? currentCompany { get; set; }

        public string? currentGlobalAccessLevelId { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }
}
