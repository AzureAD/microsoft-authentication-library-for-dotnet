using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
    internal class MsalAppMetadataCacheItem
    {
        /// <remarks>mandatory</remarks>
        public string ClientId { get; set;}

        /// <remarks>mandatory</remarks>

        public string Environment { get; set; }

        /// <summary>
        /// The family id of which this application is part of. This is an internal feature and there is currently a single app,
        /// with id 1. If familyId is empty, it means an app is not part of a family. A missing entry means unkown status.
        /// </summary>
        public string FamilyId { get; set; }
    }
}
