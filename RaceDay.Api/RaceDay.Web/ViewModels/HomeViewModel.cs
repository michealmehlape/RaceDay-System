namespace RaceDay.Web.ViewModels;

public class HomeViewModel
{
    public List<EventDto> Events { get; set; } = new List<EventDto>();
    public string? SelectedType { get; set; }
    public string? ErrorMessage { get; set; }
}