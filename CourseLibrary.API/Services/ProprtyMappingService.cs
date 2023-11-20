using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;

namespace CourseLibrary.API.Services
{
    public class ProprtyMappingService : IProprtyMappingService
    {
        private readonly Dictionary<string, PropertyMappingValue> _autherPropertyMapping =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "Id", new(new[] { "Id" }) },
                { "MainCategory", new(new[] { "MainCategory" }) },
                { "Age", new(new[] { "DateOfBirth" }, true) },
                { "Name", new(new[] { "FirstName", "LastName" }) }
            };
        private readonly IList<IPropertyMapping> _proprtyMappings = new List<IPropertyMapping>();

        public ProprtyMappingService()
        {
            _proprtyMappings.Add(new PropertyMapping<AuthorDto, Author>(_autherPropertyMapping));
        }
        public Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>()
        {
            var machingMapping = _proprtyMappings.OfType<PropertyMapping<TSource, TDestination>>();

            if (machingMapping.Count() == 1)
            {
                return machingMapping.First().MappingDictinory;
            }

            throw new Exception($"Cannot find exact property mapping instance " +
                $"for <{typeof(TSource)}, {typeof(TDestination)} ."
                );
        }
        public bool ValidMappingExistsFor<TSource, TDestination>(string fields)
        {
            var propertyMapping = GetPropertyMapping<TSource, TDestination>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                return true;
            }

            // the string is separated by ",", so we split it.
            var fieldsAfterSplit = fields.Split(',');

            // run through the fields clauses
            foreach (var field in fieldsAfterSplit)
            {
                // trim
                var trimmedField = field.Trim();

                // remove everything after the first " " - if the fields 
                // are coming from an orderBy string, this part must be 
                // ignored
                var indexOfFirstSpace = trimmedField.IndexOf(" ");
                var propertyName = indexOfFirstSpace == -1 ?
                    trimmedField : trimmedField.Remove(indexOfFirstSpace);

                // find the matching property
                if (!propertyMapping.ContainsKey(propertyName))
                {
                    return false;
                }
            }
            return true;
        }


    }
}
