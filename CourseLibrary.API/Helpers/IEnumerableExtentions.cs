using System.Dynamic;
using System.Reflection;

namespace CourseLibrary.API.Helpers;

public static class IEnumerableExtentions
{
    public static IEnumerable<ExpandoObject> ShapeData<TSource>(
        this IEnumerable<TSource> source,
        string? fields
        )
    {
        if(source == null )
        {
            throw new ArgumentNullException(nameof(source));
        }    

        var expandoObjectList = new List<ExpandoObject>();

        var propertyInfoList = new List<PropertyInfo>();

        if(string.IsNullOrWhiteSpace(fields))
        {
            var propertyName = typeof(TSource)
                .GetProperties(BindingFlags.IgnoreCase |
                BindingFlags.Instance | BindingFlags.Public);

            propertyInfoList.AddRange(propertyName);
        }
        else
        {
            var fieldsToRunThrogh = fields.Split(",");

            foreach( var field in fieldsToRunThrogh)
            {
                var proprtyName = field.Trim();

                var properyInfo = typeof(TSource)
                    .GetProperty(proprtyName, BindingFlags.IgnoreCase | BindingFlags.Public |
                    BindingFlags.Instance);

                if( properyInfo == null ) 
                {
                    throw new Exception($"the property {proprtyName} is not on {typeof(TSource)}");
                }

                propertyInfoList.Add(properyInfo);
            }    
        }
        foreach(TSource sourceObject in source)
        {
            var shapedData = new ExpandoObject();

            foreach(PropertyInfo propertyInfo in propertyInfoList)
            {
                var propertyValue = propertyInfo.GetValue(sourceObject);

                ((IDictionary<string, object?>)shapedData)
                     .Add(propertyInfo.Name, propertyValue);
            }
            expandoObjectList.Add(shapedData);
        }
        return expandoObjectList;
    }
}
