//----------------------------------------------------------------------
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

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Microsoft.IdentityModel.Clients.ActiveDirectory")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("ff47962a-d498-4c63-b7e9-4db3653ad7db")]

// Assembly version information is in file ADAL.Common\CommonAssemblyInfo.cs
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]
[assembly: InternalsVisibleTo("Microsoft.IdentityModel.Clients.ActiveDirectory.Platform, PublicKey=002400000c80000014010000060200000024000052534131000800000100010085aad0bef0688d1b994a0d78e1fd29fc24ac34ed3d3ac3fb9b3d0c48386ba834aa880035060a8848b2d8adf58e670ed20914be3681a891c9c8c01eef2ab22872547c39be00af0e6c72485d7cfd1a51df8947d36ceba9989106b58abe79e6a3e71a01ed6bdc867012883e0b1a4d35b1b5eeed6df21e401bb0c22f2246ccb69979dc9e61eef262832ed0f2064853725a75485fa8a3efb7e027319c86dec03dc3b1bca2b5081bab52a627b9917450dfad534799e1c7af58683bdfa135f1518ff1ea60e90d7b993a6c87fd3dd93408e35d1296f9a7f9a97c5db56c0f3cc25ad11e9777f94d138b3cea53b9a8331c2e6dcb8d2ea94e18bf1163ff112a22dbd92d429a")]
[assembly: InternalsVisibleTo("Test.ADAL.NET.Unit, PublicKey=002400000c80000014010000060200000024000052534131000800000100010085aad0bef0688d1b994a0d78e1fd29fc24ac34ed3d3ac3fb9b3d0c48386ba834aa880035060a8848b2d8adf58e670ed20914be3681a891c9c8c01eef2ab22872547c39be00af0e6c72485d7cfd1a51df8947d36ceba9989106b58abe79e6a3e71a01ed6bdc867012883e0b1a4d35b1b5eeed6df21e401bb0c22f2246ccb69979dc9e61eef262832ed0f2064853725a75485fa8a3efb7e027319c86dec03dc3b1bca2b5081bab52a627b9917450dfad534799e1c7af58683bdfa135f1518ff1ea60e90d7b993a6c87fd3dd93408e35d1296f9a7f9a97c5db56c0f3cc25ad11e9777f94d138b3cea53b9a8331c2e6dcb8d2ea94e18bf1163ff112a22dbd92d429a")]
[assembly: InternalsVisibleTo("WinFormsAutomationApp, PublicKey=002400000c80000014010000060200000024000052534131000800000100010085aad0bef0688d1b994a0d78e1fd29fc24ac34ed3d3ac3fb9b3d0c48386ba834aa880035060a8848b2d8adf58e670ed20914be3681a891c9c8c01eef2ab22872547c39be00af0e6c72485d7cfd1a51df8947d36ceba9989106b58abe79e6a3e71a01ed6bdc867012883e0b1a4d35b1b5eeed6df21e401bb0c22f2246ccb69979dc9e61eef262832ed0f2064853725a75485fa8a3efb7e027319c86dec03dc3b1bca2b5081bab52a627b9917450dfad534799e1c7af58683bdfa135f1518ff1ea60e90d7b993a6c87fd3dd93408e35d1296f9a7f9a97c5db56c0f3cc25ad11e9777f94d138b3cea53b9a8331c2e6dcb8d2ea94e18bf1163ff112a22dbd92d429a")]

// Allow this assembly to be serviced when run on desktop CLR
[assembly: AssemblyMetadata("Serviceable", "True")]
