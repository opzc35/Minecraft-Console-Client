using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace MinecraftClientGUI
{
    static class Theme
    {
        public static Color BgDark = Color.FromArgb(18, 19, 22);
        public static Color BgPanel = Color.FromArgb(26, 27, 31);
        public static Color BgHeader = Color.FromArgb(31, 32, 37);
        public static Color BgCard = Color.FromArgb(37, 38, 44);
        public static Color BgInput = Color.FromArgb(22, 23, 27);
        public static Color TabActive = Color.FromArgb(43, 44, 51);
        public static Color TabInactive = Color.FromArgb(26, 27, 31);
        public static Color Accent = Color.FromArgb(88, 112, 150);
        public static Color AccentHover = Color.FromArgb(104, 132, 176);
        public static Color AccentRed = Color.FromArgb(205, 85, 85);
        public static Color AccentGreen = Color.FromArgb(96, 190, 120);
        public static Color Text = Color.FromArgb(226, 228, 234);
        public static Color TextDim = Color.FromArgb(165, 169, 180);
        public static Color TextMuted = Color.FromArgb(104, 108, 120);
        public static Color Border = Color.FromArgb(54, 56, 64);
    }

    class DarkComboBox : ComboBox
    {
        public DarkComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            FlatStyle = FlatStyle.Flat;
            BackColor = Theme.BgInput;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9f);
        }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.Graphics.FillRectangle(
                new SolidBrush((e.State & DrawItemState.Selected) != 0 ? Theme.TabActive : Theme.BgInput),
                e.Bounds);
            TextRenderer.DrawText(e.Graphics, Items[e.Index].ToString(), Font, e.Bounds,
                Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }

    class FlatBtn : Button
    {
        private Color _back, _hover;
        public FlatBtn(string text, Color back, Color? hover = null)
        {
            Text = text;
            _back = back;
            _hover = hover ?? ControlPaint.Light(back, 0.2f);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = _back;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            MouseEnter += (s, e) => BackColor = _hover;
            MouseLeave += (s, e) => BackColor = _back;
        }
    }

    public partial class Form1 : Form
    {
        private const string SettingsFile = "settings_v3.txt";
        private const string MacrosFile = "macros.txt";
        private static readonly string[] DefaultSettingsContent = new[] { "", "", "" };
        private static readonly string[] DefaultMacrosContent = new[]
        {
            "Creative|/gamemode creative|Gold",
            "Survival|/gamemode survival|Gray",
            "Hello|Hello everyone!|Green",
            "Login|/login password123|Purple",
            "Spawn|/spawn|Blue"
        };
        private string currentLang = "en";

        private Panel tabBar, contentArea, topPanel, bottomPanel, rightPanel;
        private Label lblLogin, lblPass, lblIP, lblActive;
        private DarkComboBox cmbLogin, cmbIP;
        private TextBox txtPassword;
        private CheckBox chkRememberPassword;
        private FlatBtn btnAddBot;
        private TextBox boxGlobalInput;
        private FlatBtn btnGlobalSend;
        private CheckBox chkSendToAll;
        private Label lblMacrosTitle;
        private FlowLayoutPanel macroPanel;
        private FlatBtn btnEditMacros, btnRefreshMacros, btnLangSwitch;

        private List<string> historyLogins = new List<string>();
        private List<string> historyIPs = new List<string>();
        private List<ConsoleTab> tabs = new List<ConsoleTab>();
        private ConsoleTab activeTab = null;

        public Form1(string[] args)
        {
            InitializeComponent();
            BuildUI();
            EnsureRuntimeFiles();
            LoadSettings();
            LoadMacros();
            UpdateLanguage();
            if (args.Length > 0) AddNewTab("Session", args);
            this.FormClosing += (s, e) => { foreach (var t in tabs.ToList()) t.CloseTab(); };
        }

        private static void EnsureRuntimeFiles()
        {
            EnsureFileExists(SettingsFile, DefaultSettingsContent);
            EnsureFileExists(MacrosFile, DefaultMacrosContent);
        }

        private static void EnsureFileExists(string path, string[] defaultContent)
        {
            if (!File.Exists(path))
            {
                File.WriteAllLines(path, defaultContent);
            }
        }

        private void BuildUI()
        {
            this.Text = "Client Console";
            this.Size = new Size(1080, 720);
            this.MinimumSize = new Size(860, 560);
            this.BackColor = Theme.BgDark;
            this.ForeColor = Theme.Text;
            this.Font = new Font("Segoe UI", 9f);
            this.StartPosition = FormStartPosition.CenterScreen;

            // TOP PANEL
            topPanel = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Theme.BgHeader, Padding = new Padding(12, 0, 12, 0) };
            topPanel.Paint += PaintBottomBorder;
            this.Controls.Add(topPanel);

            int y = 17;
            lblLogin = MkLabel("Account:", 10, 2); topPanel.Controls.Add(lblLogin);
            cmbLogin = new DarkComboBox { Location = new Point(10, y), Size = new Size(195, 26) }; topPanel.Controls.Add(cmbLogin);

            lblPass = MkLabel("Password:", 215, 2); topPanel.Controls.Add(lblPass);
            txtPassword = new TextBox { Location = new Point(215, y), Size = new Size(155, 26), BackColor = Theme.BgInput, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true, Font = new Font("Segoe UI", 9f) };
            topPanel.Controls.Add(txtPassword);
            chkRememberPassword = new CheckBox { Text = "Remember", Location = new Point(215, 43), Size = new Size(96, 18), ForeColor = Theme.TextDim, Font = new Font("Segoe UI", 8f), BackColor = Theme.BgHeader };
            topPanel.Controls.Add(chkRememberPassword);

            lblIP = MkLabel("Server:", 380, 2); topPanel.Controls.Add(lblIP);
            cmbIP = new DarkComboBox { Location = new Point(380, y), Size = new Size(215, 26) }; topPanel.Controls.Add(cmbIP);

            btnAddBot = new FlatBtn("Connect", Theme.Accent, Theme.AccentHover) { Location = new Point(608, y - 1), Size = new Size(118, 28) };
            btnAddBot.Click += BtnAddBot_Click;
            topPanel.Controls.Add(btnAddBot);

            lblActive = new Label { Location = new Point(744, y), AutoSize = true, ForeColor = Theme.AccentGreen, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            topPanel.Controls.Add(lblActive);

            var timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += (s, e) => lblActive.Text = (currentLang == "en" ? "Sessions: " : "Sesje: ") + tabs.Count;
            timer.Start();

            // RIGHT PANEL
            rightPanel = new Panel { Dock = DockStyle.Right, Width = 185, BackColor = Theme.BgPanel };
            rightPanel.Paint += PaintLeftBorder;
            this.Controls.Add(rightPanel);

            // Language toggle - large button at the top
            btnLangSwitch = new FlatBtn("PL", Theme.BgCard, Color.FromArgb(49, 51, 60))
            {
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Text
            };
            btnLangSwitch.Click += (s, e) => { currentLang = currentLang == "en" ? "pl" : "en"; UpdateLanguage(); };
            rightPanel.Controls.Add(btnLangSwitch);

            // Macro header
            var macroHeader = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.BgPanel };
            macroHeader.Paint += PaintBottomBorder;
            rightPanel.Controls.Add(macroHeader);

            lblMacrosTitle = new Label { Text = "Actions", Location = new Point(8, 8), Size = new Size(169, 18), ForeColor = Theme.Text, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            macroHeader.Controls.Add(lblMacrosTitle);

            btnEditMacros = new FlatBtn("Edit", Theme.BgCard, Color.FromArgb(49, 51, 60)) { Location = new Point(8, 28), Size = new Size(76, 20), Font = new Font("Segoe UI", 8f, FontStyle.Bold) };
            btnEditMacros.Click += BtnEditMacros_Click;
            macroHeader.Controls.Add(btnEditMacros);

            btnRefreshMacros = new FlatBtn("Reload", Theme.BgCard, Color.FromArgb(49, 51, 60)) { Location = new Point(90, 28), Size = new Size(76, 20), Font = new Font("Segoe UI", 8f, FontStyle.Bold) };
            btnRefreshMacros.Click += (s, e) => LoadMacros();
            macroHeader.Controls.Add(btnRefreshMacros);

            macroPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Theme.BgPanel, Padding = new Padding(8, 8, 0, 8) };
            rightPanel.Controls.Add(macroPanel);

            rightPanel.Controls.SetChildIndex(macroPanel, 0);
            rightPanel.Controls.SetChildIndex(macroHeader, 1);
            rightPanel.Controls.SetChildIndex(btnLangSwitch, 2);

            // BOTTOM PANEL
            bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Theme.BgHeader, Padding = new Padding(6, 6, 6, 0) };
            bottomPanel.Paint += PaintTopBorder;
            this.Controls.Add(bottomPanel);

            btnGlobalSend = new FlatBtn("Send", Theme.Accent, Theme.AccentHover) { Dock = DockStyle.Right, Width = 80 };
            btnGlobalSend.Click += BtnGlobalSend_Click;
            bottomPanel.Controls.Add(btnGlobalSend);

            chkSendToAll = new CheckBox { Text = "All sessions", Dock = DockStyle.Right, Width = 120, ForeColor = Theme.TextDim, Padding = new Padding(8, 0, 0, 0), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            bottomPanel.Controls.Add(chkSendToAll);

            boxGlobalInput = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.BgInput, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 11f) };
            boxGlobalInput.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { BtnGlobalSend_Click(s, e); e.SuppressKeyPress = true; } };
            bottomPanel.Controls.Add(boxGlobalInput);

            // TAB BAR
            tabBar = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Theme.BgPanel };
            tabBar.Paint += PaintBottomBorder;
            this.Controls.Add(tabBar);

            // CONTENT AREA
            contentArea = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgDark };
            this.Controls.Add(contentArea);

            this.Controls.SetChildIndex(contentArea, 0);
            this.Controls.SetChildIndex(tabBar, 1);
            this.Controls.SetChildIndex(bottomPanel, 2);
            this.Controls.SetChildIndex(rightPanel, 3);
            this.Controls.SetChildIndex(topPanel, 4);
        }

        private void AddNewTab(string title, string[] args)
        {
            var tab = new ConsoleTab(title, args, currentLang) { Dock = DockStyle.Fill };
            tabs.Add(tab);
            contentArea.Controls.Add(tab);
            RebuildTabBar();
            ActivateTab(tab);
        }

        private void ActivateTab(ConsoleTab tab)
        {
            activeTab = tab;
            foreach (Control c in contentArea.Controls) c.Visible = (c == tab);
            RebuildTabBar();
        }

        private void RebuildTabBar()
        {
            tabBar.Controls.Clear();
            int x = 4;
            foreach (var tab in tabs)
            {
                var t = tab;
                bool active = (t == activeTab);

                var btn = new Panel { Location = new Point(x, active ? 2 : 5), Size = new Size(148, active ? 32 : 27), BackColor = active ? Theme.TabActive : Theme.TabInactive, Cursor = Cursors.Hand };
                btn.Paint += (s, e) => {
                    if (t == activeTab)
                        e.Graphics.FillRectangle(new SolidBrush(Theme.Accent), 0, btn.Height - 2, btn.Width, 2);
                };

                var lbl = new Label
                {
                    Text = t.TabTitle,
                    Location = new Point(8, 0),
                    Size = new Size(108, 30),
                    ForeColor = active ? Theme.Text : Theme.TextDim,
                    Font = new Font("Segoe UI", 9f, active ? FontStyle.Bold : FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand
                };
                lbl.Click += (s, e) => ActivateTab(t);
                btn.Click += (s, e) => ActivateTab(t);
                btn.Controls.Add(lbl);

                var btnX = new Label
                {
                    Text = "x",
                    Location = new Point(120, 0),
                    Size = new Size(25, 30),
                    ForeColor = Theme.TextMuted,
                    Font = new Font("Segoe UI", 9f),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand
                };
                btnX.MouseEnter += (s, e) => btnX.ForeColor = Theme.AccentRed;
                btnX.MouseLeave += (s, e) => btnX.ForeColor = Theme.TextMuted;
                btnX.Click += (s, e) => {
                    t.CloseTab();
                    tabs.Remove(t);
                    contentArea.Controls.Remove(t);
                    if (activeTab == t) { activeTab = tabs.LastOrDefault(); if (activeTab != null) ActivateTab(activeTab); }
                    RebuildTabBar();
                };
                btn.Controls.Add(btnX);
                tabBar.Controls.Add(btn);
                x += 152;
            }
        }

        private void UpdateLanguage()
        {
            bool en = currentLang == "en";
            btnLangSwitch.Text = en ? "Switch to PL" : "Switch to EN";
            lblLogin.Text = en ? "Account:" : "Konto:";
            lblPass.Text = en ? "Password:" : "Haslo:";
            chkRememberPassword.Text = en ? "Remember" : "Pamietaj";
            lblIP.Text = en ? "Server:" : "Serwer:";
            btnAddBot.Text = en ? "Connect" : "Polacz";
            lblMacrosTitle.Text = en ? "Actions" : "Akcje";
            btnEditMacros.Text = en ? "Edit" : "Edytuj";
            btnRefreshMacros.Text = en ? "Reload" : "Odswiez";
            btnGlobalSend.Text = en ? "Send" : "Wyslij";
            chkSendToAll.Text = en ? "All sessions" : "Wszystkie";
            foreach (var tab in tabs) tab.UpdateLang(currentLang);
        }

        private void BtnEditMacros_Click(object sender, EventArgs e)
        {
            EnsureFileExists(MacrosFile, DefaultMacrosContent);
            Process.Start("notepad.exe", MacrosFile);
        }

        private void LoadMacros()
        {
            macroPanel.Controls.Clear();
            if (!File.Exists(MacrosFile)) return;
            try
            {
                foreach (var line in File.ReadAllLines(MacrosFile))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('|');
                    if (parts.Length >= 2)
                    {
                        Color c = parts.Length > 2 ? Color.FromName(parts[2]) : Theme.Accent;
                        if (c.IsEmpty) c = Theme.Accent;
                        AddMacroBtn(parts[1], parts[0], c);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading macros: " + ex.Message); }
        }

        private void AddMacroBtn(string cmd, string label, Color color)
        {
            Color bg = Theme.BgCard;
            Color hov = Color.FromArgb(49, 51, 60);
            var btn = new Button
            {
                Text = "  " + label,
                Width = macroPanel.Width - 22,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Theme.Text,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 4),
                TextAlign = ContentAlignment.MiddleLeft,
                Tag = color
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = hov;
            btn.MouseLeave += (s, e) => btn.BackColor = bg;
            btn.Paint += (s, e) => e.Graphics.FillRectangle(new SolidBrush(color), 0, 0, 4, btn.Height);
            btn.Click += (s, e) => {
                if (chkSendToAll.Checked) foreach (var tab in tabs) tab.Send(cmd);
                else activeTab?.Send(cmd);
            };
            macroPanel.Controls.Add(btn);
        }

        private void BtnAddBot_Click(object sender, EventArgs e)
        {
            string user = cmbLogin.Text.Trim(), pass = txtPassword.Text.Trim(), ip = cmbIP.Text.Trim();
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(ip))
            {
                MessageBox.Show(currentLang == "en" ? "Please enter account and server." : "Podaj konto i serwer.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SaveSettings(user, ip);
            string tabTitle = user.Contains("@") ? user.Split('@')[0] : user;
            AddNewTab(tabTitle, new[] { user, pass, ip });
        }

        private void BtnGlobalSend_Click(object sender, EventArgs e)
        {
            string cmd = boxGlobalInput.Text.Trim();
            if (string.IsNullOrEmpty(cmd)) return;
            if (chkSendToAll.Checked) foreach (var tab in tabs) tab.Send(cmd);
            else activeTab?.Send(cmd);
            boxGlobalInput.Clear();
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFile)) return;
                var lines = File.ReadAllLines(SettingsFile).ToList();
                while (lines.Count < 3)
                {
                    lines.Add(string.Empty);
                }
                if (lines.Count > 0)
                {
                    txtPassword.Text = lines[0];
                    chkRememberPassword.Checked = !string.IsNullOrEmpty(lines[0]);
                }
                if (lines.Count > 1) { historyLogins = lines[1].Split('|').ToList(); cmbLogin.Items.AddRange(historyLogins.ToArray()); if (cmbLogin.Items.Count > 0) cmbLogin.SelectedIndex = 0; }
                if (lines.Count > 2) { historyIPs = lines[2].Split('|').ToList(); cmbIP.Items.AddRange(historyIPs.ToArray()); if (cmbIP.Items.Count > 0) cmbIP.SelectedIndex = 0; }
            }
            catch { }
        }

        private void SaveSettings(string user, string ip)
        {
            historyLogins.Remove(user); historyLogins.Insert(0, user); if (historyLogins.Count > 10) historyLogins.RemoveAt(10);
            historyIPs.Remove(ip); historyIPs.Insert(0, ip); if (historyIPs.Count > 10) historyIPs.RemoveAt(10);
            cmbLogin.Items.Clear(); cmbLogin.Items.AddRange(historyLogins.ToArray()); cmbLogin.Text = user;
            cmbIP.Items.Clear(); cmbIP.Items.AddRange(historyIPs.ToArray()); cmbIP.Text = ip;
            string storedPassword = chkRememberPassword.Checked ? txtPassword.Text : string.Empty;
            try { File.WriteAllLines(SettingsFile, new[] { storedPassword, string.Join("|", historyLogins), string.Join("|", historyIPs) }); } catch { }
        }

        private void PaintBottomBorder(object sender, PaintEventArgs e) { var c = (Control)sender; e.Graphics.DrawLine(new Pen(Theme.Border), 0, c.Height - 1, c.Width, c.Height - 1); }
        private void PaintTopBorder(object sender, PaintEventArgs e) { var c = (Control)sender; e.Graphics.DrawLine(new Pen(Theme.Border), 0, 0, c.Width, 0); }
        private void PaintLeftBorder(object sender, PaintEventArgs e) { var c = (Control)sender; e.Graphics.DrawLine(new Pen(Theme.Border), 0, 0, 0, c.Height); }
        private Label MkLabel(string text, int x, int y) => new Label { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = Theme.TextDim, Font = new Font("Segoe UI", 8f) };
    }

    // =====================================================
    // LINE TYPES (for filtering)
    // =====================================================
    enum LineType { Chat, System, Error }

    struct LogLine
    {
        public string Raw;
        public string Display;
        public LineType Type;
        public DateTime Time;
    }

    public class ConsoleTab : Panel
    {
        private MinecraftClient Client;
        private Thread t_read;
        private RichTextBox boxOutput;
        private Button btnDisconnect;
        private Label lblStatus;
        private Label lblTimer;
        private Panel inventoryPanel;
        private Label lblInventoryTitle;
        private ListView inventoryList;
        private Button btnRefreshInventory;
        private Button btnCloseInventory;
        private CheckBox chkAutoScroll;
        private bool autoScroll = true;
        private bool inventoryVisible = false;
        private int currentInventoryId = -1;
        private string currentInventoryTitle = "";
        private bool capturingInventory = false;

        private Button btnFilterAll, btnFilterChat, btnFilterSystem, btnFilterError;
        private LineType? activeFilter = null;

        private List<LogLine> allLines = new List<LogLine>();
        private object logLock = new object();

        private StreamWriter logWriter;
        private DateTime connectedAt;
        private System.Windows.Forms.Timer timerClock;
        private System.Windows.Forms.Timer timerInventoryRefresh;
        private bool isConnected = false;

        public string TabTitle { get; set; }

        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);
        private const int WM_VSCROLL = 0x115, SB_BOTTOM = 7;

        public ConsoleTab(string title, string[] args, string lang)
        {
            this.TabTitle = title;
            this.BackColor = Theme.BgDark;

            InitLogFile(title);

            // Top bar
            var topBar = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Theme.BgCard };
            topBar.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);

            btnDisconnect = new Button
            {
                Text = lang == "en" ? "Disconnect" : "Rozlacz",
                Dock = DockStyle.Right,
                Width = 105,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(128, 64, 64),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDisconnect.FlatAppearance.BorderSize = 0;
            btnDisconnect.Click += (s, e) => CloseTab();
            topBar.Controls.Add(btnDisconnect);

            chkAutoScroll = new CheckBox
            {
                Text = "Auto-scroll",
                Dock = DockStyle.Right,
                Width = 95,
                ForeColor = Theme.TextDim,
                Font = new Font("Segoe UI", 8f),
                Checked = true,
                Padding = new Padding(0, 0, 12, 0)
            };
            chkAutoScroll.CheckedChanged += (s, e) => autoScroll = chkAutoScroll.Checked;
            topBar.Controls.Add(chkAutoScroll);

            lblTimer = new Label
            {
                Text = "00:00:00",
                Dock = DockStyle.Right,
                Width = 70,
                ForeColor = Theme.TextDim,
                Font = new Font("Consolas", 8f),
                TextAlign = ContentAlignment.MiddleCenter
            };
            topBar.Controls.Add(lblTimer);

            lblStatus = new Label
            {
                Text = "  ● " + title,
                Dock = DockStyle.Fill,
                ForeColor = Theme.TextDim,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            topBar.Controls.Add(lblStatus);
            this.Controls.Add(topBar);

            // Filter bar
            var filterBar = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Theme.BgPanel };
            filterBar.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 0, filterBar.Height - 1, filterBar.Width, filterBar.Height - 1);

            btnFilterAll = MakeFilterBtn("All", null, filterBar, 4);
            btnFilterChat = MakeFilterBtn("Chat", LineType.Chat, filterBar, 54);
            btnFilterSystem = MakeFilterBtn("System", LineType.System, filterBar, 118);
            btnFilterError = MakeFilterBtn("Errors", LineType.Error, filterBar, 192);
            SetFilterActive(btnFilterAll);
            this.Controls.Add(filterBar);

            // Inventory panel
            inventoryPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 320,
                BackColor = Theme.BgPanel,
                Visible = false
            };
            inventoryPanel.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 0, 0, 0, inventoryPanel.Height);

            var inventoryHeader = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Theme.BgHeader };
            inventoryHeader.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Theme.Border), 0, inventoryHeader.Height - 1, inventoryHeader.Width, inventoryHeader.Height - 1);

            lblInventoryTitle = new Label
            {
                Text = "Inventory",
                Location = new Point(10, 8),
                Size = new Size(200, 18),
                ForeColor = Theme.Text,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            inventoryHeader.Controls.Add(lblInventoryTitle);

            btnCloseInventory = MakePanelButton("Close", 226, 6, 78);
            btnCloseInventory.Click += (s, e) => Client?.SendInternalCommand("inventory container close");
            inventoryHeader.Controls.Add(btnCloseInventory);

            btnRefreshInventory = MakePanelButton("Refresh", 10, 31, 88);
            btnRefreshInventory.Click += (s, e) => RequestInventorySnapshot();
            inventoryHeader.Controls.Add(btnRefreshInventory);

            inventoryPanel.Controls.Add(inventoryHeader);

            inventoryList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BackColor = Theme.BgCard,
                ForeColor = Theme.Text,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f)
            };
            inventoryList.Columns.Add("Slot", 52);
            inventoryList.Columns.Add("Item", 238);
            inventoryPanel.Controls.Add(inventoryList);
            this.Controls.Add(inventoryPanel);

            // Console
            boxOutput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgDark,
                ForeColor = Theme.Text,
                Font = new Font("Consolas", 10f),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            var ctx = new ContextMenuStrip { BackColor = Theme.BgCard, ForeColor = Theme.Text };
            ctx.Items.Add("Disconnect / Close", null, (s, e) => CloseTab());
            ctx.Items.Add("Clear Console", null, (s, e) => { boxOutput.Clear(); lock (logLock) allLines.Clear(); });
            boxOutput.ContextMenuStrip = ctx;
            this.Controls.Add(boxOutput);

            try { SetWindowTheme(boxOutput.Handle, "DarkMode_Explorer", null); } catch { }

            timerClock = new System.Windows.Forms.Timer { Interval = 1000 };
            timerClock.Tick += (s, e) => {
                if (isConnected && !lblTimer.IsDisposed)
                {
                    var elapsed = DateTime.Now - connectedAt;
                    lblTimer.Text = elapsed.ToString(@"hh\:mm\:ss");
                }
            };
            timerClock.Start();

            timerInventoryRefresh = new System.Windows.Forms.Timer { Interval = 1200 };
            timerInventoryRefresh.Tick += (s, e) => {
                if (inventoryVisible && isConnected)
                    RequestInventorySnapshot();
            };
            timerInventoryRefresh.Start();

            PrintSystem("Initializing...", LineType.System);
            if (args.Length == 3) new Thread(() => InitClient(new MinecraftClient(args[0], args[1], args[2]))).Start();
            else new Thread(() => InitClient(new MinecraftClient(args))).Start();
        }

        private void InitLogFile(string title)
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(dir);
                string safeName = string.Concat(title.Split(Path.GetInvalidFileNameChars()));
                string date = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
                string path = Path.Combine(dir, safeName + "_" + date + ".txt");
                logWriter = new StreamWriter(path, append: true, encoding: System.Text.Encoding.UTF8) { AutoFlush = true };
                logWriter.WriteLine("=== Session started: " + DateTime.Now + " | Account: " + title + " ===");
            }
            catch { }
        }

        private void WriteLog(string text, LineType type)
        {
            try { logWriter?.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "][" + type + "] " + text); } catch { }
        }

        private Button MakeFilterBtn(string label, LineType? type, Panel parent, int x)
        {
            int w = label == "All" ? 44 : label == "System" ? 68 : 58;
            var btn = new Button
            {
                Text = label,
                Location = new Point(x, 4),
                Size = new Size(w, 22),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.BgCard,
                ForeColor = Theme.TextDim,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Theme.Border;
            btn.Click += (s, e) => { activeFilter = type; SetFilterActive(btn); RedrawFiltered(); };
            parent.Controls.Add(btn);
            return btn;
        }

        private Button MakePanelButton(string label, int x, int y, int width)
        {
            var btn = new Button
            {
                Text = label,
                Location = new Point(x, y),
                Size = new Size(width, 22),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.BgCard,
                ForeColor = Theme.TextDim,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Theme.Border;
            return btn;
        }

        private void SetFilterActive(Button active)
        {
            foreach (var b in new[] { btnFilterAll, btnFilterChat, btnFilterSystem, btnFilterError })
            {
                if (b == null) continue;
                b.BackColor = b == active ? Theme.Accent : Theme.BgCard;
                b.ForeColor = b == active ? Color.White : Theme.TextDim;
            }
        }

        private void RedrawFiltered()
        {
            InvokeUI(() => {
                boxOutput.Clear();
                List<LogLine> snapshot;
                lock (logLock) snapshot = new List<LogLine>(allLines);
                foreach (var line in snapshot)
                    if (activeFilter == null || line.Type == activeFilter)
                        RenderLine(line);
                if (autoScroll) SendMessage(boxOutput.Handle, WM_VSCROLL, (IntPtr)SB_BOTTOM, IntPtr.Zero);
            });
        }

        private void RenderLine(LogLine line)
        {
            if (line.Type == LineType.System || line.Type == LineType.Error)
            {
                boxOutput.SelectionColor = line.Type == LineType.Error ? Theme.AccentRed : Theme.TextDim;
                boxOutput.AppendText(line.Display + "\n");
            }
            else
            {
                string[] subs = line.Raw.Split('\u00a7');
                boxOutput.SelectionColor = Theme.Text;
                if (subs.Length > 0) boxOutput.AppendText(subs[0]);
                for (int i = 1; i < subs.Length; i++)
                {
                    if (subs[i].Length > 1)
                    {
                        boxOutput.SelectionColor = GetColor(subs[i][0]);
                        boxOutput.SelectionFont = GetFont(subs[i][0], boxOutput.Font);
                        boxOutput.AppendText(subs[i].Substring(1));
                    }
                }
                boxOutput.AppendText("\n");
            }
        }

        public void UpdateLang(string lang) { if (btnDisconnect != null) btnDisconnect.Text = lang == "en" ? "Disconnect" : "Rozlacz"; }

        private void InitClient(MinecraftClient client)
        {
            Client = client;
            t_read = new Thread(ReadLoop) { IsBackground = true };
            t_read.Start();
            connectedAt = DateTime.Now;
            isConnected = true;
            InvokeUI(() => {
                lblStatus.Text = "  ● " + TabTitle;
                lblStatus.ForeColor = Color.FromArgb(100, 210, 130);
                PrintSystem("Connected.", LineType.System);
            });
        }

        private void ReadLoop()
        {
            try
            {
                while (Client != null && !Client.Disconnected)
                {
                    string line = Client.ReadLine();
                    if (!string.IsNullOrEmpty(line)) PrintChat(line);
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception ex) { InvokeUI(() => PrintSystem("Error: " + ex.Message, LineType.Error)); }
            finally
            {
                isConnected = false;
                InvokeUI(() => {
                    PrintSystem("Disconnected.", LineType.Error);
                    if (lblStatus != null) { lblStatus.Text = "  ● " + TabTitle; lblStatus.ForeColor = Color.FromArgb(220, 80, 80); }
                });
            }
        }

        public void Send(string text)
        {
            if (Client != null && !Client.Disconnected)
            {
                Client.SendText(text);
                InvokeUI(() => PrintSystem("> " + text, LineType.System, Color.FromArgb(100, 180, 255)));
            }
        }

        public void CloseTab()
        {
            try
            {
                isConnected = false;
                timerClock?.Stop();
                timerInventoryRefresh?.Stop();
                logWriter?.WriteLine("=== Session ended: " + DateTime.Now + " ===");
                logWriter?.Close();
                if (Client != null) { Client.Close(); Client = null; }
                if (t_read != null && t_read.IsAlive) { t_read.Abort(); t_read = null; }
            }
            catch { }
        }

        private void PrintSystem(string text, LineType type, Color? color = null)
        {
            string prefix = type == LineType.Error ? "[ERR] " : "[SYS] ";
            Color c = color ?? (type == LineType.Error ? Theme.AccentRed : Theme.TextDim);
            var entry = new LogLine { Raw = prefix + text, Display = prefix + text, Type = type, Time = DateTime.Now };
            lock (logLock) allLines.Add(entry);
            WriteLog(text, type);
            InvokeUI(() => {
                if (activeFilter == null || activeFilter == type)
                {
                    boxOutput.SelectionColor = c;
                    boxOutput.AppendText(entry.Display + "\n");
                    if (autoScroll) SendMessage(boxOutput.Handle, WM_VSCROLL, (IntPtr)SB_BOTTOM, IntPtr.Zero);
                }
            });
        }

        private void PrintChat(string raw)
        {
            string plain = System.Text.RegularExpressions.Regex.Replace(raw, @"§.", "");
            HandleInventoryUiLine(plain);
            var entry = new LogLine { Raw = raw, Display = plain, Type = LineType.Chat, Time = DateTime.Now };
            lock (logLock) allLines.Add(entry);
            WriteLog(plain, LineType.Chat);
            InvokeUI(() => {
                if (activeFilter == null || activeFilter == LineType.Chat)
                {
                    boxOutput.SuspendLayout();
                    RenderLine(entry);
                    if (autoScroll) SendMessage(boxOutput.Handle, WM_VSCROLL, (IntPtr)SB_BOTTOM, IntPtr.Zero);
                    boxOutput.ResumeLayout();
                }
            });
        }

        private void InvokeUI(Action a) { if (!boxOutput.IsDisposed) { if (boxOutput.InvokeRequired) try { boxOutput.Invoke(a); } catch { } else a(); } }
        private void RequestInventorySnapshot()
        {
            if (Client != null && !Client.Disconnected)
            {
                capturingInventory = true;
                Client.SendInternalCommand("inventory container list");
            }
        }

        private void HandleInventoryUiLine(string line)
        {
            string plain = StripLogPrefix(line).Trim();

            Match open = Regex.Match(plain, @"Inventory\s+#\s*(\d+)\s+opened:\s*(.+)$", RegexOptions.IgnoreCase);
            if (open.Success)
            {
                currentInventoryId = int.Parse(open.Groups[1].Value);
                currentInventoryTitle = open.Groups[2].Value.Trim();
                InvokeUI(() => {
                    lblInventoryTitle.Text = "#" + currentInventoryId + " " + currentInventoryTitle;
                    inventoryList.Items.Clear();
                    inventoryPanel.Visible = true;
                    inventoryVisible = true;
                });
                ThreadPool.QueueUserWorkItem(_ => {
                    Thread.Sleep(350);
                    RequestInventorySnapshot();
                });
                return;
            }

            Match close = Regex.Match(plain, @"Inventory\s+#\s*(\d+)\s+closed\.", RegexOptions.IgnoreCase);
            if (close.Success && int.TryParse(close.Groups[1].Value, out int closedId) && closedId == currentInventoryId)
            {
                InvokeUI(() => {
                    inventoryPanel.Visible = false;
                    inventoryVisible = false;
                    inventoryList.Items.Clear();
                });
                currentInventoryId = -1;
                currentInventoryTitle = "";
                capturingInventory = false;
                return;
            }

            Match header = Regex.Match(plain, @"Inventory\s+#\s*(\d+)\s+-\s*(.+)$", RegexOptions.IgnoreCase);
            if (header.Success)
            {
                capturingInventory = true;
                currentInventoryId = int.Parse(header.Groups[1].Value);
                currentInventoryTitle = header.Groups[2].Value.Trim();
                InvokeUI(() => {
                    lblInventoryTitle.Text = "#" + currentInventoryId + " " + currentInventoryTitle;
                    inventoryList.Items.Clear();
                    inventoryPanel.Visible = true;
                    inventoryVisible = true;
                });
                return;
            }

            if (!capturingInventory)
                return;

            Match item = Regex.Match(plain, @"^(?:>?\d+|\s*)\s*\|\s*#\s*(-?\d+)\s*:\s*(.+)$");
            if (item.Success)
            {
                string slot = item.Groups[1].Value;
                string itemText = item.Groups[2].Value.Trim();
                InvokeUI(() => UpsertInventoryItem(slot, itemText));
            }
        }

        private static string StripLogPrefix(string line)
        {
            int bracket = line.LastIndexOf(']');
            if (line.StartsWith("[") && bracket >= 0 && bracket < line.Length - 1)
                return line.Substring(bracket + 1);
            return line;
        }

        private void UpsertInventoryItem(string slot, string itemText)
        {
            foreach (ListViewItem existing in inventoryList.Items)
            {
                if (existing.Text == slot)
                {
                    existing.SubItems[1].Text = itemText;
                    return;
                }
            }

            var row = new ListViewItem(slot);
            row.SubItems.Add(itemText);
            inventoryList.Items.Add(row);
        }

        private Font GetFont(char c, Font f) => c == 'l' ? new Font(f, FontStyle.Bold) : f;
        private Color GetColor(char c)
        {
            switch (c)
            {
                case '0': return Color.FromArgb(20, 20, 20);
                case '1': return Color.FromArgb(85, 85, 255);
                case '2': return Color.FromArgb(85, 200, 85);
                case '3': return Color.FromArgb(85, 220, 220);
                case '4': return Color.FromArgb(220, 85, 85);
                case '5': return Color.FromArgb(200, 85, 200);
                case '6': return Color.FromArgb(255, 180, 30);
                case '7': return Color.Silver;
                case '8': return Color.FromArgb(120, 120, 140);
                case '9': return Color.FromArgb(100, 130, 255);
                case 'a': return Color.FromArgb(85, 255, 85);
                case 'b': return Color.FromArgb(85, 255, 255);
                case 'c': return Color.FromArgb(255, 85, 85);
                case 'd': return Color.FromArgb(255, 130, 255);
                case 'e': return Color.FromArgb(255, 255, 85);
                case 'f': return Color.White;
                default: return Color.FromArgb(200, 200, 215);
            }
        }
    }
}
