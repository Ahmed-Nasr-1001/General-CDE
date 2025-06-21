using DataLayer.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace ACC.ViewModels.MemberVM.MemberVM
{
    public class InserProjecttMembersVM
    {

        [Required(ErrorMessage = "Member is required")]
        public string? Name { get; set; }  

      
        public string Email { get; set; }
        public int? CompanyId { get; set; }
        [Required(ErrorMessage = "Global access level is required")]
        public string? GlobalAccessLevelId { get; set; }
        [Required(ErrorMessage = "Rule is required")]

        public string? PositionId { get; set; }
        [Required(ErrorMessage = "Project access level is required")]
        public string? ProjectAccessLevelId { get; set; }


        public string? FirstName { get; set; }

    
        public string? LastName { get; set; }

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
