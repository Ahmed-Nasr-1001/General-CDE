namespace ACC.ViewModels.TeamsVM
{
    public class EditMemberPermissionsViewModel
    {
        public int TeamId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public Dictionary<string, string> Permissions { get; set; } = new();
        public List<string> PermissionLevels { get; set; } = new() { "View", "Create", "Delete", "Edit", "Manage" };
    }
}
