// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace MauiB2C;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new AppShell();
	}
}
