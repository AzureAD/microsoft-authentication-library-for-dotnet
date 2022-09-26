// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace MauiAppWithBroker
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            MainPage = new AppShell();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            System.Diagnostics.Debug.WriteLine(ex?.Message);
        }
    }
}
