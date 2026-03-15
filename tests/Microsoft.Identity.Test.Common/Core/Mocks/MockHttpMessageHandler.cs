// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    /// <summary>
    /// Thin subclass that reexports
    /// <see cref="Microsoft.Identity.Client.TestOnly.Http.MockHttpMessageHandler"/>
    /// into this namespace for backward compatibility with existing MSAL test code.
    ///
    /// All mock-handler logic lives in the base class. Do not add any new logic here;
    /// add it to <see cref="Microsoft.Identity.Client.TestOnly.Http.MockHttpMessageHandler"/>
    /// instead.
    /// </summary>
    internal class MockHttpMessageHandler : Microsoft.Identity.Client.TestOnly.Http.MockHttpMessageHandler
    {
    }
}
