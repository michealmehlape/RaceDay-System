namespace RaceDay.Web.ViewModels;

// Used by the page that lists everyone enrolled in one event
public class EventEnrolmentsViewModel
{
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public List<EnrolmentDto> Enrolments { get; set; } = new List<EnrolmentDto>();
    public string? ErrorMessage { get; set; }
}