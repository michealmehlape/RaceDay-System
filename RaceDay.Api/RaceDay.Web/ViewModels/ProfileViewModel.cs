using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.ViewModels;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100, ErrorMessage = "Your name can be at most 100 characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    // Only shown on the page, it is not edited in this form
    public string? ProfilePictureUrl { get; set; }
}