using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            object tuple = Activator.CreateInstance(tupleType);
            int itemIndex = 1;
            var jobj = JObject.Parse(body);
            foreach (var name in tupleAttr.TransformNames)
            {
                var currentItemName = "Item" + itemIndex;
                var field = tupleType.GetField(currentItemName);
                if (field.FieldType.IsPrimitive || field.FieldType == typeof(string) || field.FieldType == typeof(decimal))
                {
                    var data = jobj[name];
                    if (((JToken)data).Type == JTokenType.Null && IsNullable(field.FieldType))
                    {
                        field.SetValue(tuple, null);
                    }
                    else
                    {
                        field.SetValue(tuple, Convert.ChangeType(data, field.FieldType));
                    }
                }
                else
                {
                    var data = jobj[name];
                    if (data == null)
                    {
                        field.SetValue(tuple, null);
                    }
                    else // is a class maybe ?
                    {
                        var json = data.ToString();
                        field.SetValue(tuple, JsonConvert.DeserializeObject(json, field.FieldType));
                    }
                }
                itemIndex++;
            }

            return tuple;
        }

        static bool IsNullable(Type type)
        {
            if (!type.IsValueType) return true; // ref-type
            if (Nullable.GetUnderlyingType(type) != null) return true; // Nullable<T>
            return false; // value-type
        }
    }
}
