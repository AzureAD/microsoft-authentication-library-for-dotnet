using System;
using System.Collections.Specialized;
using System.IO;
//using System.Configuration;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Identity.Test.ConfigurationProvider
{
    public static class CloudConfigurationProvider
    {
        const string CloudSelectionConfig = "cloudSelection.json";
        const string CloudConfig = "cloudConfig.json";
        static CloudConfiguration _cloudConfiguration;

        static CloudConfigurationProvider()
        {
            var cloudSelectorConfigBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(CloudSelectionConfig);
            var selectionConfig = cloudSelectorConfigBuilder.Build();
            string cloudType = selectionConfig.GetValue(typeof(string), "CloudType").ToString();

            var configBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(CloudConfig);
            var config = configBuilder.Build();
            _cloudConfiguration = new CloudConfiguration(config.GetSection(cloudType));
        }

        public static CloudType CloudType
        {
            get
            {
                return _cloudConfiguration.CloudType;
            }
        }

        public static string Authority
        {
            get
            {
                return _cloudConfiguration.Authority;
            }
        }

        public static string Scopes
        {
            get
            {
                return _cloudConfiguration.Scopes;
            }
        }
    }
}
