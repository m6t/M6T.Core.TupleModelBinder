using System;
using System.IO;
using System.Linq;
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
            //Could be better ?
            if (context.Metadata.ModelType.Name.StartsWith("ValueTuple"))
            {
                return new BinderTypeModelBinder(typeof(TupleModelBinder));
            }

            return null;
        }
    }

    public class TupleModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var modelName = bindingContext.ModelName;

            var reader = new StreamReader(bindingContext.HttpContext.Request.Body);

            var body = reader.ReadToEnd();

            var jobj = JObject.Parse(body);
            (string username, string password) deneme = JsonConvert.DeserializeObject<(string username, string password)>(body);
            string asd = ((dynamic)jobj).username;

            var modelAttributes = bindingContext.ModelMetadata.GetType().GetProperty("Attributes").GetValue(bindingContext.ModelMetadata) as ModelAttributes;
            var tuplenames = modelAttributes.Attributes.FirstOrDefault(x => x.GetType() == typeof(System.Runtime.CompilerServices.TupleElementNamesAttribute));
            if (tuplenames == null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }
            else
            {
                var names = (System.Runtime.CompilerServices.TupleElementNamesAttribute)tuplenames;
                object tuple = Activator.CreateInstance(bindingContext.ModelType);
                var tupleType = tuple.GetType();
                int itemIndex = 1;

                foreach (var name in names.TransformNames)
                {
                    var currentItemName = "Item" + itemIndex;
                    var field = tupleType.GetField(currentItemName);
                    if (field.FieldType.IsPrimitive || field.FieldType == typeof(string) || field.FieldType == typeof(decimal))
                    {
                        field.SetValue(tuple, Convert.ChangeType(jobj[name], field.FieldType));
                    }
                    else
                    {
                        var obj = jobj[name];
                        if (obj == null)
                        {
                            field.SetValue(tuple, Convert.ChangeType(jobj[name], field.FieldType));
                        }
                        else // is a class maybe ?
                        {
                            var json = obj.ToString();
                            field.SetValue(tuple, JsonConvert.DeserializeObject(json, field.FieldType));
                        }
                    }

                    itemIndex++;
                }
                bindingContext.Result = ModelBindingResult.Success(tuple);
                return Task.CompletedTask;
            }
        }
    }
}
