// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LogPage : ContentPage
    {
        private static readonly StringBuilder Sb = new StringBuilder();
        private static readonly StringBuilder SbPii = new StringBuilder();
        private static readonly object BufferLock = new object();
        private static readonly object BufferLockPii = new object();


        public LogPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            ShowLog();
        }

        private void ShowLog()
        {
            lock (BufferLock)
            {
                log.Text = Sb.ToString();
            }
            lock (BufferLockPii)
            {
                logPii.Text = SbPii.ToString();
            }
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            lock (BufferLock)
            {
                Sb.Clear();
            }
            lock (BufferLockPii)
            {
                SbPii.Clear();
            }
            ShowLog();
        }

        public static void AddToLog(string str, bool containsPii)
        {
            if (containsPii)
            {
                lock (BufferLockPii)
                {
                    SbPii.AppendLine(str);
                }
            }
            else
            {
                lock (BufferLock)
                {
                    Sb.AppendLine(str);
                }
            }
        }
    }
}
