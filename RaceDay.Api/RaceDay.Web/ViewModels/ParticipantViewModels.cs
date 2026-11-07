namespace RaceDay.Web.ViewModels;

public class MyEventsViewModel
{
    public List<EnrolmentDto> Enrolments { get; set; } = new List<EnrolmentDto>();
    public string? ErrorMessage { get; set; }
}

public class MyResultsViewModel
{
    public List<ResultDto> Results { get; set; } = new List<ResultDto>();
    public string? ErrorMessage { get; set; }
}