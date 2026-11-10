using System;
using System.Text.RegularExpressions;

namespace RegionConsole
{
    /// <summary>
    /// Проверка введённых пользователем данных. Пароль никогда не сохраняется и не пишется в журнал.
    /// </summary>
    public static class Validators
    {
        private static readonly Regex ControlChars = new Regex(@"\p{C}", RegexOptions.Compiled);

        public static bool ValidateSsid(string ssid, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(ssid))
            {
                error = "Имя сети (SSID) не может быть пустым.";
                return false;
            }
            var value = ssid.Trim();
            if (value.Length > 32)
            {
                error = "SSID не может превышать 32 символа.";
                return false;
            }
            if (ControlChars.IsMatch(value))
            {
                error = "SSID содержит недопустимые управляющие символы.";
                return false;
            }
            return true;
        }

        public static bool ValidatePassword(string password, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(password))
            {
                error = "Пароль не может быть пустым.";
                return false;
            }
            if (password.Length < 8)
            {
                error = "Пароль слишком короткий (минимум 8 символов для WPA2-PSK).";
                return false;
            }
            if (password.Length > 63)
            {
                error = "Пароль слишком длинный (максимум 63 символа для WPA2-PSK).";
                return false;
            }
            if (ControlChars.IsMatch(password))
            {
                error = "Пароль содержит недопустимые управляющие символы.";
                return false;
            }
            return true;
        }

        public static bool ValidateDistinct(string adapterA, string adapterB, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(adapterA) || string.IsNullOrWhiteSpace(adapterB))
            {
                error = "Выберите оба адаптера: источник интернета и получатель.";
                return false;
            }
            if (adapterA.Trim().Equals(adapterB.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                error = "Исходный и получающий адаптеры не могут быть одинаковыми.";
                return false;
            }
            return true;
        }
    }
}
