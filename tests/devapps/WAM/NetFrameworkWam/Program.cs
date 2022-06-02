using System;
using System.Windows.Forms;
using Microsoft.Identity.Client.NativeInterop;

namespace NetDesktopWinForms
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            Core.VerifyHandleLeaksForTest();
        }
    }
}
