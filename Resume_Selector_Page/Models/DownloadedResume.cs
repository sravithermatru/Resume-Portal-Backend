namespace Resume_Selector_Page.Models
{
    public class DownloadedResume
    {
        public int Id { get; set; }
        public string RecruiterId { get; set; }
        public int ResumeId { get; set; }
        public DateTime DownloadedAt { get; set; }

        public Recruiter Recruiter { get; set; }
        public Resume Resume { get; set; }
    }
}
