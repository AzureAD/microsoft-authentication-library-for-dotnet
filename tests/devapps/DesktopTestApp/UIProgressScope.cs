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
using System.Windows.Forms;

namespace DesktopTestApp
{
    public partial class MainForm
    {
#pragma warning disable CA1034 // Nested types should not be visible
        public class UIProgressScope : IDisposable
#pragma warning restore CA1034 // Nested types should not be visible
        {
            MainForm mainForm;

            public UIProgressScope(MainForm mainForm)
            {
                this.mainForm = mainForm;
                this.mainForm.Enabled = false;
                this.mainForm.progressBar1.Style = ProgressBarStyle.Marquee;
                this.mainForm.progressBar1.MarqueeAnimationSpeed = 30;
            }

            #region IDisposable Support


            public void Dispose()
            {
                this.mainForm.Enabled = true;
                this.mainForm.progressBar1.Style = ProgressBarStyle.Continuous;
                this.mainForm.progressBar1.MarqueeAnimationSpeed = 0;
            }

            #endregion
        }
    }
}