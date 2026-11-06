// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WAMMauiApp;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState activationState)
	{
		return new Window(new AppShell());
	}
}
