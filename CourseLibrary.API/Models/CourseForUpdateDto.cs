using System.ComponentModel.DataAnnotations;

namespace CourseLibrary.API.Models
{
    public class CourseForUpdateDto : CourseForManibulatinDto
    {
        [Required(ErrorMessage = "You must include a Description of that course when Updating it")]
        public override string Description { get => base.Description;
            set => base.Description = value; }
    }
}
