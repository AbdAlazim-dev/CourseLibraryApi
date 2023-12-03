using System.ComponentModel.DataAnnotations;

namespace CourseLibrary.API.Models
{
    public class AuthorForCreationWithDateOfDeathDto : AutherForCreationDto
    {
        public DateTimeOffset? DateOfDeath { get; set; }
    }
}
