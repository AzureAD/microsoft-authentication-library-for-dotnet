// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.TestOnly.Http
{
    /// <summary>
    /// Exception thrown when a <see cref="MockHttpMessageHandler"/> detects that an actual
    /// HTTP request does not match the configured expectations (e.g., wrong URL, wrong method,
    /// missing headers, unexpected query parameters).
    /// </summary>
    /// <remarks>
    /// This exception replaces MSTest <c>Assert.*</c> calls so that mock validation failures
    /// produce standard exceptions usable from any test framework (xUnit, NUnit, MSTest, etc.).
    /// </remarks>
    public sealed class MockHttpValidationException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MockHttpValidationException"/> with
        /// a descriptive message.
        /// </summary>
        /// <param name="message">Message describing the validation failure.</param>
        public MockHttpValidationException(string message) : base(message)
        {
        }
    }
}
