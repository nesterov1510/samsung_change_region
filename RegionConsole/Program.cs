using System;
using System.Windows.Forms;

namespace RegionConsole
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (sender, e) =>
            {
                try
                {
                    MessageBox.Show("Необработанное исключение: " + e.Exception.Message,
                        "Region Console \u2014 ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch
                {
                    // nothing
                }
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                try
                {
                    var ex = e.ExceptionObject as Exception;
                    MessageBox.Show("Критическая ошибка: " + (ex?.Message ?? "неизвестно"),
                        "Region Console \u2014 критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch
                {
                    // nothing
                }
            };

            Application.Run(new MainForm());
        }
    }
}
