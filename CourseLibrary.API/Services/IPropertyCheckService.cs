namespace CourseLibrary.API.Services
{
    public interface IPropertyCheckService
    {
        bool TypeHasProperties<T>(string? fields);
    }
}