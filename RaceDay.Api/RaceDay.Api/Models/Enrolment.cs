namespace RaceDay.Api.Models
{
    public class Enrolment
    {
        public int EnrolmentID { get; set; }
        public int ParticipantID { get; set; }
        public int CategoryID { get; set; }
        public string BibNumber { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
        public string Status { get; set; } = "Registered";

        public User Participant { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public Result? Result { get; set; }     // zero or one result
    }
}
