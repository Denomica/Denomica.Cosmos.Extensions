using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Cosmos.Extensions.Model
{
    /// <summary>
    /// The arguments class for an event signaling that the value of a property has changed.
    /// </summary>
    public class PropertyValueChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="name">The name of the property that was changed.</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        public PropertyValueChangedEventArgs(string name, object? oldValue, object? newValue)
        {
            this.Name = name;
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        /// <summary>
        /// The name of the property whose value was changed.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The old value of the property.
        /// </summary>
        public object? OldValue { get; private set; }

        /// <summary>
        /// The new value of the property.
        /// </summary>
        public object? NewValue { get; private set; }

    }
}
