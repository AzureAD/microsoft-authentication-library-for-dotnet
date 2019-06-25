// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XamarinAutomationApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LogPage : ContentPage
    {
        private static readonly StringBuilder s_sb = new StringBuilder();
        private static readonly StringBuilder s_sbPii = new StringBuilder();
        private static readonly object s_bufferLock = new object();
        private static readonly object s_bufferLockPii = new object();

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
            lock (s_bufferLock)
            {
                log.Text = s_sb.ToString();
            }
            lock (s_bufferLockPii)
            {
                logPii.Text = s_sbPii.ToString();
            }
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            lock (s_bufferLock)
            {
                s_sb.Clear();
            }
            lock (s_bufferLockPii)
            {
                s_sbPii.Clear();
            }
            ShowLog();
        }

        public static void AddToLog(string str, bool containsPii)
        {
            if (containsPii)
            {
                lock (s_bufferLockPii)
                {
                    s_sbPii.AppendLine(str);
                }
            }
            else
            {
                lock (s_bufferLock)
                {
                    s_sb.AppendLine(str);
                }
            }
        }
    }
}
