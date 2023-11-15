using AutoMapper.Execution;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel;
using System.Reflection;

namespace CourseLibrary.API.Helpers
{
    public class ArrayModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            //our binder only works with IEnumerable type
            if (!bindingContext.ModelMetadata.IsEnumerableType)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }
            //Get the inputted values through the value provider
            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName)
                .ToString();

            //Check if there is no inputted value and return null if true
            if(string.IsNullOrEmpty(value))
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }
            //Get the Ienumberable's type and converter
            var elementType = bindingContext.ModelType.GetTypeInfo()
                .GenericTypeArguments[0];

            var converter = TypeDescriptor.GetConverter(elementType);

            //Convert each value on the value list to Ienumberable Type

            var values = value.Split(new[] { "," },
                StringSplitOptions.RemoveEmptyEntries)
                .Select(x => converter.ConvertFromString(x.Trim()))
                .ToArray();

            //Create an array of this type

            var TypedValyes = Array.CreateInstance(elementType, values.Length);
            values.CopyTo(TypedValyes, 0);
            bindingContext.Model = TypedValyes;

            bindingContext.Result = ModelBindingResult.Success(bindingContext.Model);
            return Task.CompletedTask;


        }
    }
}
