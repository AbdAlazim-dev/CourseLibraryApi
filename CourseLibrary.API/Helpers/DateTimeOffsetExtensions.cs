
namespace CourseLibrary.API.Helpers;
public static class DateTimeOffsetExtensions
{
    public static int GetCurrentAge(this DateTimeOffset dateTimeOffset,
        DateTimeOffset? dateOfDeath)
    {
        var currentDate = DateTime.UtcNow;

        if (dateOfDeath != null)
        {
            currentDate = dateOfDeath.Value.UtcDateTime;
        }

        int age = currentDate.Year - dateTimeOffset.Year;

        if (currentDate < dateTimeOffset.AddYears(age))
        {
            age--;
        }

        return age;
    }
}

