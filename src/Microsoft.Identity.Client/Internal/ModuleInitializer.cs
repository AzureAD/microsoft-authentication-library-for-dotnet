// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Internal
{
    /// <summary>
    ///     Initializes the MSAL module. This can be considered an entry point into MSAL
    ///     for initialization purposes.
    /// </summary>
    /// <remarks>
    ///     The CLR defines a module initializer, however this is not implemented in C# and to
    ///     use this it would require IL weaving, which does not seem to work on all target frameworks.
    ///     Instead, call <see cref="EnsureModuleInitialized" /> from static ctors of public entry points.
    /// </remarks>
    internal class ModuleInitializer
    {
        private static volatile bool _isInitialized = false;
        private static readonly object LockObj = new object();

        /// <summary>
        ///     Handle all the initialization of singletons, factories, statics etc. Initialization will only happen once.
        /// </summary>
        public static void EnsureModuleInitialized()
        {
            // double check locking instead locking first to improve performace
            if (!_isInitialized)
            {
                lock (LockObj)
                {
                    if (!_isInitialized)
                    {
                        InitializeModule();
                    }
                }
            }
        }

        /// <summary>
        ///     Force initialization of the module, ignoring any previous initializations. Only TESTS should call this method.
        /// </summary>
        /// <remarks>
        ///     Tests can access the internals of the module and modify the initialization pattern, so it is
        ///     acceptable for tests to reinitialize the module.
        /// </remarks>
        public static void ForceModuleInitializationTestOnly()
        {
            lock (LockObj)
            {
                InitializeModule();
            }
        }

        private static void InitializeModule()
        {
            CoreExceptionFactory.Instance = new MsalExceptionFactory();

            // initialize the telemetry instance so statics get created
            Telemetry.GetInstance();

            CoreLoggerBase.Default = new MsalLogger(Guid.Empty, null);
            _isInitialized = true;
        }
    }
}