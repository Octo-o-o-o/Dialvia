using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Dialvia {
    public sealed class SessionBrowserWindow : Form {
        readonly Func<List<RecentThread>> load;
        readonly Action<RecentThread> open;
        readonly ComboBox projects = new BrandComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 730 };
        readonly ListView list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, BorderStyle = BorderStyle.FixedSingle };
        List<RecentThread> inventory;
        public SessionSnapshot Snapshot { get; private set; }
        public string ProjectPath { get { return projects.SelectedItem == null ? "" : (string)projects.SelectedItem; } }
        public event Action SelectionContextChanged;
        public SessionBrowserWindow(string project, Func<List<RecentThread>> inventoryLoader, Action<RecentThread> onOpen) {
            load = inventoryLoader; open = onOpen; DesktopTheme.Apply(this); Text = "Dialvia · 最近会话";
            ClientSize = new Size(840, 660); MinimumSize = new Size(740, 560); Padding = new Padding(24); KeyPreview = true;
            var header = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 126, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            header.Controls.Add(DesktopTheme.Label("选择接下来的会话", 22, true, false));
            header.Controls.Add(DesktopTheme.Label("2／8＋绿键：上一条／下一条　空白绿键：确认　5＋绿键：退出", 9, false, true)); header.Controls.Add(projects);
            header.ClientSizeChanged += delegate { projects.Width = Math.Max(100, header.ClientSize.Width - 4); };
            list.Columns.Add("编号", 60); list.Columns.Add("会话", 460); list.Columns.Add("最近活跃", 180); list.BackColor = DesktopTheme.Surface;
            list.ClientSizeChanged += delegate {
                float scale = Math.Max(DeviceDpi, CurrentAutoScaleDimensions.Width) / 96f;
                list.Columns[0].Width = (int)(52 * scale); list.Columns[2].Width = (int)(150 * scale);
                list.Columns[1].Width = Math.Max(240, list.ClientSize.Width - list.Columns[0].Width - list.Columns[2].Width - 24);
            };
            list.DoubleClick += delegate { Confirm(); };
            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 72, Padding = new Padding(0, 15, 0, 0), WrapContents = false };
            Add(footer, "上一条", false, delegate { MoveSelection(-1); }); Add(footer, "下一条", false, delegate { MoveSelection(1); });
            Add(footer, "进入会话", true, Confirm); Add(footer, "刷新列表", false, delegate { Reload(ProjectPath); }); Add(footer, "收起", false, Close);
            Controls.Add(list); Controls.Add(footer); Controls.Add(header);
            Reload(project); projects.SelectedIndexChanged += delegate { BuildSnapshot(); };
            Shown += delegate { projects.Width = Math.Max(100, header.ClientSize.Width - 4); list.Focus(); };
            KeyDown += delegate(object sender, KeyEventArgs e) {
                if (e.KeyCode == Keys.Enter) { Confirm(); e.Handled = true; e.SuppressKeyPress = true; }
                else if (!projects.DroppedDown && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)) { MoveSelection(e.KeyCode == Keys.Up ? -1 : 1); e.Handled = true; e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Escape) { Close(); e.Handled = true; }
                else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) {
                    int next = projects.SelectedIndex + (e.KeyCode == Keys.Left ? -1 : 1);
                    if (next >= 0 && next < projects.Items.Count) projects.SelectedIndex = next; e.Handled = true; e.SuppressKeyPress = true;
                }
            };
            DesktopTheme.ScaleWindow(this);
        }
        void Add(FlowLayoutPanel panel, string title, bool primary, Action action) { var button = new BrandButton(title, primary); button.Click += delegate { action(); }; panel.Controls.Add(button); }
        public void Reload(string project) {
            inventory = load(); projects.Items.Clear(); projects.Items.Add("");
            foreach (string path in inventory.Select(x => x.ProjectPath).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)) projects.Items.Add(path);
            projects.Format -= FormatProject; projects.Format += FormatProject;
            projects.FormattingEnabled = true; projects.SelectedIndex = 0;
            for (int index = 0; index < projects.Items.Count; index++) if (String.Equals((string)projects.Items[index], project, StringComparison.OrdinalIgnoreCase)) projects.SelectedIndex = index;
            BuildSnapshot();
        }
        void FormatProject(object sender, ListControlConvertEventArgs e) { if (Convert.ToString(e.ListItem) == "") e.Value = "全部本机项目"; }
        void BuildSnapshot() {
            if (inventory == null) return; Snapshot = new SessionSnapshot(inventory, ProjectPath); list.BeginUpdate(); list.Items.Clear();
            int rank = 1;
            foreach (RecentThread thread in Snapshot.Threads) { var row = new ListViewItem(rank <= 9 ? rank.ToString() : "—"); row.SubItems.Add(thread.Title); row.SubItems.Add(thread.UpdatedLabel); row.Tag = thread; list.Items.Add(row); rank++; }
            list.EndUpdate(); if (list.Items.Count > 0) { list.Items[0].Selected = true; list.Items[0].Focused = true; }
            if (SelectionContextChanged != null) SelectionContextChanged();
        }
        public void MoveSelection(int delta) {
            if (list.Items.Count == 0) return;
            int current = list.SelectedIndices.Count == 0 ? 0 : list.SelectedIndices[0];
            int next = Math.Max(0, Math.Min(list.Items.Count - 1, current + delta)); list.SelectedItems.Clear(); list.Items[next].Selected = true; list.Items[next].Focused = true; list.EnsureVisible(next);
        }
        public void Confirm() { if (list.SelectedItems.Count == 0) return; RecentThread target = (RecentThread)list.SelectedItems[0].Tag; Close(); open(target); }
    }
}
