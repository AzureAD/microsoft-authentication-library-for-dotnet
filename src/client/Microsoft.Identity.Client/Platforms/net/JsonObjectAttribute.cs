// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Platforms.net
{
    /// <summary>
    /// Dummy class to mimic Microsoft.Identity.Json.JsonObjectAttribute on .NET platform to reduce the number of compilation flags in the code
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false)]
    internal class JsonObjectAttribute : Attribute
    {
        public string Title { get; set; }
    }
}
