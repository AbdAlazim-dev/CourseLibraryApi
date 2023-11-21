using System.Reflection;

namespace CourseLibrary.API.Services;

public class PropertyCheckService : IPropertyCheckService
{
    public bool TypeHasProperties<T>(string? fields)
    {
        if (string.IsNullOrEmpty(fields))
        {
            return true;
        }

        var fieldsArray = fields.Split(',');

        foreach (var field in fieldsArray)
        {
            var propertyName = field.Trim();

            var propertyInfo = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase
                | BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null)
            {
                return false;
            }
        }
        return true;
    }
}
