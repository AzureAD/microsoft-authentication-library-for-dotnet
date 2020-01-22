// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
#if ANDROID
using Android;
#endif
#if iOS
using Foundation;
#endif

// Version and Metadata are set at build time from msbuild properties defined in the csproj

[assembly: AssemblyMetadata("Serviceable", "True")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
#if ANDROID || iOS
[assembly: LinkerSafe]
#endif
