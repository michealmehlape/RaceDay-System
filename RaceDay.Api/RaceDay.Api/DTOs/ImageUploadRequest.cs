using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

// The form a user fills in to upload an image
public class ImageUploadRequest
{
    /// <summary>The image file (JPG, PNG or WEBP).</summary>
    [Required]
    public IFormFile File { get; set; } = null!;
}