using Schema.NET;
using System;
using System.Text;
using System.Collections.Generic;
using Denomica.Text.Json;
using System.Linq;

namespace Denomica.Cosmos.Extensions.JsonLd
{
    using JsonDictionary = Dictionary<string, object?>;
    using JsonList = List<object?>;

    public static class ExtensionMethods
    {
        public static JsonDictionary Normalize<T>(this T? thing) where T : Thing
        {
            if(null != thing)
            {
                var d = JsonUtil.CreateDictionary(thing);
                RemoveNullValues(d);
                NormalizeValues(d);

                return d;
            }

            return new JsonDictionary();
        }



        private static void NormalizeValues(JsonDictionary dictionary)
        {
            foreach(var key in from x in dictionary.Keys where !x.StartsWith("@") select x)
            {
                var value = dictionary[key];
                if(!(value is JsonList))
                {
                    dictionary[key] = new JsonList() { value };
                }
            }
        }

        private static void RemoveNullValues(JsonDictionary dictionary)
        {
            foreach (var key in from x in dictionary.Keys where x != "@context" select x)
            {
                var value = dictionary[key];
                if (value == null)
                {
                    dictionary.Remove(key);
                }
                else if (value is JsonDictionary)
                {
                    RemoveNullValues((JsonDictionary)value);
                }
                else if(value is JsonList)
                {
                    var list = (JsonList)value;
                    foreach (var item in list)
                    {
                        if(item is JsonDictionary)
                        {
                            RemoveNullValues((JsonDictionary)item);
                        }
                    }
                }
            }
        }


    }
}
