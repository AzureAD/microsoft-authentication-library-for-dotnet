// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Identity.Client.Broker;
using Interop = Microsoft.Identity.Client.NativeInterop;

namespace NetDesktopWinForms
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            CheckMemoryLeak();
        }

        internal static void CheckMemoryLeak()
        {
            const string InteropAssemblyName = "Microsoft.Identity.Client.NativeInterop";
            const string IdentityClientBrokerAssemblyName = "Microsoft.Identity.Client.Broker";

            Assembly assemblyBroker = GetAssembly(IdentityClientBrokerAssemblyName);

            if (assemblyBroker != null)
            {
                Assembly interOpAssembly = null;
                interOpAssembly = GetAssembly(InteropAssemblyName);

                // check the mem leak
                CheckMemLeak(interOpAssembly, 1);

                // Dispose off core from static
                Type runtimeBrokerType = assemblyBroker.GetType("Microsoft.Identity.Client.Broker.RuntimeBroker");
                
                FieldInfo coreField = runtimeBrokerType.GetField("s_lazyCore", BindingFlags.Static | BindingFlags.NonPublic);
                Lazy<Microsoft.Identity.Client.NativeInterop.Core> core = (Lazy<Microsoft.Identity.Client.NativeInterop.Core>)coreField.GetValue(null);
                core.Value.Dispose();

                // check the mem leak again
                CheckMemLeak(interOpAssembly, 0);
            }
        }

        private static void CheckMemLeak(Assembly interOpAssembly, int expectedCount)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Type moduleType = interOpAssembly.GetType("Microsoft.Identity.Client.NativeInterop.Module");
            PropertyInfo handleCountProp = moduleType.GetProperty("HandleCount", BindingFlags.Static | BindingFlags.Public);
            int count = (int)handleCountProp.GetValue(null);
            Debug.WriteLine($"Count is {count}");
            if (count != expectedCount)
            {
                throw new Exception($"HandleCount Expected = {expectedCount} Actual = {count}");
            }
        }

        private static Assembly GetAssembly(string assemblyName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Debug.WriteLine($"Assembly Name {assembly.FullName}");
                if (assembly.FullName.StartsWith(assemblyName))
                {
                    return assembly;
                }
            }

            return null;
        }
    }
}
