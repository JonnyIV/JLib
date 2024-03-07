using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JLib.Helper;

namespace JLib.Exceptions
{
    /// <summary>
    /// Base class for all custom exceptions.
    /// Can be used to handle custom exceptions separately.
    /// </summary>
    public class JLibAggregateException : AggregateException
    {
        /// <summary>
        /// Gets the user-defined message associated with the exception.
        /// </summary>
        public string UserMessage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JLibAggregateException"/> class with a specified user-defined message and a collection of inner exceptions.
        /// </summary>
        /// <param name="userMessage">The user-defined message associated with the exception.</param>
        /// <param name="innerExceptions">The collection of inner exceptions.</param>
        public JLibAggregateException(string userMessage, Exception[] innerExceptions) : base(userMessage, innerExceptions)
        {
            UserMessage = userMessage;
            _message = new Lazy<string>(this.GetTreeInfo);
        }

        /// <summary>
        /// Throws a <see cref="JLibAggregateException"/> if the collection of exceptions is not empty.
        /// </summary>
        /// <param name="message">The user-defined message associated with the exception.</param>
        /// <param name="content">The collection of exceptions.</param>
        public static void ThrowIfNotEmpty(string message, IEnumerable<Exception> content)
        {
            var ex = ReturnIfNotEmpty(message, content);
            if (ex is not null)
                throw ex;
        }

        /// <summary>
        /// Throws a <see cref="JLibAggregateException"/> if the collection of exceptions is not empty.
        /// </summary>
        /// <param name="message">The user-defined message associated with the exception.</param>
        /// <param name="content">The collection of exceptions.</param>
        public static void ThrowIfNotEmpty(string message, params Exception[] content)
            => ThrowIfNotEmpty(message, content.AsEnumerable());

        /// <summary>
        /// Returns a <see cref="JLibAggregateException"/> if the collection of exceptions is not empty; otherwise, returns null.
        /// </summary>
        /// <param name="message">The user-defined message associated with the exception.</param>
        /// <param name="content">The collection of exceptions.</param>
        /// <returns>A <see cref="JLibAggregateException"/> if the collection of exceptions is not empty; otherwise, null.</returns>
        public static Exception? ReturnIfNotEmpty(string message, params Exception[] content)
            => content.Length switch
            {
                0 => null,
                _ => new JLibAggregateException(message, content)
            };

        /// <summary>
        /// Returns a <see cref="JLibAggregateException"/> if the collection of exceptions is not empty; otherwise, returns null.
        /// </summary>
        /// <param name="message">The user-defined message associated with the exception.</param>
        /// <param name="content">The collection of exceptions.</param>
        /// <returns>A <see cref="JLibAggregateException"/> if the collection of exceptions is not empty; otherwise, null.</returns>
        public static Exception? ReturnIfNotEmpty(string message, IEnumerable<Exception?> content)
            => ReturnIfNotEmpty(message, content.WhereNotNull().ToArray());

        private readonly Lazy<string> _message;

        /// <summary>
        /// <inheritdoc cref="Exception.Message"/>
        /// </summary>
        public override string Message => _message.Value;
    }
}