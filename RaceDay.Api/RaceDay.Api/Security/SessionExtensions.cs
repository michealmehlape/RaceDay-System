namespace RaceDay.Api.Security;

public static class SessionExtensions
{
    public static int GetCurrentUserId(this ISession session) =>
        session.GetInt32("UserId") ?? throw new InvalidOperationException("No user in session.");
}