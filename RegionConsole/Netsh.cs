using System;
using System.Diagnostics;
using System.Text;

namespace RegionConsole
{
    public sealed class NetshResult
    {
        public int ExitCode { get; }
        public string Output { get; }
        public NetshResult(int exitCode, string output)
        {
            ExitCode = exitCode;
            Output = output;
        }
    }

    /// <summary>
    /// Обёртка над командой netsh. Вывод декодируется в кодировке OEM (866),
    /// которая используется консолью Windows на русскоязычных системах (в т.ч. Windows 8.1).
    /// </summary>
    public static class Netsh
    {
        private static readonly Encoding OemEncoding;

        static Netsh()
        {
            try
            {
                OemEncoding = Encoding.GetEncoding(866);
            }
            catch
            {
                OemEncoding = Encoding.Default;
            }
        }

        public static NetshResult Run(string arguments, int timeoutMs = 20000)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = OemEncoding,
                StandardErrorEncoding = OemEncoding
            };

            Process process;
            try
            {
                process = Process.Start(psi);
            }
            catch (Exception ex)
            {
                return new NetshResult(-1, "Не удалось запустить netsh: " + ex.Message);
            }

            string output = "";
            string error = "";
            int exitCode = -1;
            try
            {
                output = process.StandardOutput.ReadToEnd();
                error = process.StandardError.ReadToEnd();
                bool exited = process.WaitForExit(timeoutMs);
                if (!exited)
                {
                    try { process.Kill(); }
                    catch { /* ignore */ }
                }
                exitCode = process.ExitCode;
            }
            catch (Exception ex)
            {
                output = "";
                error = "Ошибка чтения вывода netsh: " + ex.Message;
            }
            finally
            {
                process.Dispose();
            }

            return new NetshResult(exitCode, output + error);
        }
    }
}
