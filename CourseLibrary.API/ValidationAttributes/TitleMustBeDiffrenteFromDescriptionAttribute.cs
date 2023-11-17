using CourseLibrary.API.Models;
using System.ComponentModel.DataAnnotations;

namespace CourseLibrary.API.ValidationAttributes;

public class TitleMustBeDiffrenteFromDescriptionAttribute : ValidationAttribute
{
    public TitleMustBeDiffrenteFromDescriptionAttribute()
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if(validationContext.ObjectInstance is not CourseForManibulatinDto course)
        {
            throw new Exception($"The Attribute" +
                $" {nameof(TitleMustBeDiffrenteFromDescriptionAttribute)} is have to be on " +
                $"{nameof(CourseForManibulatinDto)}");
        }
        if(course.Title == course.Description)
        {
            return new ValidationResult("The Title privided have to be different from the Description");
        }

        return ValidationResult.Success;
        
    }

}
