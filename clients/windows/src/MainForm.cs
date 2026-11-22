using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace XrayLite
{
    public sealed class MainForm : Form
    {
        private static readonly Color Brand = Color.FromArgb(22, 119, 255);
        private static readonly Color Ink = Color.FromArgb(15, 23, 42);
        private static readonly Color Muted = Color.FromArgb(100, 116, 139);
        private static readonly Color Line = Color.FromArgb(226, 232, 240);

        private readonly ProfileStore store = new ProfileStore();
        private readonly List<VlessProfile> profiles = new List<VlessProfile>();
        private readonly XrayManager manager;
        private readonly Timer statusTimer;

        private ListView listView;
        private Label statusTitle;
        private Label statusSubtitle;
        private Label footer;
        private Button connectButton;
        private int selectedIndex = -1;

        public MainForm()
        {
            manager = new XrayManager(AppDomain.CurrentDomain.BaseDirectory);
            Text = "Xray Lite · VLESS REALITY";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(940, 640);
            MinimumSize = new Size(820, 560);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F);
            Icon = SystemIcons.Application;

            BuildUi();
            profiles.AddRange(store.Load());
            RefreshList();

            statusTimer = new Timer();
            statusTimer.Interval = 1200;
            statusTimer.Tick += delegate { RefreshStatus(); };
            statusTimer.Start();
        }

        private void BuildUi()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 5;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 108F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            Controls.Add(root);

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Brand;
            root.Controls.Add(header, 0, 0);

            Label title = new Label();
            title.Text = "Xray Lite";
            title.ForeColor = Color.White;
            title.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            title.AutoSize = true;
            title.Location = new Point(28, 18);
            header.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "VLESS · REALITY · TLS 1.3 · TUN";
            subtitle.ForeColor = Color.FromArgb(219, 234, 254);
            subtitle.Font = new Font("Segoe UI", 10F);
            subtitle.AutoSize = true;
            subtitle.Location = new Point(31, 63);
            header.Controls.Add(subtitle);

            Panel toolbar = new Panel();
            toolbar.Dock = DockStyle.Fill;
            toolbar.BackColor = Color.White;
            toolbar.Padding = new Padding(20, 10, 20, 6);
            root.Controls.Add(toolbar, 0, 1);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.LeftToRight;
            buttons.WrapContents = false;
            toolbar.Controls.Add(buttons);

            Button importLink = MakeButton("＋ 导入链接", true);
            importLink.Click += delegate { ImportLink(); };
            buttons.Controls.Add(importLink);

            Button importFile = MakeButton("导入文件", false);
            importFile.Click += delegate { ImportFile(); };
            buttons.Controls.Add(importFile);

            Button delete = MakeButton("删除", false);
            delete.Click += delegate { DeleteSelected(); };
            buttons.Controls.Add(delete);

            Button exit = MakeButton("退出", false);
            exit.Click += delegate { Close(); };
            buttons.Controls.Add(exit);

            Panel status = new Panel();
            status.Dock = DockStyle.Fill;
            status.Padding = new Padding(24, 10, 24, 6);
            status.BackColor = Color.White;
            root.Controls.Add(status, 0, 2);

            Panel card = new Panel();
            card.Dock = DockStyle.Fill;
            card.BackColor = Color.FromArgb(248, 250, 252);
            card.Padding = new Padding(18, 12, 18, 12);
            status.Controls.Add(card);

            statusTitle = new Label();
            statusTitle.Text = "未连接";
            statusTitle.ForeColor = Ink;
            statusTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            statusTitle.AutoSize = true;
            statusTitle.Location = new Point(20, 18);
            card.Controls.Add(statusTitle);

            statusSubtitle = new Label();
            statusSubtitle.Text = "选择一个节点，然后点击连接";
            statusSubtitle.ForeColor = Muted;
            statusSubtitle.AutoSize = true;
            statusSubtitle.Location = new Point(22, 58);
            card.Controls.Add(statusSubtitle);

            connectButton = MakeButton("连接", true);
            connectButton.Size = new Size(130, 40);
            connectButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            connectButton.Location = new Point(card.Width - 160, 42);
            connectButton.Click += delegate { ToggleConnection(); };
            card.Controls.Add(connectButton);
            card.Resize += delegate
            {
                connectButton.Location = new Point(card.Width - 160, 42);
            };

            Panel listPad = new Panel();
            listPad.Dock = DockStyle.Fill;
            listPad.Padding = new Padding(24, 6, 24, 6);
            listPad.BackColor = Color.White;
            root.Controls.Add(listPad, 0, 3);

            listView = new ListView();
            listView.Dock = DockStyle.Fill;
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.HideSelection = false;
            listView.MultiSelect = false;
            listView.BorderStyle = BorderStyle.None;
            listView.Font = new Font("Segoe UI", 10F);
            listView.Columns.Add("节点", 250);
            listView.Columns.Add("服务器", 190);
            listView.Columns.Add("SNI", 210);
            listView.Columns.Add("状态", 160);
            listView.SelectedIndexChanged += delegate { OnSelectionChanged(); };
            listView.DoubleClick += delegate { ToggleConnection(); };
            listPad.Controls.Add(listView);

            footer = new Label();
            footer.Dock = DockStyle.Fill;
            footer.TextAlign = ContentAlignment.MiddleLeft;
            footer.Padding = new Padding(24, 0, 0, 0);
            footer.ForeColor = Muted;
            footer.Text = "TUN 全局模式 · 需要管理员权限 · TLS 1.3 REALITY";
            root.Controls.Add(footer, 0, 4);
        }
        private static Button MakeButton(string text, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            button.AutoSize = true;
            button.MinimumSize = new Size(86, 34);
            button.Padding = new Padding(10, 0, 10, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = primary ? Brand : Line;
            button.BackColor = primary ? Brand : Color.White;
            button.ForeColor = primary ? Color.White : Ink;
            button.Cursor = Cursors.Hand;
            button.Margin = new Padding(0, 0, 10, 0);
            return button;
        }

        private void ImportLink()
        {
            using (InputDialog dialog = new InputDialog("导入 VLESS 链接", "粘贴 vless:// 链接："))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                AddProfile(dialog.Value);
            }
        }

        private void ImportFile()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "所有文件|*.*";
                dialog.Title = "选择包含 VLESS 链接的文件";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    string text = File.ReadAllText(dialog.FileName, Encoding.UTF8);
                    foreach (string line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string value = line.Trim();
                        if (value.StartsWith("vless://", StringComparison.OrdinalIgnoreCase))
                        {
                            AddProfile(value);
                            return;
                        }
                    }
                    MessageBox.Show(this, "文件中没有找到 vless:// 链接。", "导入失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "读取文件失败：" + ex.Message, "导入失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AddProfile(string url)
        {
            try
            {
                VlessProfile profile = VlessProfile.Parse(url);
                foreach (VlessProfile existing in profiles)
                {
                    if (string.Equals(existing.Uuid, profile.Uuid, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(existing.Host, profile.Host, StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show(this, "这个节点已经存在。", "提示",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }
                profiles.Add(profile);
                store.Save(profiles);
                RefreshList();
                listView.Items[listView.Items.Count - 1].Selected = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "链接解析失败：" + ex.Message, "导入失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteSelected()
        {
            if (selectedIndex < 0 || selectedIndex >= profiles.Count)
                return;
            if (manager.IsRunning)
                manager.Stop();
            profiles.RemoveAt(selectedIndex);
            store.Save(profiles);
            RefreshList();
        }

        private void ToggleConnection()
        {
            if (manager.IsRunning)
            {
                manager.Stop();
                RefreshStatus();
                return;
            }

            if (selectedIndex < 0 || selectedIndex >= profiles.Count)
            {
                MessageBox.Show(this, "请先选择一个节点。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            VlessProfile profile = profiles[selectedIndex];
            Cursor previous = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                manager.Start(profile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "连接失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = previous;
                RefreshStatus();
            }
        }

        private void RefreshList()
        {
            listView.BeginUpdate();
            listView.Items.Clear();
            for (int i = 0; i < profiles.Count; i++)
            {
                VlessProfile profile = profiles[i];
                ListViewItem item = new ListViewItem(profile.Name);
                item.SubItems.Add(profile.ServerText);
                item.SubItems.Add(profile.Sni);
                item.SubItems.Add(manager.IsRunning && i == selectedIndex ? "已连接" : "未连接");
                item.Tag = i;
                if (i == selectedIndex)
                    item.Selected = true;
                listView.Items.Add(item);
            }
            listView.EndUpdate();
            if (profiles.Count == 0)
            {
                selectedIndex = -1;
            }
            else if (selectedIndex < 0 || selectedIndex >= profiles.Count)
            {
                selectedIndex = 0;
                listView.Items[0].Selected = true;
            }
            RefreshStatus();
        }

        private void OnSelectionChanged()
        {
            if (listView.SelectedIndices.Count == 0)
                return;
            int index = listView.SelectedIndices[0];
            if (index == selectedIndex)
                return;
            selectedIndex = index;
            listView.Items[index].SubItems[3].Text = manager.IsRunning ? "当前节点" : "未连接";
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            bool running = manager.IsRunning;
            if (running && selectedIndex >= 0 && selectedIndex < profiles.Count)
            {
                statusTitle.Text = "已连接";
                statusSubtitle.Text = profiles[selectedIndex].Name + " · " +
                    profiles[selectedIndex].ServerText + " · TLS 1.3 REALITY";
                connectButton.Text = "断开";
            }
            else
            {
                statusTitle.Text = "未连接";
                statusSubtitle.Text = selectedIndex >= 0 && selectedIndex < profiles.Count
                    ? "已选择 " + profiles[selectedIndex].Name + "，点击连接"
                    : "导入一个 VLESS REALITY 节点开始";
                connectButton.Text = "连接";
            }
            if (selectedIndex >= 0 && selectedIndex < listView.Items.Count)
                listView.Items[selectedIndex].SubItems[3].Text = running ? "已连接" : "未连接";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            statusTimer.Stop();
            manager.Stop();
            base.OnFormClosing(e);
        }
    }

    internal sealed class InputDialog : Form
    {
        private readonly TextBox textBox;

        public string Value
        {
            get { return textBox.Text.Trim(); }
        }

        public InputDialog(string title, string label)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(580, 170);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9F);

            Label prompt = new Label();
            prompt.Text = label;
            prompt.AutoSize = true;
            prompt.Location = new Point(22, 20);
            Controls.Add(prompt);

            textBox = new TextBox();
            textBox.Location = new Point(22, 48);
            textBox.Width = 536;
            textBox.Height = 28;
            Controls.Add(textBox);

            Button ok = new Button();
            ok.Text = "导入";
            ok.DialogResult = DialogResult.OK;
            ok.Location = new Point(386, 105);
            ok.Width = 82;
            Controls.Add(ok);

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Location = new Point(476, 105);
            cancel.Width = 82;
            Controls.Add(cancel);

            AcceptButton = ok;
            CancelButton = cancel;
        }
    }
}
