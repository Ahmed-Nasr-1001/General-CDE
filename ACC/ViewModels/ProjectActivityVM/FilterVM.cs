namespace ACC.ViewModels.ProjectActivityVM
{
    public class FilterVM
    {
        public DateTime? startDate { get; set; }
        public DateTime? endDate { get; set; }

        public string? activityType { get; set; }
        public string? memberName { get; set; }

        public List<string> activityTypesList { get; set; } = new List<string>();
        public List<string> memberNamesList { get; set; } = new List<string>();
    }
}
