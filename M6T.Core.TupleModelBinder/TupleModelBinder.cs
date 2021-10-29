using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
        private static readonly Type[] VALUE_TUPLE_TYPES = new[] {
            typeof(ValueTuple<>),
            typeof(ValueTuple<,>),
            typeof(ValueTuple<,,>),
            typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>),
            typeof(ValueTuple<,,,,,>),
            typeof(ValueTuple<,,,,,,>),
            typeof(ValueTuple<,,,,,,,>),
        };

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var reader = new StreamReader(bindingContext.HttpContext.Request.Body);

            var body = await reader.ReadToEndAsync();

            var modelAttributes = bindingContext.ModelMetadata.GetType().GetProperty("Attributes").GetValue(bindingContext.ModelMetadata) as ModelAttributes;

            var tupleAttr = modelAttributes.Attributes.OfType<TupleElementNamesAttribute>().FirstOrDefault();
            if (tupleAttr == null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }

            var jobj = JObject.Parse(body);
            int parameterIndex = 0;
            object tuple = ParseTupleFromModelAttributes(jobj, tupleAttr.TransformNames, bindingContext.ModelType, ref parameterIndex);

            bindingContext.Result = ModelBindingResult.Success(tuple);
        }

        public static object ParseTupleFromModelAttributes(JObject jobject, IList<string> parameterNames, Type parameterType, ref int parameterIndex)
        {
            if (parameterType.GenericTypeArguments.Length > VALUE_TUPLE_TYPES.Length)
                throw new InvalidOperationException($"Cannot bind model of ValueTuple with more than {VALUE_TUPLE_TYPES.Length} generic type arguments.");

            bool isNestedValueTuple = parameterType.GenericTypeArguments.Length == VALUE_TUPLE_TYPES.Length;
            int numSimpleGenericTypeArgs = Math.Min(VALUE_TUPLE_TYPES.Length - 1, parameterType.GenericTypeArguments.Length);
            object[] parameters = new object[parameterType.GenericTypeArguments.Length];
            for (int a = 0; a < numSimpleGenericTypeArgs; ++a)
            {
                parameters[a] = GetValue(jobject, parameterNames[parameterIndex], parameterType.GenericTypeArguments[a]);
                ++parameterIndex;
            }
            if (isNestedValueTuple)
            {
                Type nestedValueTupleType = parameterType.GenericTypeArguments[VALUE_TUPLE_TYPES.Length - 1];
                parameters[VALUE_TUPLE_TYPES.Length - 1] = ParseTupleFromModelAttributes(jobject, parameterNames, nestedValueTupleType, ref parameterIndex);
            }
            Type genericValueTupleType = VALUE_TUPLE_TYPES[parameterType.GenericTypeArguments.Length - 1];
            Type concreteValueTupleType = genericValueTupleType.MakeGenericType(parameterType.GenericTypeArguments);
            return Activator.CreateInstance(concreteValueTupleType, parameters);
        }

        static object GetValue(JObject jobject, string name, Type parameterType)
        {
            var value = jobject.GetValue(name, StringComparison.CurrentCultureIgnoreCase);

            if (value == null || value.Type == JTokenType.Null)
            {
                if (IsNullable(parameterType))
                    return Convert.ChangeType(null, parameterType);
                else
                    return Activator.CreateInstance(parameterType); //default value
            }

            /*
             *  If parameter type is guid, special handling is required since NewtonSoft.Json doesnt handle it correctly for some reason.
             *  If there is another possible input type besides string for guid please add it. 
             *  This currently supports all Guid format types stated in
             *  https://docs.microsoft.com/en-us/dotnet/api/system.guid.tostring?view=net-5.0
             */
            if (parameterType == typeof(Guid) && value.Type == JTokenType.String)
            {
                return Guid.Parse(value.ToString());
            }

            if (parameterType.IsPrimitive || parameterType == typeof(string) || parameterType == typeof(decimal))
                return Convert.ChangeType(value, parameterType);
            else
                return Convert.ChangeType(JsonConvert.DeserializeObject(value.ToString(), parameterType), parameterType);
        }

        static bool IsNullable(Type type)
        {
            if (!type.IsValueType) return true; // ref-type
            if (Nullable.GetUnderlyingType(type) != null) return true; // Nullable<T>
            return false; // value-type
        }
    }
}
