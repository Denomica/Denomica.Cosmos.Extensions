using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.Cosmos.Extensions.Tests.Configuration
{
    public class ConnectionOptions
    {

        public string ConnectionString { get; set; } = null!;

        public string DatabaseId { get; set; } = null!;

        public string ContainerId { get; set; } = null!;

    }
}
