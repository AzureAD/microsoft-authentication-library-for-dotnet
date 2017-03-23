using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CachePage : ContentPage
    {
        public CachePage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            DateTime dateTime = DateTime.UtcNow;
            cache.Text = dateTime.ToString();
        }
    }
}

