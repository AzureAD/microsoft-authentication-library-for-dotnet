using Microsoft.UI.Xaml;
using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;

namespace WinUI3TestApp;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }

    private Window m_window;
}
