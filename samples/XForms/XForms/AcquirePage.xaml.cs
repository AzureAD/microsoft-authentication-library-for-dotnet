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

        protected override void OnAppearing()
        {
            App.PCA.PlatformParameters = platformParameters;
        }

        private async void OnAcquireClicked(object sender, EventArgs e)
        {

            if (App.PCA.PlatformParameters == null)
            {
                OnAppearing();
            }


            // execute any acquire call

            // update 
            acquireResponseLabel.Text = "acquire call Start";
            LogPage.AddToLog("hello from Acquire page");

            // PublicClientApplication app = new PublicClientApplication("5a434691-ccb2-4fd1-b97b-b64bcfbc03fc")
            // {
            //      RedirectUri = "adaliosxformsapp://com.yourcompany.xformsapp"
            // };


            /*
            Device.OnPlatform(Android: () =>
            {
                Xamarin.Forms.Platform;
                var renderer = Platform.GetRenderer(this);

                if (renderer == null)
                {
                    renderer = RendererFactory.GetRenderer(page);
                    Platform.SetRenderer(page, renderer);
                }
                var viewController = renderer.ViewController;

                //Activity androidActivity;

                IPlatformParameters androidParams = new PlatformParameters();

                app.PlatformParameters = androidParams;
            });
            */

            try
            {
                AuthenticationResult res = await App.PCA.AcquireTokenAsync(App.Scopes);

                acquireResponseLabel.Text = "Result - " + res;

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

