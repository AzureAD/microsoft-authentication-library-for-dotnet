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

using System.Runtime.CompilerServices;

// The core test project is the only one allowed to reference Core.dll for convenience. Do not add any other entries here, except for test.
[assembly: InternalsVisibleTo("Test.Microsoft.Identity.Unit.Infrastructure, PublicKey=00240000048000009400000006020000002400005253413100040000010001002d96616729b54f6d013d71559a017f50aa4861487226c523959d1579b93f3fdf71c08b980fd3130062b03d3de115c4b84e7ac46aef5e192a40e7457d5f3a08f66ceab71143807f2c3cb0da5e23b38f0559769978406f6e5d30ceadd7985fc73a5a609a8b74a1df0a29399074a003a226c943d480fec96dbec7106a87896539ad")]
[assembly: InternalsVisibleTo("Test.Microsoft.Identity.Core.net45.Unit, PublicKey=00240000048000009400000006020000002400005253413100040000010001002d96616729b54f6d013d71559a017f50aa4861487226c523959d1579b93f3fdf71c08b980fd3130062b03d3de115c4b84e7ac46aef5e192a40e7457d5f3a08f66ceab71143807f2c3cb0da5e23b38f0559769978406f6e5d30ceadd7985fc73a5a609a8b74a1df0a29399074a003a226c943d480fec96dbec7106a87896539ad")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]