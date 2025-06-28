namespace ACC.ViewModels.TeamsVM
{
    public class AddTeamMembersViewModel
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public List<UserSelectionViewModel> ProjectUsers { get; set; }
    }
}
