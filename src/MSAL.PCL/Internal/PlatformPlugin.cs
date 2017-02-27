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
using Microsoft.Identity.Client.Internal.Interfaces;

namespace Microsoft.Identity.Client.Internal
{
    internal static class PlatformPluginSwitch
    {
        static PlatformPluginSwitch()
        {
            DynamicallyLinkAssembly = true;
        }

        public static bool DynamicallyLinkAssembly { get; set; }
    }

    internal static class PlatformPlugin
    {
        private const string Namespace = "Microsoft.Identity.Client.";

        static PlatformPlugin()
        {
            if (PlatformPluginSwitch.DynamicallyLinkAssembly)
            {
                InitializeByAssemblyDynamicLinking();
            }
        }

        public static IWebUIFactory WebUIFactory { get; set; }

        public static ITokenCachePlugin NewTokenCachePluginInstance
        {
            get
            {
                Assembly assembly = LoadPlatformSpecificAssembly();
                return (ITokenCachePlugin) Activator.CreateInstance(assembly.GetType(Namespace + "TokenCachePlugin"));
            }
        }

        public static ITokenCachePlugin TokenCachePlugin { get; set; }

        public static Logger Logger { get; set; }
        //public static LoggerBase Logger { get; set; }
        public static PlatformInformationBase PlatformInformation { get; set; }
        public static ICryptographyHelper CryptographyHelper { get; set; }
        public static IDeviceAuthHelper DeviceAuthHelper { get; set; }
        public static IBrokerHelper BrokerHelper { get; set; }
        public static IPlatformParameters DefaultPlatformParameters { get; set; }

        public static void InitializeByAssemblyDynamicLinking()
        {
            Assembly assembly = LoadPlatformSpecificAssembly();
            InjectDependecies(
                (IWebUIFactory) Activator.CreateInstance(assembly.GetType(Namespace + "WebUIFactory")),
                (ITokenCachePlugin) Activator.CreateInstance(assembly.GetType(Namespace + "TokenCachePlugin")),
                (LoggerBase) Activator.CreateInstance(assembly.GetType(Namespace + "Logger")),
                (PlatformInformationBase) Activator.CreateInstance(assembly.GetType(Namespace + "PlatformInformation")),
                (ICryptographyHelper) Activator.CreateInstance(assembly.GetType(Namespace + "CryptographyHelper")),
                (IDeviceAuthHelper) Activator.CreateInstance(assembly.GetType(Namespace + "DeviceAuthHelper")),
                (IBrokerHelper) Activator.CreateInstance(assembly.GetType(Namespace + "BrokerHelper")),
                (IPlatformParameters) Activator.CreateInstance(assembly.GetType(Namespace + "PlatformParameters"))
                );
        }

        public static void InjectDependecies(IWebUIFactory webUIFactory, ITokenCachePlugin tokenCachePlugin,
            LoggerBase logger,
            PlatformInformationBase platformInformation, ICryptographyHelper cryptographyHelper,
            IDeviceAuthHelper deviceAuthHelper, IBrokerHelper brokerHelper, IPlatformParameters platformParameters)
        {
            WebUIFactory = webUIFactory;
            TokenCachePlugin = tokenCachePlugin;
            //Logger = logger;
            PlatformInformation = platformInformation;
            CryptographyHelper = cryptographyHelper;
            DeviceAuthHelper = deviceAuthHelper;
            BrokerHelper = brokerHelper;
            DefaultPlatformParameters = platformParameters;
        }

        private static Assembly LoadPlatformSpecificAssembly()
        {
            // For security reasons, it is important to have PublicKeyToken mentioned referencing the assembly.
            const string PlatformSpecificAssemblyNameTemplate =
                "Microsoft.Identity.Client.Platform, Version={0}, Culture=neutral, PublicKeyToken=0a613f4dd989e8ae";

            string platformSpecificAssemblyName = string.Format(CultureInfo.InvariantCulture,
                PlatformSpecificAssemblyNameTemplate, MsalIdHelper.GetMsalVersion());

            try
            {
                return Assembly.Load(new AssemblyName(platformSpecificAssemblyName));
            }
            catch (FileNotFoundException ex)
            {
                PlatformPlugin.Logger.LogMessage(null, ex, Logger.EventType.Error);
                throw new MsalException(MsalError.AssemblyNotFound,
                    string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.AssemblyNotFoundTemplate,
                        platformSpecificAssemblyName), ex);
            }
            catch (Exception ex) // FileLoadException is missing from PCL
            {
                PlatformPlugin.Logger.LogMessage(null, ex, Logger.EventType.Error);
                throw new MsalException(MsalError.AssemblyLoadFailed,
                    string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.AssemblyLoadFailedTemplate,
                        platformSpecificAssemblyName), ex);
            }
        }

    }
}