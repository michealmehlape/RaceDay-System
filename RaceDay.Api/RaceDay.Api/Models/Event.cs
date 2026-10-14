namespace RaceDay.Api.Models
{
    public class Event
    {
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string EventType { get; set; } = "Run";   // Run, Walk or Cycle (the new Part 2 column)
        public string? BannerImageUrl { get; set; }
        public int OrganiserID { get; set; }
        public DateTime CreatedAt { get; set; }

        public User Organiser { get; set; } = null!;
        public List<Category> Categories { get; set; } = new();
        public List<EventOrganiser> EventOrganisers { get; set; } = new();
    }
}
