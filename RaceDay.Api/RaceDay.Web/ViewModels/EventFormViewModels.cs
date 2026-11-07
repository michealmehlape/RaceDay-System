using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.ViewModels;

// The fields shared by the Create and Edit pages
public class EventFormViewModel
{
    public int EventID { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(150, ErrorMessage = "The event name can be at most 150 characters.")]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "The description can be at most 500 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Event date")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    [StringLength(150, ErrorMessage = "The location can be at most 150 characters.")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose the event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";
}

// The Create page also asks for the event's first category
public class CreateEventViewModel : EventFormViewModel
{
    [Required(ErrorMessage = "Please enter the main distance.")]
    [Range(0.01, 999.99, ErrorMessage = "The distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Main distance (km)")]
    public decimal? DistanceKM { get; set; }

    [Required(ErrorMessage = "Please enter the maximum number of participants.")]
    [Range(1, 100000, ErrorMessage = "Participants must be between 1 and 100000.")]
    [Display(Name = "Maximum participants")]
    public int? MaxParticipants { get; set; }

    [Required(ErrorMessage = "Please enter the entry fee (0 if it is free).")]
    [Range(0, 999999.99, ErrorMessage = "The entry fee must be between 0 and 999999.99.")]
    [Display(Name = "Entry fee (R)")]
    public decimal? EntryFee { get; set; }
}