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

[assembly: AssemblyTitle("Microsoft.Identity.Client.Platform")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]
 
// Allow this assembly to be serviced when run on desktop CLR
[assembly: AssemblyMetadata("Serviceable", "True")]

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: Guid("ff47962a-d498-4c63-b7e9-4db3653ad7dc")]

// Assembly version information is in file MSAL.Common\CommonAssemblyInfo.cs

[assembly:
    InternalsVisibleTo(
        "Test.MSAL.NET.Unit, PublicKey=00240000048000009400000006020000002400005253413100040000010001002D96616729B54F6D013D71559A017F50AA4861487226C523959D1579B93F3FDF71C08B980FD3130062B03D3DE115C4B84E7AC46AEF5E192A40E7457D5F3A08F66CEAB71143807F2C3CB0DA5E23B38F0559769978406F6E5D30CEADD7985FC73A5A609A8B74A1DF0A29399074A003A226C943D480FEC96DBEC7106A87896539AD"
        )]