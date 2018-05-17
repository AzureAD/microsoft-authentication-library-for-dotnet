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

using Microsoft.Identity.Core;
using Microsoft.Identity.Core.UI;

namespace Microsoft.Identity.Client.Internal
{
    internal static class PlatformPlugin
    {
        static PlatformPlugin()
        {
            InitializeByAssemblyDynamicLinking();
        }

        public static IWebUIFactory WebUIFactory { get; set; }
        
        public static PlatformInformationBase PlatformInformation { get; set; }

        public static void InitializeByAssemblyDynamicLinking()
        {
#if !FACADE
            CoreLoggerBase.Default = new MsalLogger(Guid.Empty, null);
            IWebUIFactory obj = null;

#if ANDROID || iOS
            obj = new Microsoft.Identity.Core.UI.WebUIFactory();
#else
            obj = new Microsoft.Identity.Client.Internal.UI.WebUIFactory();
#endif
            InjectDependecies(obj,
                (PlatformInformationBase) new PlatformInformation());
#endif
        }

        public static void InjectDependecies(IWebUIFactory webUIFactory,
            PlatformInformationBase platformInformation)
        {
            WebUIFactory = webUIFactory;
            PlatformInformation = platformInformation;
        }

#if !FACADE
        public static void LogMessage(MsalLogLevel logLevel, string formattedMessage)
        {
            switch (logLevel)
            {
                case MsalLogLevel.Error:
                    PlatformLogger.Error(formattedMessage);
                    break;
                case MsalLogLevel.Warning:
                    PlatformLogger.Warning(formattedMessage);
                    break;
                case MsalLogLevel.Info:
                    PlatformLogger.Information(formattedMessage);
                    break;
                case MsalLogLevel.Verbose:
                    PlatformLogger.Verbose(formattedMessage);
                    break;
            }
        }
#endif
    }
}