using System;
using System.Collections.Generic;

namespace RegionConsole
{
    public sealed class IcsConnection
    {
        public string Name { get; set; }
        public bool SharingEnabled { get; set; }
        // 0 = приватный подключение (получает общий доступ), 1 = публичный (раздаёт свой интернет)
        public int SharingType { get; set; }
    }

    /// <summary>
    /// Управление общим доступом к интернету (ICS) через COM-компонент NetSharingManager.
    /// Работает только с правами администратора.
    /// </summary>
    public static class IcsManager
    {
        private const string ProgId = "NetSharingManager.NetSharingManager";
        private const int SharingPrivate = 0;
        private const int SharingPublic = 1;

        public static bool IsAvailable()
        {
            try
            {
                return Type.GetTypeFromProgID(ProgId, false) != null;
            }
            catch
            {
                return false;
            }
        }

        public static List<IcsConnection> GetConnections()
        {
            var list = new List<IcsConnection>();
            try
            {
                var type = Type.GetTypeFromProgID(ProgId, true);
                dynamic manager = Activator.CreateInstance(type);
                dynamic connections = manager.EveryConnection;
                foreach (dynamic connection in connections)
                {
                    try
                    {
                        dynamic config = connection.SharingConfiguration;
                        list.Add(new IcsConnection
                        {
                            Name = connection.Name,
                            SharingEnabled = config.SharingEnabled,
                            SharingType = (int)config.SharingConnectionType
                        });
                    }
                    catch
                    {
                        // пропускаем подключение, конфигурацию которого нельзя прочитать
                    }
                }
            }
            catch
            {
                // COM-компонент недоступен
            }
            return list;
        }

        public static (bool Success, string Message) EnableSharingOn(string connectionName, bool isPublic)
        {
            try
            {
                var type = Type.GetTypeFromProgID(ProgId, true);
                dynamic manager = Activator.CreateInstance(type);
                dynamic connections = manager.EveryConnection;
                foreach (dynamic connection in connections)
                {
                    if (string.Equals(connection.Name, connectionName, StringComparison.OrdinalIgnoreCase))
                    {
                        dynamic config = connection.SharingConfiguration;
                        config.EnableSharing(isPublic ? SharingPublic : SharingPrivate);
                        return (true, "Общий доступ включён.");
                    }
                }
                return (false, "Адаптер не найден в коллекции сетевых подключений.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static (bool Success, string Message) DisableSharingOn(string connectionName)
        {
            try
            {
                var type = Type.GetTypeFromProgID(ProgId, true);
                dynamic manager = Activator.CreateInstance(type);
                dynamic connections = manager.EveryConnection;
                foreach (dynamic connection in connections)
                {
                    if (string.Equals(connection.Name, connectionName, StringComparison.OrdinalIgnoreCase))
                    {
                        dynamic config = connection.SharingConfiguration;
                        config.DisableSharing();
                        return (true, "Общий доступ отключён.");
                    }
                }
                return (false, "Адаптер не найден в коллекции сетевых подключений.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static bool AnySharingEnabled(out string name, out bool isPublic)
        {
            name = null;
            isPublic = false;
            foreach (var c in GetConnections())
            {
                if (c.SharingEnabled)
                {
                    name = c.Name;
                    isPublic = c.SharingType == SharingPublic;
                    return true;
                }
            }
            return false;
        }
    }
}
