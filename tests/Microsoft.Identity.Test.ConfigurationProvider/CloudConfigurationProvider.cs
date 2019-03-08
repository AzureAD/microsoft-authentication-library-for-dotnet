using System;
using System.Collections.Specialized;
using System.Configuration;

namespace Microsoft.Identity.Test.ConfigurationProvider
{
    public static class CloudConfigurationProvider
    {
        private const string _cloudTypeSetting = "CloudType";
        private const string _authoritySetting = "Authority";
        private const string _graphResource = "GraphResource";

        public static CloudType CloudType
        {
            get
            {
                //var res = ReadSetting(_cloudTypeSetting);
                //Enum.TryParse(res, out CloudType type);
                //return type;
                return CloudType.AzureChinaCloud;
            }
        }

        public static string Authority
        {
            get
            {
                //return ReadSetting(_authoritySetting);
                return "https://login.chinacloudapi.cn/common";
            }
        }

        public static string GraphResource
        {
            get
            {
                return ReadSetting(_graphResource);
            }
        }

        static string ReadSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                return appSettings[key] ?? "Not Found";
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
            }

            return null;
        }
    }
}
