using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Glossary
{
    static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            mainWindowForm mainWindow = new mainWindowForm();
            Application.Run(mainWindow);
        }
    }
}
