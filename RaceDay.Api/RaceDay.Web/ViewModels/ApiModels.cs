namespace RaceDay.Web.ViewModels;

public class UserDto
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}

public class EventDto
{
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public int OrganiserID { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
}

public class CategoryDto
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentID { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string BibNumber { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ResultDto
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public string BibNumber { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalFinishers { get; set; }

    // Turns 00:52:14 into text for the page
    public string FinishTimeText
    {
        get
        {
            if (FinishTime == null)
            {
                return "-";
            }
            return FinishTime.Value.ToString(@"hh\:mm\:ss");
        }
    }

    // Turns position 47 and 312 finishers into "47th out of 312"
    public string PositionText
    {
        get
        {
            if (Position == null)
            {
                return "-";
            }

            int number = Position.Value;
            string ending = "th";

            // 11, 12 and 13 are always "th", so only check the others
            int lastTwoDigits = number % 100;
            if (lastTwoDigits < 11 || lastTwoDigits > 13)
            {
                if (number % 10 == 1) { ending = "st"; }
                else if (number % 10 == 2) { ending = "nd"; }
                else if (number % 10 == 3) { ending = "rd"; }
            }

            return number + ending + " out of " + TotalFinishers;
        }
    }
    public class OrganiserEventDto : EventDto
    {
        public int CategoryCount { get; set; }
        public int TotalEnrolments { get; set; }
    }
}