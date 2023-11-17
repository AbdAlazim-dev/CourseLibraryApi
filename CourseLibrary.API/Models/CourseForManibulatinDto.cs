using CourseLibrary.API.ValidationAttributes;
using System.ComponentModel.DataAnnotations;

namespace CourseLibrary.API.Models
{
    [TitleMustBeDiffrenteFromDescription]
    public abstract class CourseForManibulatinDto //: IValidatableObject
    {
        [Required(ErrorMessage = "You have to fill the title")]
        [MaxLength(100, ErrorMessage = "The max length of the title is 100")]
        public string Title { get; set; } = string.Empty;
        [MaxLength(1500, ErrorMessage = "The max length of the title is 1500")]
        public virtual string Description { get; set; } = string.Empty;


    }
}
