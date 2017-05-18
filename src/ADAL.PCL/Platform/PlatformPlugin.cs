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
            HttpClientFactory = new HttpClientFactory();

            if (PlatformPluginSwitch.DynamicallyLinkAssembly)
            {
                InitializeByAssemblyDynamicLinking();
            }
        }

        public static IWebUIFactory WebUIFactory { get; set; }
        public static IHttpClientFactory HttpClientFactory { get; set; }
        public static ITokenCachePlugin TokenCachePlugin { get; set; }
        public static LoggerBase Logger { get; set; }
        public static PlatformInformationBase PlatformInformation { get; set; }
        public static ICryptographyHelper CryptographyHelper { get; set; }
        public static IDeviceAuthHelper DeviceAuthHelper { get; set; }
        public static IBrokerHelper BrokerHelper { get; set; }
        public static IWebProxyProvider WebProxyProvider { get; set; }

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
                (IBrokerHelper)Activator.CreateInstance(assembly.GetType(Namespace + "BrokerHelper")),
                (IWebProxyProvider)Activator.CreateInstance(assembly.GetType(Namespace + "WebProxyProvider"))
            );
        }

        public static void InjectDependecies(IWebUIFactory webUIFactory, ITokenCachePlugin tokenCachePlugin, LoggerBase logger, 
            PlatformInformationBase platformInformation, ICryptographyHelper cryptographyHelper,
            IDeviceAuthHelper deviceAuthHelper, IBrokerHelper brokerHelper, IWebProxyProvider webProxyProvider)
        {
            WebUIFactory = webUIFactory;
            TokenCachePlugin = tokenCachePlugin;
            Logger = logger;
            PlatformInformation = platformInformation;
            CryptographyHelper = cryptographyHelper;
            DeviceAuthHelper = deviceAuthHelper;
            BrokerHelper = brokerHelper;
            WebProxyProvider = webProxyProvider;
        }

        private static Assembly LoadPlatformSpecificAssembly()
        {
            // For security reasons, it is important to have PublicKeyToken mentioned referencing the assembly.
            const string PlatformSpecificAssemblyNameTemplate = "Microsoft.IdentityModel.Clients.ActiveDirectory.Platform, Version={0}, Culture=neutral, PublicKeyToken=31bf3856ad364e35";

            string platformSpecificAssemblyName = string.Format(CultureInfo.CurrentCulture, PlatformSpecificAssemblyNameTemplate, AdalIdHelper.GetAdalVersion());

            try
            {
                return Assembly.Load(new AssemblyName(platformSpecificAssemblyName));
            }
            catch (FileNotFoundException ex)
            {
                throw new AdalException(AdalError.AssemblyNotFound, string.Format(CultureInfo.InvariantCulture, AdalErrorMessage.AssemblyNotFoundTemplate, platformSpecificAssemblyName), ex);
            }
            catch (Exception ex) // FileLoadException is missing from PCL
            {
                throw new AdalException(AdalError.AssemblyLoadFailed, string.Format(CultureInfo.InvariantCulture, AdalErrorMessage.AssemblyLoadFailedTemplate, platformSpecificAssemblyName), ex);
            }
        }
    }
}
