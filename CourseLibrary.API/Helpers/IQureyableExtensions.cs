using CourseLibrary.API.Services;
using System.Linq.Dynamic.Core;

namespace CourseLibrary.API.Helpers;

public static class IQureyableExtensions
{
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> source,
        string orderBy,
        Dictionary<string, PropertyMappingValue> mappingDictionary)
    {
        if(source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        if(mappingDictionary == null)
        {
            throw new ArgumentNullException(nameof(mappingDictionary));
        }
        if(string.IsNullOrEmpty(orderBy))
        {
            return source;
        }

        var orderByString = string.Empty;

        // the orderBy string is sparated by "," so we split it.
        var orederByAfterSplit = orderBy.Split(',');

        foreach( var orderByClause in orederByAfterSplit)
        {
            //trim the orderBy Clause, as it might contain leading
            // or trailing spaces. Cant trim the var in foreach
            // so use another var.
            var trimmedOrderByClause = orderByClause.Trim();

            //if the sort options ends with "desc", we order
            //descending, otherwise ascending
            var orderDescending = trimmedOrderByClause.EndsWith(" desc");

            //remove " asc" or " desc" from the orderBy clause, so
            // we get the propery name to look for in the mapping dicionary
            var indexOfFirstSpace = trimmedOrderByClause.IndexOf(" "); ;
            var propertyName = indexOfFirstSpace == -1 ?
                trimmedOrderByClause : trimmedOrderByClause
                .Remove(indexOfFirstSpace);

            //find the matching property
            if(!mappingDictionary.ContainsKey(propertyName))
            {
                throw new ArgumentNullException($"Key mapping for {propertyName} is mis");
            }

            //get the PropertyMappingValue 
            var propertyMappingValue = mappingDictionary[propertyName];

            if(propertyMappingValue == null)
            {
                throw new ArgumentNullException(nameof(propertyMappingValue));
            }
            //revert sort order if nacessary
            if (propertyMappingValue.Revert)
            {
                orderDescending = !orderDescending;
            }
            //run through the property names 
            foreach(var destinationProperty in 
                propertyMappingValue.DestinationProperties)
            {
                orderByString = orderByString +
                    (string.IsNullOrWhiteSpace(orderByString) ? string.Empty : ", ")
                    + destinationProperty
                    + (orderDescending ? " descending" : " ascending");
            }

        }

        return source.OrderBy(orderByString);
    }
}
