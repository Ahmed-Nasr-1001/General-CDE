namespace ACC.ViewModels.TeamsVM
{
    public class AddTeamMembersPostModel
    {
        public int TeamId { get; set; }
        public List<UserSelectionViewModel> SelectedUsers { get; set; }
    }
}
