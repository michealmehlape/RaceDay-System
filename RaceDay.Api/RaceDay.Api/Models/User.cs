namespace RaceDay.Api.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Participant";
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public List<Event> OrganisedEvents { get; set; } = new();
        public List<EventOrganiser> EventOrganisers { get; set; } = new();
        public List<Enrolment> Enrolments { get; set; } = new();
    }
}
