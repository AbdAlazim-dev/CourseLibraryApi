namespace CourseLibrary.API.Services
{
    public class PropertyMapping<TSource, TDestination> : IPropertyMapping
    {
        public Dictionary<string, PropertyMappingValue> MappingDictinory { get; private set; }


        public PropertyMapping(Dictionary<string, PropertyMappingValue> mappingDictinory)
        {
            MappingDictinory = mappingDictinory ?? throw new ArgumentNullException(nameof(mappingDictinory));  
        }
    }
}
