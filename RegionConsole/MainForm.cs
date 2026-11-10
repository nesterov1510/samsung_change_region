using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RegionConsole
{
    public sealed class MainForm : Form
    {
        // ---------- цветовая схема (тёмная техническая консоль) ----------
        private static readonly Color ColorBackground = Color.FromArgb(11, 22, 32);
        private static readonly Color ColorPanel = Color.FromArgb(18, 30, 46);
        private static readonly Color ColorSurface = Color.FromArgb(26, 42, 58);
        private static readonly Color ColorConsoleBg = Color.FromArgb(4, 7, 11);
        private static readonly Color ColorAccentGreen = Color.FromArgb(0, 255, 136);
        private static readonly Color ColorAccentCyan = Color.FromArgb(64, 224, 208);
        private static readonly Color ColorText = Color.FromArgb(230, 247, 255);
        private static readonly Color ColorMuted = Color.FromArgb(143, 179, 200);
        private static readonly Color ColorDanger = Color.FromArgb(255, 90, 90);
        private static readonly Font FontUi = new Font("Segoe UI", 9F, FontStyle.Regular);
        private static readonly Font FontUiBold = new Font("Segoe UI", 9F, FontStyle.Bold);
        private static readonly Font FontConsole = new Font("Consolas", 10F, FontStyle.Regular);

        private readonly TerminalEngine _engine = new TerminalEngine();
        private readonly Timer _animationTimer = new Timer { Interval = 40 };
        private readonly Timer _progressTimer = new Timer { Interval = 110 };
        private readonly Random _rand = new Random();
        private int _totalLines = 0;
        private int _progressValue = 0;

        // ---------- вкладка «Терминал» ----------
        private TabControl _tabs;
        private RichTextBox _console;
        private Button _btnScan;
        private Button _btnCompute;
        private Button _btnStop;
        private Button _btnClear;
        private Label _lblStatus;
        private ProgressBar _progressBar;
        private Label _lblLines;

        // ---------- вкладка «Wi-Fi + VPN» ----------
        private TextBox _txtSsid;
        private TextBox _txtPassword;
        private Button _btnStartAp;
        private Button _btnStopAp;
        private Label _lblApStatus;
        private ListBox _lstAdapters;
        private Button _btnRefreshAdapters;
        private Label _lblAdapterCount;
        private ComboBox _cbVpnSource;
        private ComboBox _cbVirtualReceiver;
        private CheckBox _chkReplaceIcs;
        private Button _btnIcsOn;
        private Button _btnIcsOff;
        private Label _lblIcsStatus;
        private RichTextBox _logNet;

        public MainForm()
        {
            Text = "Region Console";
            Size = new Size(1060, 820);
            MinimumSize = new Size(1000, 740);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ColorBackground;
            ForeColor = ColorText;
            Font = FontUi;
            Icon = LoadAppIcon();

            _animationTimer.Tick += AnimationTick;
            _progressTimer.Tick += ProgressTick;
            _engine.Line += (text, color) => AppendConsole(text, color);

            _tabs = new TabControl { Dock = DockStyle.Fill };
            var pageTerminal = new TabPage("Терминал") { BackColor = ColorBackground };
            var pageWifi = new TabPage("Wi-Fi + VPN") { BackColor = ColorBackground };

            pageTerminal.Controls.Add(BuildTerminalPanel());
            pageWifi.Controls.Add(BuildWifiPanel());

            _tabs.TabPages.Add(pageTerminal);
            _tabs.TabPages.Add(pageWifi);
            _tabs.SelectedIndexChanged += (s, e) =>
            {
                if (_tabs.SelectedTab == pageWifi) RefreshAdapters();
            };
            Controls.Add(_tabs);

            Load += (s, e) => RefreshAdapters();
            FormClosing += (s, e) =>
            {
                _animationTimer.Stop();
                _progressTimer.Stop();
                _engine.Stop();
            };
        }

        private Icon LoadAppIcon()
        {
            try
            {
                var asm = typeof(MainForm).Assembly;
                var name = asm.GetName().Name + ".Resources.region_console.ico";
                using (var stream = asm.GetManifestResourceStream(name))
                {
                    if (stream != null) return new Icon(stream);
                }
            }
            catch
            {
                // ignored — окно останется со стандартной иконкой
            }
            return null;
        }

        // ---------- сборка вкладки «Терминал» ----------
        private Panel BuildTerminalPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = ColorBackground };

            var bar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = ColorPanel };
            _btnScan = AccentButton("Сканирование", ColorAccentGreen, 14, 14, 130, 32, BtnScan_Click);
            _btnCompute = AccentButton("Вычисления", ColorAccentCyan, 152, 14, 130, 32, BtnCompute_Click);
            _btnStop = AccentButton("Стоп", ColorDanger, 290, 14, 100, 32, BtnStop_Click);
            _btnClear = AccentButton("Очистить", ColorMuted, 404, 14, 100, 32, BtnClear_Click);
            _lblStatus = new Label
            {
                Text = "Статус: готов",
                Location = new Point(520, 18),
                Size = new Size(220, 22),
                ForeColor = ColorMuted,
                Font = FontUi,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            bar.Controls.AddRange(new Control[] { _btnScan, _btnCompute, _btnStop, _btnClear, _lblStatus });
            panel.Controls.Add(bar);

            _console = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = ColorConsoleBg,
                ForeColor = ColorAccentGreen,
                Font = FontConsole,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Both,
                WordWrap = false
            };
            panel.Controls.Add(_console);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 72, BackColor = ColorPanel };
            _progressBar = new ProgressBar
            {
                Location = new Point(12, 8),
                Size = new Size(300, 10),
                Style = ProgressBarStyle.Continuous
            };
            var lblPb = new Label
            {
                Text = "Визуальный прогресс симуляции",
                Location = new Point(320, 6),
                Size = new Size(220, 16),
                ForeColor = ColorMuted,
                Font = FontUi
            };
            _lblLines = new Label
            {
                Text = "Всего строк: 0",
                Location = new Point(12, 28),
                Size = new Size(160, 18),
                ForeColor = ColorMuted,
                Font = FontUi
            };
            var disclaimer = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Text = "Внимание: только визуальная симуляция («киберпанк»-эффект). Приложение не взламывает системы " +
                       "и не меняет регион телевизора автоматически. Регион меняется вручную.",
                ForeColor = ColorMuted,
                Font = FontUi
            };
            bottom.Controls.AddRange(new Control[] { _progressBar, lblPb, _lblLines, disclaimer });
            panel.Controls.Add(bottom);

            return panel;
        }

        private Button AccentButton(string text, Color color, int x, int y, int width, int height, EventHandler click)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = color,
                ForeColor = Color.FromArgb(10, 20, 30),
                FlatStyle = FlatStyle.Flat,
                Font = FontUiBold,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = ScaleColor(color, 0.75);
            b.FlatAppearance.MouseDownBackColor = ScaleColor(color, 0.55);
            b.Click += click;
            return b;
        }

        private static Color ScaleColor(Color c, double factor)
        {
            return Color.FromArgb(
                (int)(c.R * factor),
                (int)(c.G * factor),
                (int)(c.B * factor));
        }

        // ---------- терминал: действия ----------
        private void BtnScan_Click(object sender, EventArgs e)
        {
            _engine.StartScan();
            _animationTimer.Start();
            _progressTimer.Start();
            AppendConsole("[Терминал] Запуск сканирования сетевых интерфейсов и эфира Wi-Fi...", ColorAccentGreen);
            _lblStatus.Text = "Статус: сканирование";
            _lblStatus.ForeColor = ColorAccentGreen;
        }

        private void BtnCompute_Click(object sender, EventArgs e)
        {
            _engine.StartCompute();
            _animationTimer.Start();
            _progressTimer.Start();
            AppendConsole("[Терминал] Запуск вычислительного контура: хэши, ключи, контрольные суммы...", ColorAccentCyan);
            _lblStatus.Text = "Статус: вычисления";
            _lblStatus.ForeColor = ColorAccentCyan;
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            _engine.Stop();
            AppendConsole("[Терминал] Все потоки остановлены пользователем.", ColorMuted);
            _lblStatus.Text = "Статус: остановлено";
            _lblStatus.ForeColor = ColorMuted;
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            _console.Clear();
            _totalLines = 0;
            _lblLines.Text = "Всего строк: 0";
            AppendConsole("[Терминал] Журнал очищен.", ColorMuted);
        }

        private void AnimationTick(object sender, EventArgs e)
        {
            _engine.Tick();
        }

        private void ProgressTick(object sender, EventArgs e)
        {
            _progressValue += _rand.Next(1, 6);
            if (_progressValue >= 100)
            {
                _progressValue = 0;
                AppendConsole("[СИСТЕМА] Этап завершён. Готов к следующему циклу.", ColorAccentCyan);
            }
            _progressBar.Value = _progressValue;
        }

        public void AppendConsole(string text, Color color)
        {
            if (_console.InvokeRequired)
            {
                _console.Invoke(new Action<string, Color>(AppendConsole), text, color);
                return;
            }
            _console.SelectionStart = _console.TextLength;
            _console.SelectionLength = 0;
            _console.SelectionColor = color;
            _console.AppendText(text + Environment.NewLine);
            _console.SelectionColor = _console.ForeColor;
            _totalLines++;
            _lblLines.Text = "Всего строк: " + _totalLines;
            if (_totalLines > 700) TrimConsole(200);
            _console.ScrollToCaret();
        }

        private void TrimConsole(int removeLines)
        {
            int pos = 0;
            int removed = 0;
            while (pos < _console.TextLength && removed < removeLines)
            {
                int nl = _console.Text.IndexOf('\n', pos);
                if (nl < 0) break;
                pos = nl + 1;
                removed++;
            }
            if (pos > 0)
            {
                _console.Select(0, pos);
                _console.SelectedText = "";
                _totalLines -= removed;
                _lblLines.Text = "Всего строк: " + _totalLines;
            }
        }

        // ---------- сборка вкладки «Wi-Fi + VPN» ----------
        private Panel BuildWifiPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = ColorBackground };

            var gbAp = GroupBoxDark("Точка доступа Windows (раздача интернета с ПК)", 12, 10, 500, 170);
            var lblSsid = LabelDark("Имя сети (SSID):", 14, 20, 220, 16);
            _txtSsid = TextBoxDark("RegionConsole_AP", 14, 38, 300, 24);
            var lblPwd = LabelDark("Пароль (8\u201363 символа):", 14, 70, 240, 16);
            _txtPassword = TextBoxDark("", 14, 88, 300, 24) { PasswordChar = '*' };
            _btnStartAp = AccentButton("Запустить точку доступа", ColorAccentGreen, 14, 120, 170, 32, BtnStartAp_Click);
            _btnStopAp = AccentButton("Остановить точку доступа", ColorDanger, 192, 120, 170, 32, BtnStopAp_Click);
            _lblApStatus = new Label
            {
                Text = "Статус: не запущено",
                Location = new Point(370, 126),
                Size = new Size(120, 22),
                ForeColor = ColorMuted,
                Font = FontUi
            };
            gbAp.Controls.AddRange(new Control[] { lblSsid, _txtSsid, lblPwd, _txtPassword,
                _btnStartAp, _btnStopAp, _lblApStatus });
            panel.Controls.Add(gbAp);

            var gbAdapters = GroupBoxDark("Сетевые адаптеры", 528, 10, 500, 330);
            var lblAd = LabelDark("Список адаптеров (команда netsh interface show interface):", 14, 20, 470, 16);
            _lstAdapters = new ListBox
            {
                Location = new Point(14, 38),
                Size = new Size(472, 220),
                BackColor = ColorConsoleBg,
                ForeColor = ColorText,
                BorderStyle = BorderStyle.FixedSingle,
                Font = FontConsole
            };
            _btnRefreshAdapters = AccentButton("Обновить список", ColorMuted, 14, 266, 140, 28, BtnRefreshAdapters_Click);
            _lblAdapterCount = new Label
            {
                Text = "Адаптеров: 0",
                Location = new Point(160, 272),
                Size = new Size(180, 18),
                ForeColor = ColorMuted,
                Font = FontUi
            };
            gbAdapters.Controls.AddRange(new Control[] { lblAd, _lstAdapters,
                _btnRefreshAdapters, _lblAdapterCount });
            panel.Controls.Add(gbAdapters);

            var gbIcs = GroupBoxDark("Общий доступ к интернету (ICS)", 12, 192, 500, 255);
            var lblVpn = LabelDark("VPN-адаптер (источник интернета):", 14, 20, 300, 16);
            _cbVpnSource = ComboBoxDark(14, 38, 300, 24);
            var lblVirt = LabelDark("Виртуальный адаптер точки доступа (получатель):", 14, 70, 360, 16);
            _cbVirtualReceiver = ComboBoxDark(14, 88, 300, 24);
            _chkReplaceIcs = new CheckBox
            {
                Text = "Подтверждаю, что хочу заменить существующий общий доступ другого адаптера",
                Location = new Point(14, 124),
                Size = new Size(470, 22),
                BackColor = ColorSurface,
                ForeColor = ColorMuted,
                Font = FontUi,
                Checked = false
            };
            _btnIcsOn = AccentButton("Настроить общий доступ", ColorAccentGreen, 14, 156, 170, 32, BtnIcsOn_Click);
            _btnIcsOff = AccentButton("Отключить общий доступ", ColorDanger, 192, 156, 170, 32, BtnIcsOff_Click);
            _lblIcsStatus = new Label
            {
                Text = "Статус: не настроено",
                Location = new Point(370, 162),
                Size = new Size(120, 22),
                ForeColor = ColorMuted,
                Font = FontUi
            };
            gbIcs.Controls.AddRange(new Control[] { lblVpn, _cbVpnSource, lblVirt, _cbVirtualReceiver,
                _chkReplaceIcs, _btnIcsOn, _btnIcsOff, _lblIcsStatus });
            panel.Controls.Add(gbIcs);

            var gbLog = GroupBoxDark("Лог команд Windows", 528, 352, 500, 325);
            var lblLog = LabelDark("Результат выполнения netsh и действий ICS (пароль не пишется в лог):", 14, 20, 470, 16);
            _logNet = new RichTextBox
            {
                Location = new Point(14, 38),
                Size = new Size(472, 258),
                BackColor = ColorConsoleBg,
                ForeColor = ColorText,
                BorderStyle = BorderStyle.FixedSingle,
                Font = FontConsole,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Both
            };
            var btnClearLog = AccentButton("Очистить лог", ColorMuted, 14, 304, 110, 24, BtnClearLog_Click);
            gbLog.Controls.AddRange(new Control[] { lblLog, _logNet, btnClearLog });
            panel.Controls.Add(gbLog);

            var warn = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 88,
                BackColor = ColorSurface,
                ForeColor = ColorDanger,
                Font = FontUi,
                Text = "ВНИМАНИЕ: работа точки доступа зависит от Wi-Fi-адаптера, его драйвера и поддержки режима " +
                       "размещённой сети (hosted network). Прохождение трафика через VPN и отсутствие утечек " +
                       "не гарантируются. Проверьте внешний IP телевизора (напр. на 2ip.ru) после настройки. " +
                       "Пароль не сохраняется в журнале. Отключить общий доступ: кнопка «Отключить общий доступ» " +
                       "или свойства адаптера в Панели управления \u2192 Сетевые подключения."
            };
            panel.Controls.Add(warn);

            return panel;
        }

        private static GroupBox GroupBoxDark(string text, int x, int y, int width, int height)
        {
            return new GroupBox
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = ColorSurface,
                ForeColor = ColorMuted,
                Font = FontUiBold
            };
        }

        private static Label LabelDark(string text, int x, int y, int width, int height)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                ForeColor = ColorMuted,
                Font = FontUi
            };
        }

        private static TextBox TextBoxDark(string text, int x, int y, int width, int height)
        {
            return new TextBox
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = ColorConsoleBg,
                ForeColor = ColorText,
                BorderStyle = BorderStyle.FixedSingle,
                Font = FontConsole
            };
        }

        private static ComboBox ComboBoxDark(int x, int y, int width, int height)
        {
            return new ComboBox
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = ColorConsoleBg,
                ForeColor = ColorText,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = FontConsole
            };
        }

        // ---------- Wi-Fi: логи ----------
        private void LogNet(string text)
        {
            LogNet(text, ColorText);
        }

        private void LogNet(string text, Color color)
        {
            if (_logNet.InvokeRequired)
            {
                _logNet.Invoke(new Action<string, Color>(LogNet), text, color);
                return;
            }
            int start = _logNet.TextLength;
            _logNet.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + Environment.NewLine);
            _logNet.Select(start, _logNet.TextLength - start);
            _logNet.SelectionColor = color;
            _logNet.Select(_logNet.TextLength, 0);
            _logNet.SelectionColor = ColorText;
            _logNet.ScrollToCaret();
        }

        private void BtnClearLog_Click(object sender, EventArgs e)
        {
            _logNet.Clear();
        }

        // ---------- Wi-Fi: точка доступа ----------
        private void BtnStartAp_Click(object sender, EventArgs e)
        {
            string ssid = _txtSsid.Text.Trim();
            string password = _txtPassword.Text ?? "";
            string error;
            if (!Validators.ValidateSsid(ssid, out error)) { ShowWarning(error); return; }
            if (!Validators.ValidatePassword(password, out error)) { ShowWarning(error); return; }
            if (!EnsureAdmin("запуск точки доступа Wi-Fi")) return;

            _btnStartAp.Enabled = false;
            LogNet("Команда: netsh wlan set hostednetwork mode=allow ssid=" + ssid + " key=*** (пароль умышленно не пишется в лог)");

            Task.Run(() =>
            {
                try
                {
                    var r1 = Netsh.Run($"wlan set hostednetwork mode=allow ssid=\"{ssid}\" key=\"{password}\"");
                    LogNet("set hostednetwork — код выхода " + r1.ExitCode);
                    LogNet(r1.Output);
                    if (r1.ExitCode != 0) return;

                    var r2 = Netsh.Run("wlan start hostednetwork");
                    LogNet("start hostednetwork — код выхода " + r2.ExitCode);
                    LogNet(r2.Output);

                    Invoke(new Action(() =>
                    {
                        if (r2.ExitCode == 0)
                        {
                            _lblApStatus.Text = "Статус: запущено (SSID: " + ssid + ")";
                            _lblApStatus.ForeColor = ColorAccentGreen;
                            RefreshAdapters();
                        }
                        else
                        {
                            _lblApStatus.Text = "Статус: ошибка запуска точки доступа";
                            _lblApStatus.ForeColor = ColorDanger;
                        }
                    }));
                }
                catch (Exception ex)
                {
                    LogNet("Исключение: " + ex.Message, ColorDanger);
                    Invoke(new Action(() =>
                    {
                        _lblApStatus.Text = "Статус: исключение";
                        _lblApStatus.ForeColor = ColorDanger;
                    }));
                }
                finally
                {
                    Invoke(new Action(() => _btnStartAp.Enabled = true));
                }
            });
        }

        private void BtnStopAp_Click(object sender, EventArgs e)
        {
            if (!EnsureAdmin("остановка точки доступа Wi-Fi")) return;
            _btnStopAp.Enabled = false;
            Task.Run(() =>
            {
                try
                {
                    var r = Netsh.Run("wlan stop hostednetwork");
                    LogNet("stop hostednetwork — код выхода " + r.ExitCode);
                    LogNet(r.Output);
                    Invoke(new Action(() =>
                    {
                        _lblApStatus.Text = r.ExitCode == 0
                            ? "Статус: точка доступа остановлена"
                            : "Статус: ошибка остановки";
                        _lblApStatus.ForeColor = r.ExitCode == 0 ? ColorAccentGreen : ColorDanger;
                        RefreshAdapters();
                    }));
                }
                catch (Exception ex)
                {
                    LogNet("Исключение: " + ex.Message, ColorDanger);
                    Invoke(new Action(() =>
                    {
                        _lblApStatus.Text = "Статус: исключение";
                        _lblApStatus.ForeColor = ColorDanger;
                    }));
                }
                finally
                {
                    Invoke(new Action(() => _btnStopAp.Enabled = true));
                }
            });
        }

        // ---------- Wi-Fi: адаптеры ----------
        private void BtnRefreshAdapters_Click(object sender, EventArgs e)
        {
            _btnRefreshAdapters.Enabled = false;
            _lstAdapters.Items.Clear();
            _cbVpnSource.Items.Clear();
            _cbVirtualReceiver.Items.Clear();

            Task.Run(() =>
            {
                var names = new List<string>();
                try
                {
                    var r = Netsh.Run("interface show interface");
                    var lines = r.Output.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var m = Regex.Match(line, @"^\S+\s+\S+\s+\S+\s+(?<name>.+)$");
                        if (m.Success)
                        {
                            var name = m.Groups["name"].Value.Trim();
                            if (name.Length > 0) names.Add(name);
                        }
                        Invoke(new Action(() => _lstAdapters.Items.Add(line)));
                    }
                }
                catch (Exception ex)
                {
                    Invoke(new Action(() => LogNet("Ошибка обновления адаптеров: " + ex.Message, ColorDanger)));
                }

                Invoke(new Action(() =>
                {
                    _cbVpnSource.Items.AddRange(names.ToArray());
                    _cbVirtualReceiver.Items.AddRange(names.ToArray());
                    _lblAdapterCount.Text = "Адаптеров: " + names.Count;
                    if (_lstAdapters.Items.Count == 0)
                        _lstAdapters.Items.Add("(не удалось распознать адаптеры)");
                }));
                Invoke(new Action(() => _btnRefreshAdapters.Enabled = true));
            });
        }

        // ---------- Wi-Fi: ICS ----------
        private void BtnIcsOn_Click(object sender, EventArgs e)
        {
            string vpn = _cbVpnSource.Text.Trim();
            string virt = _cbVirtualReceiver.Text.Trim();
            string error;
            if (!Validators.ValidateDistinct(vpn, virt, out error)) { ShowWarning(error); return; }
            if (!IcsManager.IsAvailable())
            {
                ShowWarning("COM-компонент NetSharingManager недоступен в этой системе. " +
                            "Общий доступ к интернету через ICS нельзя настроить автоматически.");
                return;
            }
            if (!EnsureAdmin("настройка общего доступа к интернету (ICS)")) return;

            bool anyExisting = IcsManager.AnySharingEnabled(out var existingName, out var existingIsPublic);
            bool replaceNeeded = anyExisting && existingName != vpn && existingName != virt;
            if (replaceNeeded && !_chkReplaceIcs.Checked)
            {
                MessageBox.Show("Общий доступ уже включён на адаптере \"" + existingName + "\".\n" +
                                "Поставьте галочку подтверждения замены и повторите, если действительно хотите перенести общий доступ.",
                    "Подтверждение замены", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Включить общий доступ к интернету (ICS)?\n\n" +
                "Источник (публичный, раздаёт интернет): " + vpn + "\n" +
                "Получатель (приватный, раздаёт Wi-Fi): " + virt + "\n\n" +
                "Это изменит сетевые настройки выбранных адаптеров.",
                "Подтверждение ICS", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes) return;

            _btnIcsOn.Enabled = false;
            Task.Run(() =>
            {
                Action<string> log = msg => LogNet("[ICS] " + msg, ColorMuted);

                var r1 = IcsManager.EnableSharingOn(vpn, true);
                log("VPN-адаптер \"" + vpn + "\" как публичный (источник интернета): " +
                    (r1.Success ? "OK" : "ОШИБКА: " + r1.Message));

                var r2 = IcsManager.EnableSharingOn(virt, false);
                log("Виртуальный адаптер \"" + virt + "\" как приватный (получатель): " +
                    (r2.Success ? "OK" : "ОШИБКА: " + r2.Message));

                bool ok = r1.Success && r2.Success;
                Invoke(new Action(() =>
                {
                    _btnIcsOn.Enabled = true;
                    _lblIcsStatus.Text = ok ? "Общий доступ включён" : "Общий доступ: ошибки — см. лог";
                    _lblIcsStatus.ForeColor = ok ? ColorAccentGreen : ColorDanger;
                }));
            });
        }

        private void BtnIcsOff_Click(object sender, EventArgs e)
        {
            string vpn = _cbVpnSource.Text.Trim();
            string virt = _cbVirtualReceiver.Text.Trim();
            if (string.IsNullOrEmpty(vpn) && string.IsNullOrEmpty(virt))
            {
                ShowWarning("Выберите адаптеры для отключения общего доступа.");
                return;
            }
            if (!EnsureAdmin("отключение общего доступа к интернету (ICS)")) return;

            _btnIcsOff.Enabled = false;
            Task.Run(() =>
            {
                var results = new List<string>();
                if (!string.IsNullOrEmpty(vpn))
                {
                    var r = IcsManager.DisableSharingOn(vpn);
                    results.Add("\"" + vpn + "\": " + (r.Success ? "отключён" : "ошибка: " + r.Message));
                }
                if (!string.IsNullOrEmpty(virt))
                {
                    var r = IcsManager.DisableSharingOn(virt);
                    results.Add("\"" + virt + "\": " + (r.Success ? "отключён" : "ошибка: " + r.Message));
                }
                Invoke(new Action(() =>
                {
                    _btnIcsOff.Enabled = true;
                    _lblIcsStatus.Text = "Общий доступ отключён (по выбранным адаптерам)";
                    _lblIcsStatus.ForeColor = ColorMuted;
                    foreach (var msg in results) LogNet("[ICS] " + msg);
                }));
            });
        }

        // ---------- вспомогательное ----------
        private static void ShowWarning(string text)
        {
            MessageBox.Show(text, "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private bool EnsureAdmin(string action)
        {
            if (IsAdmin()) return true;
            var result = MessageBox.Show(
                "Действие \"" + action + "\" требует прав администратора.\n\n" +
                "Перезапустить Region Console от имени администратора?",
                "Требуются права администратора",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (result == DialogResult.Yes)
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = Application.ExecutablePath,
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось перезапустить с правами администратора: " + ex.Message,
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Application.Exit();
            }
            return false;
        }

        private static bool IsAdmin()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void RefreshAdapters()
        {
            BtnRefreshAdapters_Click(this, EventArgs.Empty);
        }
    }
}
