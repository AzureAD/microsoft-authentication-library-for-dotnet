using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace AutomationApp
{
    public partial class ResultPage : ContentPage
    {
        public ResultPage(string result)
        {
            ScrollView scrollView = new ScrollView
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Content = new Label
                {
                    Text = result,
                    AutomationId = "resultLabel",
                    FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                }
            };

            // Build the page.
            this.Content = new StackLayout
            {
                Children =
                {
                    scrollView
                }
            };
        }
    }
}
