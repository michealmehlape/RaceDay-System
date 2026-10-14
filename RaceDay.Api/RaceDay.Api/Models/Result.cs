namespace RaceDay.Api.Models
{
    public class Result
    {
        public int ResultID { get; set; }
        public int EnrolmentID { get; set; }
        public TimeSpan? FinishTime { get; set; }   // maps to SQL TIME
        public int? Position { get; set; }
        public string Status { get; set; } = "Finished";

        public Enrolment Enrolment { get; set; } = null!;
    }
}
