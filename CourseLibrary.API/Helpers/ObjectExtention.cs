using System.Dynamic;
using System.Reflection;

namespace CourseLibrary.API.Helpers;

public static class ObjectExtention
{
    public static ExpandoObject ShapeData<TSource>(
        this TSource source,
        string fields)
    {
        if(source == null)
        {
            throw new ArgumentNullException(nameof(TSource));  
        }

        var expandoObjectToReturn = new ExpandoObject();

        if(string.IsNullOrEmpty(fields))
        {
            var proprtiesName = typeof(TSource)
                .GetProperties(BindingFlags.IgnoreCase
                | BindingFlags.Public | BindingFlags.Instance);

            foreach(var proprty in proprtiesName)
            {
                var proprtyName = proprty.Name;

                ((IDictionary<string, object?>)expandoObjectToReturn)
                    .Add(proprtyName, proprty.GetValue(source));
            }
            return expandoObjectToReturn;
        }
        else
        {
            var fieldsArray = fields.Split(',');

            foreach( var field in fieldsArray)
            {
                var proprtyName = field.Trim();

                var proprtyInfo = typeof(TSource)
                    .GetProperty(proprtyName, BindingFlags.IgnoreCase 
                    | BindingFlags.Public | BindingFlags.Instance);

                if(proprtyInfo == null) 
                {
                    throw new Exception($"the proprty {proprtyName} is not proprty on {typeof(TSource)}");
                }

                var propertyValue = proprtyInfo.GetValue(source);

                ((IDictionary<string, object?>)expandoObjectToReturn)
                    .Add(proprtyInfo.Name, propertyValue);

                
            }
            return expandoObjectToReturn;

        }
    }
}
