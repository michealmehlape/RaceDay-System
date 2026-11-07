using Microsoft.AspNetCore.Mvc.Diagnostics;

namespace RaceDay.Web.ViewModels;

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new EventDto();
    public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();

    // The logged-in user's role, or null if nobody is logged in
    public string? Role { get; set; }
}