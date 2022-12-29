// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Xamarin_Manual
{
    public partial class App : Application
    {
      

        public App()
        {
           

            InitializeComponent();

            MainPage = new MainPage();
        }

    }
}
