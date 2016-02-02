//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class PlatformPluginSwitch
    {
        public static bool DynamicallyLinkAssembly { get; set; }

        static PlatformPluginSwitch()
        {
            DynamicallyLinkAssembly = true;
        }
    }

    internal static class PlatformPlugin
    {
        static PlatformPlugin()
        {
            if (PlatformPluginSwitch.DynamicallyLinkAssembly)
            {
                InitializeByAssemblyDynamicLinking();
            }
        }

        public static IWebUIFactory WebUIFactory { get; set; }
        public static ITokenCachePlugin TokenCachePlugin { get; set; }
        public static LoggerBase Logger { get; set; }
        public static PlatformInformationBase PlatformInformation { get; set; }
        public static ICryptographyHelper CryptographyHelper { get; set; }
        public static IDeviceAuthHelper DeviceAuthHelper { get; set; }
        public static IBrokerHelper BrokerHelper { get; set; }

        public static void InitializeByAssemblyDynamicLinking()
        {
            Assembly assembly = LoadPlatformSpecificAssembly();
            const string Namespace = "Microsoft.IdentityModel.Clients.ActiveDirectory.";
            InjectDependecies(
                (IWebUIFactory)Activator.CreateInstance(assembly.GetType(Namespace + "WebUIFactory")),
                (ITokenCachePlugin)Activator.CreateInstance(assembly.GetType(Namespace + "TokenCachePlugin")),
                (LoggerBase)Activator.CreateInstance(assembly.GetType(Namespace + "Logger")),
                (PlatformInformationBase)Activator.CreateInstance(assembly.GetType(Namespace + "PlatformInformation")),
                (ICryptographyHelper)Activator.CreateInstance(assembly.GetType(Namespace + "CryptographyHelper")),
                (IDeviceAuthHelper)Activator.CreateInstance(assembly.GetType(Namespace + "DeviceAuthHelper")),
                (IBrokerHelper)Activator.CreateInstance(assembly.GetType(Namespace + "BrokerHelper"))
            );
        }

        public static void InjectDependecies(IWebUIFactory webUIFactory, ITokenCachePlugin tokenCachePlugin, LoggerBase logger, 
            PlatformInformationBase platformInformation, ICryptographyHelper cryptographyHelper,
            IDeviceAuthHelper deviceAuthHelper, IBrokerHelper brokerHelper)
        {
            WebUIFactory = webUIFactory;
            TokenCachePlugin = tokenCachePlugin;
            Logger = logger;
            PlatformInformation = platformInformation;
            CryptographyHelper = cryptographyHelper;
            DeviceAuthHelper = deviceAuthHelper;
            BrokerHelper = brokerHelper;
        }

        private static Assembly LoadPlatformSpecificAssembly()
        {
            // For security reasons, it is important to have PublicKeyToken mentioned referencing the assembly.
            const string PlatformSpecificAssemblyNameTemplate = "Microsoft.IdentityModel.Clients.ActiveDirectory.Platform, Version={0}, Culture=neutral, PublicKeyToken=31bf3856ad364e35";

            string platformSpecificAssemblyName = string.Format(PlatformSpecificAssemblyNameTemplate, MsalIdHelper.GetMsalVersion());

            try
            {
                return Assembly.Load(new AssemblyName(platformSpecificAssemblyName));
            }
            catch (FileNotFoundException ex)
            {
                throw new MsalException(MsalError.AssemblyNotFound, string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.AssemblyNotFoundTemplate, platformSpecificAssemblyName), ex);
            }
            catch (Exception ex) // FileLoadException is missing from PCL
            {
                throw new MsalException(MsalError.AssemblyLoadFailed, string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.AssemblyLoadFailedTemplate, platformSpecificAssemblyName), ex);
            }
        }
    }
}