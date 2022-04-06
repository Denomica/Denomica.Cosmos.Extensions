using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.Cosmos.Extensions
{
    public class QueryDefinitionBuilder
    {

        private StringBuilder QueryTextBuilder = new StringBuilder();
        public QueryDefinitionBuilder AppendQueryText(string text)
        {
            this.QueryTextBuilder.Append(text);
            return this;
        }

        public QueryDefinitionBuilder AppendQueryTextIf(string text, bool condition)
        {
            if (condition)
            {
                this.AppendQueryText(text);
            }

            return this;
        }

        public async Task<QueryDefinitionBuilder> AppendQueryTextIfAsync(string text, Func<Task<bool>> condition)
        {
            if(await condition())
            {
                this.AppendQueryText(text);
            }

            return this;
        }

        private List<KeyValuePair<string, object>> Parameters = new List<KeyValuePair<string, object>>();
        public QueryDefinitionBuilder AddParameter(string name, object value)
        {
            this.Parameters.Add(new KeyValuePair<string, object>(name, value));
            return this;
        }

        public QueryDefinition Build()
        {

            var qry = new QueryDefinition(this.QueryTextBuilder.ToString());
            
            foreach(var p in this.Parameters)
            {
                qry = qry.WithParameter(p.Key, p.Value);
            }

            return qry;
        }
    }
}
