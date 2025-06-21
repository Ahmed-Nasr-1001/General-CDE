using DataLayer.Models.ClassHelper;
using DataLayer.Models.Enums;

namespace ACC.ViewModels.ProjectMembersVM
{
    public class ProjectMembersVM
    {
        public string Id { get; set; }
        public string? Name { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MobilePhone { get; set; }
        public string Email { get; set; }
        public string? Company { get; set; }
        public string? Position { get; set; }
        public string? ProjectAccessLevel { get; set; }
        public DateTime? AddedOn { get; set; }


    }
}
