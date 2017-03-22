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
    public partial class LogPage : ContentPage
    {
        static Label l;

        public LogPage()
        {
            InitializeComponent();
            log.Text = "init";

            l = log;
        }

        static public void AddToLog(string str)
        {
            l.Text = l.Text + System.Environment.NewLine + str;
        }
    }
}
