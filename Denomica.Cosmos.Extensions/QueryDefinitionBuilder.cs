﻿using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.Cosmos.Extensions
{
    /// <summary>
    /// A builder class that you use to build <see cref="QueryDefinition"/> instances with
    /// a fluent interface.
    /// </summary>
    public class QueryDefinitionBuilder
    {

        private StringBuilder QueryTextBuilder = new StringBuilder();
        /// <summary>
        /// Appends a string to the query text.
        /// </summary>
        /// <param name="text">The string to append.</param>
        public QueryDefinitionBuilder AppendQueryText(string text)
        {
            this.QueryTextBuilder.Append(text);
            return this;
        }

        /// <summary>
        /// Appends a string to the query text if <paramref name="condition"/> is <c>true</c>.
        /// </summary>
        /// <param name="text">The string to conditionally append to the query text.</param>
        /// <param name="condition">The condition that determines whether the given string should be appended to the query text.</param>
        public QueryDefinitionBuilder AppendQueryTextIf(string text, bool condition)
        {
            if (condition)
            {
                this.AppendQueryText(text);
            }

            return this;
        }

        /// <summary>
        /// Appends a string to the query text if <paramref name="condition"/> returns <c>true</c>.
        /// </summary>
        /// <param name="text">The string to conditionally append to the query text.</param>
        /// <param name="condition">A delegate that returns a boolean value indicateing whether the given string should be appended to the query text.</param>
        public async Task<QueryDefinitionBuilder> AppendQueryTextIfAsync(string text, Func<Task<bool>> condition)
        {
            if(await condition())
            {
                this.AppendQueryText(text);
            }

            return this;
        }

        /// <summary>
        /// The dictionary containing the parameters added to the builder.
        /// </summary>
        public IDictionary<string, object> Parameters { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// Adds a parameter to the query definition.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        public QueryDefinitionBuilder WithParameter(string name, object value)
        {
            if(this.Parameters.ContainsKey(name))
            {
                throw new ArgumentException($"Parameters '{name}' already exsists in the parameter collection.", nameof(name));
            }

            this.Parameters[name] = value;

            return this;
        }

        /// <summary>
        /// Adds a parameter to the query if <paramref name="condition"/> is <c>true</c>.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="condition">The condition that determines whether the specified parameter is added to the query definition.</param>
        public QueryDefinitionBuilder WithParameterIf(string name, object value, bool condition)
        {
            if(condition)
            {
                this.WithParameter(name, value);
            }
            return this;
        }

        /// <summary>
        /// Adds a parameter to the query if <paramref name="condition"/> is <c>true</c>.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="valueProvider">A function that returns the value for the parameter. This function is only called if <paramref name="condition"/> is <c>true</c>.</param>
        /// <param name="condition">The condition that determines whether the specified parameter is added to the query definition.</param>
        public QueryDefinitionBuilder WithParameterIf(string name, Func<object> valueProvider, bool condition)
        {
            if(condition)
            {
                this.WithParameter(name, valueProvider());
            }

            return this;
        }

        /// <summary>
        /// Adds a parameter to the query if <paramref name="condition"/> returns <c>true</c>.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="condition">A delegate that returns a boolean value indicating whether the given parameter should be added to the query definition.</param>
        public async Task<QueryDefinitionBuilder> WithParameterIfAsync(string name, object value, Func<Task<bool>> condition)
        {
            if(await condition())
            {
                this.WithParameter(name, value);
            }
            return this;
        }

        /// <summary>
        /// Adds a parameter to the query if <paramref name="condition"/> returns <c>true</c>.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="valueProvider">A function that returns the value for the parameter. This function is only called if <paramref name="condition"/> is <c>true</c>.</param>
        /// <param name="condition">A delegate that returns a boolean value indicating whether the given parameter should be added to the query definition.</param>
        public async Task<QueryDefinitionBuilder> WithParameterIfAsync(string name, Func<Task<object>> valueProvider, Func<Task<bool>> condition)
        {
            if(await condition())
            {
                this.WithParameter(name, await valueProvider());
            }

            return this;
        }

        /// <summary>
        /// Builds the query definition and returns it.
        /// </summary>
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
