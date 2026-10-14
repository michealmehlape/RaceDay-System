namespace RaceDay.Api.Models
{
    public class EventOrganiser
    {
        public int EventOrganiserID { get; set; }
        public int EventID { get; set; }
        public int UserID { get; set; }
        public string AssignedRole { get; set; } = "Assistant";

        public Event Event { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
