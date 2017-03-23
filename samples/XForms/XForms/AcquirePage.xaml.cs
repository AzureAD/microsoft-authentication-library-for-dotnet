using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Identity.Client;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AcquirePage : ContentPage
    {
        public IPlatformParameters platformParameters { get; set; }

        public AcquirePage()
        {
            InitializeComponent();
        }

        private void SetPlatformParameters()
        {
            App.PCA.PlatformParameters = platformParameters;
        }

        protected override void OnAppearing()
        {
            SetPlatformParameters();
        }

        private async void OnAcquireClicked(object sender, EventArgs e)
        {

            if (App.PCA.PlatformParameters == null)
            {
                SetPlatformParameters();
            }

            try
            {
                AuthenticationResult res = await App.PCA.AcquireTokenAsync(App.Scopes);

                acquireResponseLabel.Text = "Result - " + res.AccessToken;

            }
            catch (MsalException exception)
            {
                acquireResponseLabel.Text = "MsalException - " + exception;
            }
            catch (Exception exception)
            {
                acquireResponseLabel.Text = "Exception - " + exception;
            }

        }
    }
}

