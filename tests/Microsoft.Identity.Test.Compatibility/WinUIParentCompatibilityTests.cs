// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Compatibility
{
    [TestClass]
    public class WinUIParentCompatibilityTests
    {
        private static Type s_windowType;
        private static Type s_derivedWindowType;
        private static ResolveEventHandler s_assemblyResolver;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            // A type-identity double exercises the existing WinUI dispatch without starting native UI.
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("Microsoft.WinUI"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("Microsoft.WinUI");
            s_windowType = module.DefineType("Microsoft.UI.Xaml.Window", TypeAttributes.Public).CreateType();
            s_derivedWindowType = module.DefineType(
                "CompatibilityWindow", TypeAttributes.Public, s_windowType).CreateType();
            s_assemblyResolver = (_, args) =>
                string.Equals(new AssemblyName(args.Name).Name, "Microsoft.WinUI", StringComparison.Ordinal)
                    ? s_windowType.Assembly
                    : null;
            AppDomain.CurrentDomain.AssemblyResolve += s_assemblyResolver;
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= s_assemblyResolver;
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WinUIWindowIsRetainedAsInteractiveOwner(bool derived)
        {
            // Arrange
            object window = Activator.CreateInstance(derived ? s_derivedWindowType : s_windowType);
            IPublicClientApplication app = PublicClientApplicationBuilder
                .Create("33333333-3333-3333-3333-333333333333")
                .Build();

            // Act
            AcquireTokenInteractiveParameterBuilder builder = app.AcquireTokenInteractive(new[] { "scope" })
                .WithParentActivityOrWindow(window);
            var parameters = (AcquireTokenInteractiveParameters)typeof(AcquireTokenInteractiveParameterBuilder)
                .GetProperty("Parameters", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(builder);

            // Assert
            Assert.AreSame(window, parameters.UiParent.OwnerWindow);
        }

        [TestMethod]
        [SupportedOSPlatform("windows")]
        public async Task ReleasedWinUICoreTypesResolveAndSchedulerRunsOnStaAsync()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("The released WinUI extension requires Windows.");
            }

            // Arrange
            Assembly core = typeof(PublicClientApplication).Assembly;
            const string SchedulerTypeName = "Microsoft.Identity.Client.Platforms.Features.DesktopOs.StaTaskScheduler";
            using var stream = File.OpenRead(Path.Combine(
                AppContext.BaseDirectory, "released-winui3", "Microsoft.Identity.Client.Desktop.WinUI3.dll"));
            using var image = new PEReader(stream);
            MetadataReader metadata = image.GetMetadataReader();
            bool referencesScheduler = false;

            // Act
            foreach (TypeReferenceHandle handle in metadata.TypeReferences)
            {
                TypeReference reference = metadata.GetTypeReference(handle);
                if (reference.ResolutionScope.Kind != HandleKind.AssemblyReference)
                {
                    continue;
                }

                AssemblyReference assembly = metadata.GetAssemblyReference(
                    (AssemblyReferenceHandle)reference.ResolutionScope);
                if (!string.Equals(metadata.GetString(assembly.Name), core.GetName().Name, StringComparison.Ordinal))
                {
                    continue;
                }

                string typeName = metadata.GetString(reference.Namespace) + "." + metadata.GetString(reference.Name);
                Assert.IsNotNull(core.GetType(typeName, throwOnError: true));
                referencesScheduler |= string.Equals(typeName, SchedulerTypeName, StringComparison.Ordinal);
            }

            Assert.IsTrue(referencesScheduler, "The released WinUI assembly must exercise the scheduler contract.");
            Type schedulerType = core.GetType(SchedulerTypeName, throwOnError: true);
            using var scheduler = (IDisposable)Activator.CreateInstance(schedulerType, new object[] { 1 });
            ApartmentState apartment = await Task.Factory.StartNew(
                () => Thread.CurrentThread.GetApartmentState(),
                CancellationToken.None, TaskCreationOptions.None, (TaskScheduler)scheduler).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(ApartmentState.STA, apartment);
        }
    }
}
