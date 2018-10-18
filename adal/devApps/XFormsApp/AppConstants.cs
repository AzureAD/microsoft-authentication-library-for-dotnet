using System;
using System.Collections.Generic;
using System.Text;

namespace XFormsApp
{
    public static class AppConstants
    {
        //Applications
        public const string UiAutomationTestClientId = "3c1e0e0d-b742-45ba-a35e-01c664e14b16";
        public const string MSIDLAB4ClientId = "4b0db8c2-9f26-4417-8bde-3f0e3656f8e0";

        //Resources
        public const string UiAutomationTestResource = "ae55a6cc-da5e-42f8-b75d-c37e41a1a0d9";
        public const string MSGraph = "https://graph.microsoft.com";
        public const string Exchange = "https://outlook.office365.com/";
        public const string SharePoint = "https://microsoft.sharepoint-df.com/ ";

        static AppConstants()
        {
            //Adding default applications and resources to make testing easier by removing the need to rebuild the application 
            //whenever a user wants to change a resource. You can add new applications and resources here and they will be available via 
            //drop down when the app runs.
            LabelToApplicationUriMap = new Dictionary<string, string>();
            LabelToApplicationUriMap.Add("Ui Test App", UiAutomationTestClientId);
            LabelToApplicationUriMap.Add("MSID Lab 4", MSIDLAB4ClientId);

            LabelToResourceUriMap = new Dictionary<string, string>();
            LabelToResourceUriMap.Add("MS Graph", MSGraph);
            LabelToResourceUriMap.Add("Ui Test Resource", UiAutomationTestResource);
            LabelToResourceUriMap.Add("Exchange", Exchange);
            LabelToResourceUriMap.Add("SharePoint", SharePoint);
        }

        public static Dictionary<string, string> LabelToApplicationUriMap { get; set; }
        public static Dictionary<string, string> LabelToResourceUriMap { get; set; }
    }
}
