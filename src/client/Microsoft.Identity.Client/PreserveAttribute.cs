// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Attribute that will be picked up by the Xamarin Linker, as a hint for the linker to not remove the type.
    /// Needs to be added to types that get created by reflection, e.g. JSON serialization types
    /// </summary>
    /// <remarks>It's important to not change the name and the 2 fields of this class. The linker looks for these.</remarks>
    [System.AttributeUsage(System.AttributeTargets.All)]
    class PreserveAttribute : System.Attribute // do not rename
    {
        public bool Conditional; // do not change to a property or rename
        public bool AllMembers;  // do not change to a property or rename
    }
}
