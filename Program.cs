using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Umaru_AI
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
            if (File.Exists("Matrix.dll"))
            {
                Application.Run(new Main());
            }
            else
            {
                byte[] myfile = Properties.Resources.Matrix;
                File.WriteAllBytes("Matrix.dll",myfile);
                Application.Run(new Main());
            }
            
        }
       
    }
}
