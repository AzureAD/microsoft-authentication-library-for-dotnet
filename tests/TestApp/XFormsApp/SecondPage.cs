using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using TestApp.PCL;
using Xamarin.Forms;

namespace XFormsApp
{
    public class SecondPage : ContentPage
    {
        private TokenBroker tokenBroker;
        private Label result;

        public SecondPage()
        {
            this.tokenBroker = new TokenBroker();

            var browseButton = new Button
            {
                Text = "Acquire Token"
            };

            var clearButton = new Button
            {
                Text = "Clear Cache"
            };

            result = new Label { };

            browseButton.Clicked += browseButton_Clicked;
            clearButton.Clicked += clearButton_Clicked;

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children = {
                    browseButton,
                    clearButton,
                    result
				}
            };
        }

        public IPlatformParameters Paramters { get; set; }

        async void browseButton_Clicked(object sender, EventArgs e)
        {
            this.result.Text = string.Empty;
            string token = await tokenBroker.GetTokenInteractiveAsync(Paramters);
            this.result.Text = token;
        }

        void clearButton_Clicked(object sender, EventArgs e)
        {
            this.result.Text = this.result.Text = "Cache items before clear: " + TokenCache.DefaultShared.Count;
            tokenBroker.ClearTokenCache();
            this.result.Text = "Cache items after clear: " + TokenCache.DefaultShared.Count;
        }
    }
}
