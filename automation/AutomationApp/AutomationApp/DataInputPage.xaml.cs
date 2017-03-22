using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Xamarin.Forms;

namespace AutomationApp
{
    public partial class DataInputPage : ContentPage
    {
        public DataInputPage(App.Command command)
        {
            Label inputLabel = new Label
            {
                Text = "",
                AutomationId = "inputLabel",
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof (Label)),
            };

            ScrollView scrollView = new ScrollView
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Content = inputLabel
            };

            Button go =
                new Button()
                {
                    Text = "GO!",
                    TextColor = Color.Black,
                    BackgroundColor = Color.Lime
                };

            go.Clicked += (sender, e) => { string output = command(AuthenticationHelper.CreateDictionaryFromJson(inputLabel.Text));
                                             Navigation.PushModalAsync(new ResultPage(output));
            };

            // Build the page.
            this.Content = new StackLayout
            {
                Children =
                {
                    scrollView,
                    go
                }
            };
        }
    }
}
