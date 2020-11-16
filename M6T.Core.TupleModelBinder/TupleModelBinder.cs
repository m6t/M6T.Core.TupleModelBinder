using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
namespace M6T.Core.TupleModelBinder
{
    public class TupleModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var modelType = context.Metadata.ModelType;

            if (typeof(ITuple).IsAssignableFrom(modelType) && modelType.Name.StartsWith("ValueTuple"))
            {
                return new BinderTypeModelBinder(typeof(TupleModelBinder));
            }

            return null;
        }
    }

    public class TupleModelBinder : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var modelName = bindingContext.ModelName;

            var reader = new StreamReader(bindingContext.HttpContext.Request.Body);

            var body = await reader.ReadToEndAsync();

            var modelAttributes = bindingContext.ModelMetadata.GetType().GetProperty("Attributes").GetValue(bindingContext.ModelMetadata) as ModelAttributes;

            var tupleAttr = modelAttributes.Attributes.OfType<TupleElementNamesAttribute>().FirstOrDefault();
            if (tupleAttr == null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }
            else
            {
                var tupleType = bindingContext.ModelType;
                object tuple = ParseTupleFromModelAttributes(body, tupleAttr, tupleType);
                bindingContext.Result = ModelBindingResult.Success(tuple);
                return;
            }
        }

        public static object ParseTupleFromModelAttributes(string body, TupleElementNamesAttribute tupleAttr, Type tupleType)
        {
            var jobj = JObject.Parse(body);
            var parameters = tupleAttr.TransformNames.Zip(tupleType.GetConstructors()
                    .Single()
                    .GetParameters())
                .Select(x => GetValue(jobj, x.First, x.Second))
                .ToArray();

            object tuple = Activator.CreateInstance(tupleType, parameters);

            return tuple;
        }

        static object GetValue(JObject jobject, string name, ParameterInfo info)
        {
            var value = jobject.GetValue(name, StringComparison.CurrentCultureIgnoreCase);

            if (value == null || value.Type == JTokenType.Null)
            {
                if (IsNullable(info.ParameterType))
                    return Convert.ChangeType(null, info.ParameterType);
                else
                    return Activator.CreateInstance(info.ParameterType); //default value
            }

            /*
             *  If parameter type is guid, special handling is required since NewtonSoft.Json doesnt handle it correctly for some reason.
             *  If there is another possible input type besides string for guid please add it. 
             *  This currently supports all Guid format types stated in
             *  https://docs.microsoft.com/en-us/dotnet/api/system.guid.tostring?view=net-5.0
             */
            if (info.ParameterType == typeof(Guid) && value.Type == JTokenType.String)
            {
                return Guid.Parse(value.ToString());
            }

            if (info.ParameterType.IsPrimitive || info.ParameterType == typeof(string) || info.ParameterType == typeof(decimal))
                return Convert.ChangeType(value, info.ParameterType);
            else
                return Convert.ChangeType(JsonConvert.DeserializeObject(value.ToString(), info.ParameterType), info.ParameterType);
        }

        static bool IsNullable(Type type)
        {
            if (!type.IsValueType) return true; // ref-type
            if (Nullable.GetUnderlyingType(type) != null) return true; // Nullable<T>
            return false; // value-type
        }
    }
}
