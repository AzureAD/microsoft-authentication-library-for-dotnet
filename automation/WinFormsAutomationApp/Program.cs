using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace WinFormsAutomationApp
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

            KillOtherInstances();

            Application.Run(new MainForm());
        }

        /// <summary>
        /// Kill all other instances of this application.
        /// </summary>
        private static void KillOtherInstances()
        {
            var thisProcess = Process.GetCurrentProcess();

            var otherProcesses = Process.GetProcessesByName(thisProcess.ProcessName)
                                        .Where(x => x.Id != thisProcess.Id);

            foreach (var otherProcess in otherProcesses)
            {
                try
                {
                    otherProcess.Kill();
                }
                catch
                {
                    // continue; not much we can do here.
                }
            }
        }
    }
}
