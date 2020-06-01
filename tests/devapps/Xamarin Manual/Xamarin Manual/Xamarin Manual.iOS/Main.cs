using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

namespace Xamarin_Manual.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");

            throw new NotImplementedException("The iOS project has not been tested and will likely not work because of redirect uri issues");
        }
    }
}
