using Denomica.Cosmos.Extensions.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Cosmos.Extensions.JsonLd.Model
{
    /// <summary>
    /// An envelope for a JSON-LD object stored in a Cosmos DB container.
    /// </summary>
    public class JsonLdObject : SyntheticPartitionKeyDocument
    {
        /// <summary>
        /// The actual JSON-LD object data.
        /// </summary>
        public Dictionary<string, object?> Data
        {
            get { return this.GetProperty<Dictionary<string, object?>>(nameof(Data), () => new Dictionary<string, object?>()); }
            set {  this.SetProperty(nameof(Data), value);}
        }

        public VectorEmbedding? Embedding
        {
            get { return this.GetProperty<VectorEmbedding?>(nameof(Embedding)); }
            set { this.SetProperty(nameof(Embedding), value); }
        }
    }
}
