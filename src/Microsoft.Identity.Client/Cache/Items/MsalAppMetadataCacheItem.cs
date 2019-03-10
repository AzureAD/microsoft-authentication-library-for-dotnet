//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Cache.Items
{
    /// <summary>
    /// Apps shouldn't rely on its presence, unless the app itself wrote it. It means that SDK should translate absense of app metadata to the default values of its required fields.
    /// Other apps that don't support app metadata should never remove existing app metadata.
    /// App metadata is a non-removable entity.It means there's no need for a public API to remove app metadata, and it shouldn't be removed when removeAccount is called.
    /// App metadata is a non-secret entity. It means that it cannot store any secret information, like tokens, nor PII, like username etc.
    /// App metadata can be extended by adding additional fields when required.Absense of any non-required field should translate to default values for those field.
    /// </summary>
    [DataContract]
    internal class MsalAppMetadataCacheItem : MsalItemWithAdditionalFields, IEquatable<MsalAppMetadataCacheItem>
    {
        public MsalAppMetadataCacheItem(string clientId, string env, string familyId)
        {
            this.ClientId = clientId;
            this.Environment = env;
            this.FamilyId = familyId;
        }

        /// <remarks>mandatory</remarks>
        public string ClientId { get; }

        /// <remarks>mandatory</remarks>

        public string Environment { get; }

        /// <summary>
        /// The family id of which this application is part of. This is an internal feature and there is currently a single app,
        /// with id 1. If familyId is empty, it means an app is not part of a family. A missing entry means unkown status.
        /// </summary>
        public string FamilyId { get; }

        public MsalAppMetadataCacheKey GetKey()
        {
            return new MsalAppMetadataCacheKey(ClientId, Environment);
        }

        internal static MsalAppMetadataCacheItem FromJsonString(string json)
        {
            return FromJObject(JObject.Parse(json));
        }

        internal static MsalAppMetadataCacheItem FromJObject(JObject j)
        {
            string clientId = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.ClientId);
            string environment = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.Environment);
            string familyId = JsonUtils.ExtractExistingOrEmptyString(j, StorageJsonKeys.FamilyId);

            var item = new MsalAppMetadataCacheItem(clientId, environment, familyId);

            item.PopulateFieldsFromJObject(j);

            return item;
        }

        internal string ToJsonString()
        {
            return ToJObject()
                .ToString();
        }

        internal override JObject ToJObject()
        {
            var json = base.ToJObject();

            json[StorageJsonKeys.Environment] = Environment;
            json[StorageJsonKeys.ClientId] = ClientId;
            json[StorageJsonKeys.FamilyId] = FamilyId;

            return json;
        }

        #region Equals and GetHashCode 

        public override bool Equals(object obj)
        {
            return obj is MsalAppMetadataCacheItem item &&
                Equals(item);
                  
        }

        public override int GetHashCode()
        {
            var hashCode = -1793347351;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ClientId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Environment);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FamilyId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AdditionalFieldsJson);

            return hashCode;
        }

        public bool Equals(MsalAppMetadataCacheItem item)
        {
            return ClientId == item.ClientId &&
                   Environment == item.Environment &&
                   FamilyId == item.FamilyId &&
                   base.AdditionalFieldsJson == item.AdditionalFieldsJson; 
        }

        #endregion
    }
}
