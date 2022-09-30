// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Microsoft.Identity.Client.Platforms.Features.WamBroker.win32
{
    internal partial class Splash : Form
    {
        private readonly IWin32Window _parentWindow;

        public Splash(IWin32Window parentWindow)
        {
            Application.EnableVisualStyles();
            InitializeComponent();
            _parentWindow = parentWindow;
        }
    }
}
