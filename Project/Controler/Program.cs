using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace OperatingSystemAnalyst
{
    static class Program
    {
        private static Launcher launcher;

        [STAThread]
        static void Main()
        {
            //OperatingSystemAnalyst kl = new OperatingSystemAnalyst();
            //kl.Enabled = true;
            //Console.ReadLine();
            //kl.Flush2File("test.txt");
            
            launcher = new Launcher();
            launcher.Launch();
        }
    }
}


