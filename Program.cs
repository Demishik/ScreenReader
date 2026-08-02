using System;
using System.Windows.Forms;

namespace ScreenReader
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Application.Run(new Form
            {
                Text = "ScreenReader",
                Width = 800,
                Height = 500
            });
        }
    }
}
