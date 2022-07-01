// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.ServiceEssentials
{
    /// <summary>
    /// Represents an object that can be serialized and deserialized. 
    /// </summary>
    public interface ICacheObject : IEquatable<ICacheObject>
    {
        /// <summary>
        /// Serializes an object into a string.
        /// </summary>
        /// <returns>The serialized value.</returns>
        string Serialize();

        /// <summary>
        /// Deserializes the <paramref name="serializedValue"/>.
        /// </summary>
        /// <param name="serializedValue">The serialized representation of the object.</param>
        void Deserialize(string serializedValue);
    }
}
