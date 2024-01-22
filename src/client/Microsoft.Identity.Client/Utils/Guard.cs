// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Identity.Client.Utils
{
    internal static class Guard
    {
        public static T AgainstNull<T>(
            [NotNull] T? argument,
            [CallerArgumentExpression("argument")] string? paramName = null)
            where T : class
        {
            if (argument is null)
            {
                throw new ArgumentNullException(paramName);
            }

            return argument;
        }

        public static string AgainstNullOrEmpty(
            [NotNull] string? argument,
            [CallerArgumentExpression("argument")] string? paramName = null)
        {
            if (string.IsNullOrEmpty(argument))
            {
                throw new ArgumentNullException(paramName);
            }

            return argument;
        }
        public static string AgainstNullOrWhitespace(
            [NotNull] string? argument,
            [CallerArgumentExpression("argument")] string? paramName = null)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentNullException(paramName);
            }

            return argument;
        }
    }
}

#if NETFRAMEWORK || NETSTANDARD || NETCOREAPP2X

namespace System.Runtime.CompilerServices
{
    using Diagnostics;
    using Diagnostics.CodeAnalysis;

    /// <summary>
    /// Indicates that a parameter captures the expression passed for another parameter as a string.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [DebuggerNonUserCode]
    [AttributeUsage(AttributeTargets.Parameter)]
    sealed class CallerArgumentExpressionAttribute :
        Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallerArgumentExpressionAttribute"/> class.
        /// </summary>
        /// <param name="parameterName">
        /// The name of the parameter whose expression should be captured as a string.
        /// </param>
        public CallerArgumentExpressionAttribute(string parameterName) =>
            ParameterName = parameterName;

        /// <summary>
        /// Gets the name of the parameter whose expression should be captured as a string.
        /// </summary>
        public string ParameterName { get; }
    }
}
#endif
