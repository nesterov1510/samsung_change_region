using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace RegionConsole
{
    /// <summary>
    /// Движок визуальной симуляции терминала в стиле фильмов про хакеров.
    /// Не взаимодействует с сетью и не выполняет никаких реальных действий.
    /// </summary>
    public sealed class TerminalEngine
    {
        private static readonly string[] ScanWords =
        {
            "интерфейс", "порт", "MAC-адрес", "ARP-запрос", "сетевой пакет", "шлюз",
            "DNS-запрос", "удалённый узел", "SSID", "канал", "частотa", "handshake",
            "шифрование WPA2", "шум линии", "широковещательный"
        };

        private static readonly string[] ComputeWords =
        {
            "хэш SHA-256", "CRC32", "RSA-2048", "сессионный ключ", "цифровая подпись",
            "модуль ключа", "смещение реестра", "база 0x7FF", "регистр EAX", "байт",
            "флаг нуля", "адрес возврата", "счётчик команд"
        };

        private readonly Random _rand = new Random(Environment.TickCount);

        private sealed class Stream
        {
            public bool Enabled;
            public string Tag;
            public Color Color;
            public string[] Words;
        }

        private readonly List<Stream> _streams = new List<Stream>();

        public event Action<string, Color> Line;
        public int TotalEmitted { get; private set; }

        public int ActiveStreamCount
        {
            get
            {
                int n = 0;
                foreach (var s in _streams)
                    if (s.Enabled) n++;
                return n;
            }
        }

        public bool IsRunning => ActiveStreamCount > 0;

        public void StartScan() => AddStream("СКАН", Color.FromArgb(0, 255, 136), ScanWords);
        public void StartCompute() => AddStream("ВЫЧИСЛ", Color.FromArgb(64, 224, 208), ComputeWords);

        public void Stop()
        {
            foreach (var s in _streams) s.Enabled = false;
        }

        private void AddStream(string tag, Color color, string[] words)
        {
            _streams.Add(new Stream { Enabled = true, Tag = tag, Color = color, Words = words });
        }

        public void Tick()
        {
            foreach (var s in _streams)
            {
                if (!s.Enabled) continue;
                if (_rand.Next(0, 100) < 65) continue;
                int count = _rand.Next(1, 4);
                for (int i = 0; i < count; i++) Emit(s);
            }
        }

        private void Emit(Stream s)
        {
            TotalEmitted++;
            int kind = _rand.Next(0, 7);
            string word = s.Words[_rand.Next(s.Words.Length)];
            string body;
            switch (kind)
            {
                case 0: body = word + ": " + RandomHex(4); break;
                case 1: body = word + ": 0x" + RandomHex(4); break;
                case 2: body = " пакетов: " + _rand.Next(100, 9999); break;
                case 3: body = " " + _rand.Next(0, 101) + "%"; break;
                case 4:
                    body = " 192.168." + _rand.Next(0, 256) + "." + _rand.Next(1, 256) +
                           " — " + (_rand.Next(0, 2) == 0 ? "OK" : "timeout");
                    break;
                case 5: body = " " + RandomMac(); break;
                default: body = " " + word; break;
            }
            string line = "[" + Timestamp() + "] [" + s.Tag + "]" + body;
            Line?.Invoke(line, s.Color);
        }

        private string RandomHex(int chars)
        {
            const string h = "0123456789ABCDEF";
            var sb = new StringBuilder(chars);
            for (int i = 0; i < chars; i++) sb.Append(h[_rand.Next(16)]);
            return sb.ToString();
        }

        private string RandomMac()
        {
            var parts = new string[6];
            for (int i = 0; i < 6; i++) parts[i] = RandomHex(2);
            return string.Join(":", parts);
        }

        private string Timestamp()
        {
            var now = DateTime.Now;
            return now.Hour.ToString("D2") + ":" + now.Minute.ToString("D2") + ":" +
                   now.Second.ToString("D2") + "." + (now.Millisecond / 100).ToString();
        }
    }
}
