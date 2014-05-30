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
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// This class loads the assembly containing the authentication dialog classes and creates a new instance of an IWebUI.
    /// This class is necessary since there is a loose coupling between this assembly and the assembly containing Windows Forms 
    /// dependencies.
    /// </summary>
    internal class WebUIFactory : IWebUIFactory
    {
        // For security reasons, it is important to have PublicKeyToken mentioned referencing the assembly.
        private const string WebAuthenticationDialogAssemblyNameTemplate = "Microsoft.IdentityModel.Clients.ActiveDirectory.WindowsForms, Version={0}, Culture=neutral, PublicKeyToken=31bf3856ad364e35";

        private static MethodInfo dialogFactory;

        public static void ThrowIfUIAssemblyUnavailable()
        {
            InitializeFactoryMethod();
        }

        public IWebUI Create(PromptBehavior promptBehavior, object ownerWindow)
        {
            InitializeFactoryMethod();

            object[] parameters = { promptBehavior };
            IWebUI dialog = (IWebUI)dialogFactory.Invoke(null, parameters);
            dialog.OwnerWindow = ownerWindow;
            return dialog;
        }

        private static void InitializeFactoryMethod()
        {
            if (null != dialogFactory)
            {
                return;
            }

            const string WebAuthenticationDialogClassName = "Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.BrowserDialogFactory";
            const string FactoryMethodName = "CreateAuthenticationDialog";

            string webAuthenticationDialogAssemblyName = string.Format(WebAuthenticationDialogAssemblyNameTemplate, AdalIdHelper.GetAdalVersion());

            try
            {
                Assembly webAuthenticationDialogAssembly = Assembly.Load(webAuthenticationDialogAssemblyName);
                Type dialogFactoryType = webAuthenticationDialogAssembly.GetType(WebAuthenticationDialogClassName);
                dialogFactory = dialogFactoryType.GetMethod(FactoryMethodName, BindingFlags.Static | BindingFlags.NonPublic);
            }
            catch (FileNotFoundException ex)
            {
                ThrowAssemlyLoadFailedException(webAuthenticationDialogAssemblyName, ex);
            }
            catch (FileLoadException ex)
            {
                ThrowAssemlyLoadFailedException(webAuthenticationDialogAssemblyName, ex);
            }
        }

        private static void ThrowAssemlyLoadFailedException(string webAuthenticationDialogAssemblyName, Exception innerException)
        {
            throw new AdalException(AdalError.AssemblyLoadFailed,
                string.Format(CultureInfo.InvariantCulture, AdalErrorMessage.AssemblyLoadFailedTemplate, webAuthenticationDialogAssemblyName), innerException);
        }
    }
}
