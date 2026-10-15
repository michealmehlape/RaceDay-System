using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CategoryRequest
{
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    // decimal(5,2) in the database, so the maximum is 999.99
    [Range(0.01, 999.99)]
    public decimal DistanceKM { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; }

    // decimal(8,2) in the database
    [Range(0, 999999.99)]
    public decimal EntryFee { get; set; }
}

public class CategoryResponse
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}