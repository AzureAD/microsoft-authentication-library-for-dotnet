// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
