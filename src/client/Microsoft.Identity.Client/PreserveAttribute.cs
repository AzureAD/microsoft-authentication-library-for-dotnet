// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Attribute that will be picked up by the Xamarin Linker, as a hint for the linker to not remove the type.
    /// Needs to be added to types that get created by reflection, e.g. JSON serialization types
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PreserveAttribute : System.Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public bool AllMembers;

        /// <summary>
        /// 
        /// </summary>
        public bool Conditional;
    }
}
