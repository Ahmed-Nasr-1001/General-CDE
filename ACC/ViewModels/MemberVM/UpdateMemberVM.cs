using DataLayer.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace ACC.ViewModels.MemberVM
{
    public class UpdateMemberVM
    {
        public string? UserName { get; set; }
        [Required]
        [EmailAddress]
        public string? email {  get; set; }
        public int? companyId { get; set; }
        public string? globalAccessLevelID { get; set; }
        public string? positionID { get; set; }
        public string? projectAccessLevelID { get; set; }
     

    }
}
