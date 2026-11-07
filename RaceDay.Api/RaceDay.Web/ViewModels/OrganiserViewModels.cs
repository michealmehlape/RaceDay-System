using static RaceDay.Web.ViewModels.ResultDto;

namespace RaceDay.Web.ViewModels;

public class OrganiserDashboardViewModel
{
    public List<OrganiserEventDto> Events { get; set; } = new List<OrganiserEventDto>();
    public string? ErrorMessage { get; set; }

    // Numbers for the summary tiles at the top of the page
    public int TotalEvents
    {
        get { return Events.Count; }
    }

    public int UpcomingEvents
    {
        get { return Events.Count(e => e.EventDate >= DateTime.Today); }
    }

    public int TotalEnrolments
    {
        get { return Events.Sum(e => e.TotalEnrolments); }
    }

    // The soonest event that hasn't happened yet (the API sends events oldest first)
    public OrganiserEventDto? NextEvent
    {
        get { return Events.FirstOrDefault(e => e.EventDate >= DateTime.Today); }
    }
}