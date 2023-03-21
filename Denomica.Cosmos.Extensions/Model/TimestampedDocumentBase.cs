using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Cosmos.Extensions.Model
{
    public class TimestampedDocumentBase : DocumentBase
    {
        /// <summary>
        /// The timestamp when the document was created.
        /// </summary>
        /// <remarks>
        /// Your application is fully responsible for managing this value.
        /// </remarks>
        public virtual DateTimeOffset Created
        {
            get { return this.GetProperty<DateTimeOffset>(nameof(Created), () => DateTimeOffset.Now); }
            set { this.SetProperty(nameof(Created), value); }
        }

        /// <summary>
        /// The timestamp when the document was last modified.
        /// </summary>
        /// <remarks>
        /// Your application is fully responsible for managing this value.
        /// </remarks>
        public virtual DateTimeOffset Modified
        {
            get { return this.GetProperty<DateTimeOffset>(nameof(Modified), () => DateTimeOffset.Now); }
            set { this.SetProperty(nameof(Modified), value); }
        }
    }
}
