using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class ResultRequest
{
    public TimeSpan? FinishTime { get; set; }      // sent as "00:52:14"

    [Range(1, 100000)]
    public int? Position { get; set; }

    [Required, RegularExpression("^(Finished|DNF|DNS)$", ErrorMessage = "Status must be Finished, DNF or DNS.")]
    public string Status { get; set; } = "Finished";
}

public class ResultResponse
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string BibNumber { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalFinishers { get; set; }   // for "47th out of 312" in Part 3
}