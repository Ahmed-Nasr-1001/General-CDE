namespace ACC.ViewModels.DashboardProjectCardVM
{
    public class DashboardProjectCardVM
    {
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public string ProjectNumber { get; set; }

        public int IssueCount { get; set; }
        public int ReviewCount { get; set; }

        public DateTime? CreationDate { get; set; }
        public bool IsArchived { get; set; }
        public int MembersCount { get; set; }
    }


}
