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
#if !NETSTANDARD1_1
                return null;
#else
                return null;
#endif
            }
        }

        public static ITokenCachePlugin TokenCachePlugin { get; set; }
        public static ILogger PlatformLogger { get; set; }
        public static PlatformInformationBase PlatformInformation { get; set; }
        public static ICryptographyHelper CryptographyHelper { get; set; }
        public static IPlatformParameters DefaultPlatformParameters { get; set; }

        public static void InitializeByAssemblyDynamicLinking()
        {
#if !NETSTANDARD1_1
            InjectDependecies(
                (IWebUIFactory) new WebUIFactory(),
                (ILogger)new PlatformLogger(),
                (PlatformInformationBase) new PlatformInformation(new RequestContext(Guid.Empty)),
                (ICryptographyHelper) new CryptographyHelper(),
                (IPlatformParameters) new PlatformParameters());
#endif
        }

        public static void InjectDependecies(IWebUIFactory webUIFactory,
            ILogger platformlogger,
            PlatformInformationBase platformInformation, ICryptographyHelper cryptographyHelper, IPlatformParameters platformParameters)
        {
            WebUIFactory = webUIFactory;
            PlatformLogger = platformlogger;
            PlatformInformation = platformInformation;
            CryptographyHelper = cryptographyHelper;
            DefaultPlatformParameters = platformParameters;
        }

        public static void LogMessage(Logger.LogLevel logLevel, string formattedMessage)
        {
            switch (logLevel)
            {
                case Logger.LogLevel.Error:
                    PlatformLogger.Error(formattedMessage);
                    break;
                case Logger.LogLevel.Warning:
                    PlatformLogger.Warning(formattedMessage);
                    break;
                case Logger.LogLevel.Info:
                    PlatformLogger.Information(formattedMessage);
                    break;
                case Logger.LogLevel.Verbose:
                    PlatformLogger.Verbose(formattedMessage);
                    break;
            }
        }
    }
}