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
        static CloudConfiguration _cloudConfiguration;

        static CloudConfigurationProvider()
        {
            var configBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("config.json");
            var config = configBuilder.Build();
            string cloudType = config.GetValue(typeof(string), "CloudType").ToString();
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
                //return "https://login.chinacloudapi.cn/common";
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
