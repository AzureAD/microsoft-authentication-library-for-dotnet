using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Identity.Test.ConfigurationProvider
{
    public class CloudConfiguration
    {
        public CloudConfiguration(IConfigurationSection section)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            section.Bind(this);
        }

        public CloudType CloudType { get; set; }

        public string Authority { get; set; }

        public string Scopes { get; set; }
    }
}
