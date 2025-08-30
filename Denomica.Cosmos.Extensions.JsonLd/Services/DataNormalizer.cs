using Denomica.Text.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;

namespace Denomica.Cosmos.Extensions.JsonLd.Services
{
    using JsonDictionary = Dictionary<string, object?>;
    using JsonList = List<object?>;

    /// <summary>
    /// The default implementation of a data normalizer for JSON-LD data.
    /// </summary>
    /// <remarks>
    /// You can create your custom data normalizer by inheriting from this class and overriding its methods.
    /// </remarks>
    public class DataNormalizer
    {

        /// <summary>
        /// Normalizes the given JSON element and returns the normalized element.
        /// </summary>
        /// <remarks>
        /// Normalizes the given element by performing the following actions.
        /// <list type="bullet">
        /// <item>
        /// Every property that contains one instance of another object is converted
        /// into an array where the single object is the only element.
        /// </item>
        /// <item>
        /// The <c>@context</c> property defined on the root is removed from descendant objects.
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="element">The element to normalize.</param>
        /// <returns>Returns the normalized element.</returns>
        public async Task<JsonElement> NormalizeAsync(JsonElement element)
        {
            var d = JsonUtil.CreateDictionary(element);

            if(!d.ContainsKey("@context"))
            {
                d["@context"] = "https://schema.org";
            }

            string rootContext = $"{d["@context"]}";
            this.ProcessDictionary(d, rootContext, 0);

            var json = await d.SerializeAsync();
            return JsonDocument.Parse(json).RootElement;
        }


        private void ProcessDictionary(JsonDictionary dictionary, string rootContext, int level)
        {
            if(level > 0 && dictionary.ContainsKey("@context") && $"{dictionary["@context"]}" == rootContext)
            {
                dictionary.Remove("@context");
            }

            foreach(var key in from x in dictionary.Keys where dictionary[x] is JsonDictionary select x)
            {
                JsonDictionary d = (JsonDictionary)dictionary[key]!;
                ProcessDictionary(d, rootContext, level + 1);

                dictionary[key] = new JsonList() { d };
            }
        }
    }
}
