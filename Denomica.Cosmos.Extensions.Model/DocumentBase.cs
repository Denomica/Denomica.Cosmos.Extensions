using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Cosmos.Extensions.Model
{
    /// <summary>
    /// A base class for document models stored in Cosmos DB.
    /// </summary>
    public class DocumentBase
    {

        /// <summary>
        /// Triggers when the value of a property has changed.
        /// </summary>
        public event EventHandler<PropertyValueChangedEventArgs> PropertyValueChanged;

        /// <summary>
        /// Sets or returns the ID of the document.
        /// </summary>
        public virtual string Id
        {
            get { return this.GetProperty<string>(nameof(Id), () => Guid.NewGuid().ToString()); }
            set { this.SetProperty(nameof(Id), value.Replace("#", "_").Replace("/", "_").Replace("\\", "_")); }
        }

        /// <summary>
        /// The name of the document type.
        /// </summary>
        /// <remarks>
        /// By default, uses the class name of the class inheriting from <see cref="DocumentBase"/>.
        /// </remarks>
        public virtual string Type
        {
            get { return this.GetProperty<string>(nameof(Type), () => this.GetType().Name); }
            set { this.SetProperty(nameof(Type), value); }
        }



        private Dictionary<string, object?> Properties = new Dictionary<string, object?>();
        /// <summary>
        /// Returns the value of the property with the given name typed as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to return the value as.</typeparam>
        /// <param name="name">The name of the property whose value to return.</param>
        /// <returns>
        /// Returns the given value, or the default value for <typeparamref name="T"/> if a value with the 
        /// given name does not exist or if its type is not compatible with <typeparamref name="T"/>.
        /// </returns>
        protected T GetProperty<T>(string name)
        {
            return this.GetProperty<T>(name, () => default(T));
        }

        /// <summary>
        /// Returns the value of the property with the name specified in <paramref name="name"/> typed as
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to return the property value as.</typeparam>
        /// <param name="name">The name of the property whose value to return.</param>
        /// <param name="defaultValue">
        /// If a property with <paramref name="name"/> is not found, or it is not compatible with <typeparamref name="T"/>, 
        /// the default value specified in this parameter is returned.
        /// </param>
        /// <returns>Returns the property value or <paramref name="defaultValue"/>.</returns>
        protected T GetProperty<T>(string name, T defaultValue)
        {
            return this.GetProperty<T>(name, () => defaultValue);
        }

        /// <summary>
        /// Returns the value fo the property with the name specified in <paramref name="name"/> typed as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to return the property value as.</typeparam>
        /// <param name="name">The name of the value to return.</param>
        /// <param name="defaultValue">A delegate that returns a default value if a value cannot be found. The delegate is invoked ONLY if the value is not found.</param>
        /// <returns>Returns the properly value or the value returned by <paramref name="defaultValue"/>.</returns>
        protected virtual T GetProperty<T>(string name, Func<T> defaultValue)
        {
            if (!this.TryGetProperty<T>(name, out T value))
            {
                this.SetProperty(name, defaultValue());
            }

            return (T)this.Properties[name];
        }

        /// <summary>
        /// Sets the value of the properly.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name of the property to set.</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>Returns <c>true</c> if the value of the property was changed.</returns>
        protected virtual bool SetProperty<T>(string name, T value)
        {
            bool changed = false;
            if (this.TryGetProperty<T>(name, out T oldValue))
            {
                if (!object.Equals(oldValue, value))
                {
                    this.Properties[name] = value;
                    changed = true;

                    this.OnPropertyValueChanged(new PropertyValueChangedEventArgs(name, oldValue, value));
                }
            }
            else
            {
                this.Properties[name] = value;
                changed = true;

                this.OnPropertyValueChanged(new PropertyValueChangedEventArgs(name, null, value));
            }

            return changed;
        }

        /// <summary>
        /// Called when the value of one of the properties have changed.
        /// </summary>
        /// <param name="e">The event arguments containing information about the property whose value was changed.</param>
        /// <remarks>
        /// This method is responsible for triggering the <see cref="PropertyValueChanged"/> event.
        /// </remarks>
        protected virtual void OnPropertyValueChanged(PropertyValueChangedEventArgs e)
        {
            if(null != this.PropertyValueChanged)
            {
                this.PropertyValueChanged.Invoke(this, e);
            }
        }


        private bool TryGetProperty<T>(string name, out T value)
        {
            value = default;
            bool result = false;
            try
            {
                if (this.Properties.ContainsKey(name))
                {
                    value = (T)this.Properties[name];
                    result = true;
                }
            }
            catch { }

            return result;
        }

    }
}
