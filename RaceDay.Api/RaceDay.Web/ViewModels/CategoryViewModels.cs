using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.ViewModels;

// Used by the page that lists one event's categories
public class CategoriesViewModel
{
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    public string? ErrorMessage { get; set; }
}

// Used by the Add and Edit category forms
public class CategoryFormViewModel
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the category name.")]
    [StringLength(100, ErrorMessage = "The name can be at most 100 characters.")]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance.")]
    [Range(0.01, 999.99, ErrorMessage = "The distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
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