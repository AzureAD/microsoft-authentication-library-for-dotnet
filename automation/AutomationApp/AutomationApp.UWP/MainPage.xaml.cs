
namespace AutomationApp.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();

            LoadApplication(new AutomationApp.App());
        }
    }
}
