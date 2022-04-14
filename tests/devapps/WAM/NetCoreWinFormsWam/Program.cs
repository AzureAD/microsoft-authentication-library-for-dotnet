using System;
using System.Windows.Forms;
using Microsoft.Identity.Client.NativeInterop;
using Microsoft.Identity.Client.Utils.Windows;

namespace NetDesktopWinForms
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (WindowsNativeUtils.IsElevatedUser())
            {
                WindowsNativeUtils.InitializeProcessSecurity();
            }
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            Core.VerifyHandleLeaksForTest();
        }
    }
}
