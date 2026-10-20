using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Dialvia {
    public sealed class SettingsWindow : Form {
        Settings draft;
        readonly Action<Settings> save;
        readonly Action<Settings> notifyTest;
        readonly Action<string> browse;
        readonly Func<string> status;
        readonly Func<List<RecentThread>> inventoryProvider;
        readonly bool preview;
        readonly Panel content = new Panel { Dock = DockStyle.Fill };
        readonly Label heading = DesktopTheme.Label("", 23, true, false);
        readonly Label subtitle = DesktopTheme.Label("", 10, false, true);
        readonly Label footer = DesktopTheme.Label("设置保存在本机", 9, false, true);
        readonly List<Control> pages = new List<Control>();
        readonly List<Button> navigation = new List<Button>();
        readonly List<RecentThread> inventory = new List<RecentThread>();
        ListView bindings;
        Label bindingNote;
        ComboBox projectFilter;
        Label startupState, connectionState;
        string bindingView = "project";
        bool dirty, initializing = true;
        public SettingsWindow(Settings value, Action<Settings> onSave, Func<string> getStatus, Action<Settings> onNotifyTest, Action<string> onBrowse, Func<List<RecentThread>> loadInventory, bool isPreview) {
            draft = SettingsStore.Json.Deserialize<Settings>(SettingsStore.Json.Serialize(value)); save = onSave; status = getStatus;
            notifyTest = onNotifyTest; browse = onBrowse; inventoryProvider = loadInventory; preview = isPreview;
            DesktopTheme.Apply(this); Text = "Dialvia · 控制中心"; ClientSize = new Size(1080, 770); MinimumSize = new Size(960, 680);
            var sidebar = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 208, Padding = new Padding(20, 26, 14, 20), FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = DesktopTheme.Surface };
            sidebar.Controls.Add(new PictureBox { Image = Program.BrandImage, Width = 48, Height = 48, SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(0, 0, 0, 12) });
            sidebar.Controls.Add(DesktopTheme.Label("Dialvia", 23, true, false)); sidebar.Controls.Add(DesktopTheme.Label("手边设备 · Codex", 9, false, true));
            sidebar.Controls.Add(new Panel { Height = 23, Width = 160 });
            string[] titles = {"概览", "会话与项目", "语音与通知", "启动与集成"};
            for (int index = 0; index < titles.Length; index++) {
                int selected = index;
                var button = new Button { Text = titles[index], Width = 174, Height = 46, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(13, 0, 0, 0), Cursor = Cursors.Hand, Margin = new Padding(0, 0, 0, 7), BackColor = DesktopTheme.Surface, ForeColor = DesktopTheme.Ink };
                button.FlatAppearance.BorderSize = 0; button.Click += delegate { SelectPage(selected); }; navigation.Add(button); sidebar.Controls.Add(button);
            }
            sidebar.Controls.Add(new Panel { Height = 26, Width = 160 });
            var sidebarNote = DesktopTheme.Label("P240 首发适配\nWindows x64\n0.1.0-alpha.2", 9, false, true); sidebarNote.MaximumSize = new Size(165, 0); sidebar.Controls.Add(sidebarNote);
            var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 24, 26, 18), BackColor = DesktopTheme.Paper };
            var header = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 92, FlowDirection = FlowDirection.TopDown, WrapContents = false }; header.Controls.Add(heading); header.Controls.Add(subtitle);
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 66, Padding = new Padding(0, 14, 0, 0) };
            var actions = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 230, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            var apply = Button("保存更改", true, delegate { SaveDraft(); }); var close = Button("收起", false, delegate { Close(); }); actions.Controls.Add(apply); actions.Controls.Add(close);
            footer.Location = new Point(0, 24); bottom.Controls.Add(footer); bottom.Controls.Add(actions);
            right.Controls.Add(content); right.Controls.Add(bottom); right.Controls.Add(header); Controls.Add(right); Controls.Add(sidebar);
            ReloadInventory();
            pages.Add(BuildOverview()); pages.Add(BuildBindings()); pages.Add(BuildVoice()); pages.Add(BuildStartup());
            foreach (Control page in pages) { page.Dock = DockStyle.Fill; page.Visible = false; content.Controls.Add(page); }
            DesktopTheme.ScaleWindow(this);
            initializing = false; SelectPage(0);
            FormClosing += delegate(object sender, FormClosingEventArgs e) {
                if (!dirty || preview) return;
                DialogResult decision = MessageBox.Show(this, "保存这次修改后收起？", "Dialvia", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (decision == DialogResult.Cancel) e.Cancel = true;
                else if (decision == DialogResult.Yes && !SaveDraft()) e.Cancel = true;
            };
        }
        BrandButton Button(string text, bool primary, Action action) {
            var button = new BrandButton(text, primary);
            button.Click += delegate { try { action(); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Dialvia"); } }; return button;
        }
        void Changed() { if (!initializing) { dirty = true; footer.Text = "有未保存的更改"; } }
        bool SaveDraft() {
            try { SettingsStore.Validate(draft); if (!preview) save(draft); dirty = false; footer.Text = "已保存 · 立即生效"; return true; }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "无法保存"); return false; }
        }
        FlowLayoutPanel Page() {
            var page = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = DesktopTheme.Paper };
            page.ClientSizeChanged += delegate { Fit(page); }; return page;
        }
        BrandCard Card(FlowLayoutPanel page, string title, string description) {
            var card = new BrandCard(); card.Controls.Add(DesktopTheme.Label(title, 12, true, false));
            if (description != "") card.Controls.Add(DesktopTheme.Label(description, 9, false, true)); page.Controls.Add(card); return card;
        }
        void Fit(FlowLayoutPanel page) {
            int width = Math.Max(300, page.ClientSize.Width - 21);
            foreach (Control card in page.Controls) {
                card.MaximumSize = Size.Empty; card.MinimumSize = new Size(width, 0); card.MaximumSize = new Size(width, 0); card.Width = width;
                foreach (Control child in card.Controls) {
                    int inner = Math.Max(100, width - card.Padding.Horizontal - 2);
                    if (child is Label) child.MaximumSize = new Size(inner, 0);
                    else if (child is ListView) child.Width = inner;
                    else if (child is ComboBox) child.Width = inner;
                    else if (child is FlowLayoutPanel) child.MaximumSize = new Size(inner, 0);
                }
            }
        }
        public void SelectPage(int index) {
            string[] titles = {"一切都在手边", "会话与项目", "语音与通知", "启动与集成"};
            string[] notes = {"查看设备状态与常用入口。关闭窗口后，控制器继续留在托盘。", "项目固定编号，设备浏览可直接切换 Codex 会话。", "保留你的声音设备选择，让输入与提醒各就其位。", "登录启动、快捷键与任务通知，都可在这里管理。"};
            for (int item = 0; item < pages.Count; item++) { pages[item].Visible = item == index; navigation[item].BackColor = item == index ? DesktopTheme.Soft : DesktopTheme.Surface; navigation[item].ForeColor = item == index ? DesktopTheme.Copper : DesktopTheme.Ink; }
            heading.Text = titles[index]; subtitle.Text = notes[index]; pages[index].BringToFront(); Fit((FlowLayoutPanel)pages[index]);
            if (connectionState != null) connectionState.Text = status(); RefreshStartupState();
        }
        Control BuildOverview() {
            var page = Page(); var device = Card(page, "设备连接", "首发设备：Plantronics Calisto P240 · 047F:AE04");
            connectionState = DesktopTheme.Label(status(), 17, true, false); device.Controls.Add(connectionState);
            device.Controls.Add(DesktopTheme.Label("中央滚动键尚未取得主机输入信号；侧面按键保留原有音量功能。", 9, false, true));
            device.Controls.Add(DesktopTheme.Actions(Button("浏览最近会话", true, delegate { if (!preview) browse(draft.recentProjectPath); }), Button("配置项目编号", false, delegate { SelectPage(1); })));
            var mode = Card(page, "控制方式", "建议先用检测模式确认编号，再启用正式控制。");
            var detection = new CheckBox { Text = "检测模式：只显示操作提示", Checked = draft.dryRun, AutoSize = true, Margin = new Padding(0, 6, 0, 14) };
            detection.CheckedChanged += delegate { draft.dryRun = detection.Checked; Changed(); }; mode.Controls.Add(detection);
            mode.Controls.Add(DesktopTheme.Label("0＋绿键：语音输入　*编号＋绿键：固定项目\n22＋绿键进入后，单按绿键连续切换；2 上／8 下／5 退出加绿键", 10, false, false));
            var guide = Card(page, "日常使用", "用熟悉的设备继续工作，先选目标，再开始说话。");
            guide.Controls.Add(DesktopTheme.Label("听写生成文字草稿，由你确认发送。\n设置窗口可随时收起，退出程序请使用托盘菜单。", 10, false, false));
            return page;
        }
        Control BuildBindings() {
            var page = Page(); var routing = Card(page, "设备浏览方式", "进入后单按绿键连续切换；2＋绿向上，8＋绿向下，5＋绿退出。");
            var browsing = new BrandComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 620, Margin = new Padding(0, 3, 0, 10) };
            browsing.Items.AddRange(new object[] {"Codex 原生侧栏顺序 · 直接切换，不另开窗口（推荐）", "Dialvia 本机最近列表 · 弹窗选择后确认"}); browsing.SelectedIndex = draft.browseMode == "codex" ? 0 : 1;
            browsing.SelectedIndexChanged += delegate { draft.browseMode = browsing.SelectedIndex == 0 ? "codex" : "list"; Changed(); }; routing.Controls.Add(browsing);
            routing.Controls.Add(DesktopTheme.Label("直接拨 1–9 时：最近活跃或固定编号；不改变 Codex 原生侧栏顺序。", 9, false, true));
            var mode = new BrandComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 620, Margin = new Padding(0, 3, 0, 10) };
            mode.Items.AddRange(new object[] {"最近活跃 · 当前项目内最近的会话（推荐）", "固定编号 · 继续使用已绑定的会话"}); mode.SelectedIndex = draft.sessionRouting == "recent" ? 0 : 1;
            mode.SelectedIndexChanged += delegate { draft.sessionRouting = mode.SelectedIndex == 0 ? "recent" : "fixed"; Changed(); }; routing.Controls.Add(mode);
            projectFilter = new BrandComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 620 }; FillProjects(projectFilter, draft.recentProjectPath);
            projectFilter.SelectedIndexChanged += delegate { draft.recentProjectPath = SelectedProject(); RefreshBindings(); Changed(); }; routing.Controls.Add(projectFilter);
            routing.Controls.Add(DesktopTheme.Label("本机索引项目筛选只作用于 Dialvia 最近列表；原生切换沿用 Codex 的侧栏顺序。", 9, false, true));
            var map = Card(page, "编号与最近会话", "从本机 Codex 列表选择，不需要编辑 JSON。");
            map.Controls.Add(DesktopTheme.Actions(Button("项目编号", false, delegate { bindingView = "project"; RefreshBindings(); }), Button("最近会话", false, delegate { bindingView = "recent"; RefreshBindings(); }), Button("固定会话", false, delegate { bindingView = "session"; RefreshBindings(); })));
            map.Controls.Add(DesktopTheme.Actions(Button("添加绑定", true, delegate { EditBinding(null); }), Button("编辑", false, EditSelected), Button("移除", false, RemoveSelected), Button("刷新本机会话", false, delegate { ReloadInventory(); FillProjects(projectFilter, draft.recentProjectPath); RefreshBindings(); })));
            bindings = new ListView { Height = 150, Width = 620, View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, BorderStyle = BorderStyle.FixedSingle, BackColor = DesktopTheme.Surface, ForeColor = DesktopTheme.Ink };
            bindings.Columns.Add("编号", 70); bindings.Columns.Add("名称 / 会话", 315); bindings.Columns.Add("目标 / 最近活跃", 210); map.Controls.Add(bindings);
            bindings.DoubleClick += delegate { EditSelected(); };
            bindingNote = DesktopTheme.Label("", 9, false, true); map.Controls.Add(bindingNote); RefreshBindings(); return page;
        }
        void ReloadInventory() { inventory.Clear(); try { inventory.AddRange(inventoryProvider()); } catch (Exception ex) { footer.Text = ex.Message; } }
        sealed class ProjectItem {
            public string Path;
            public override string ToString() { if (Path == "") return "全部本机项目"; string name = System.IO.Path.GetFileName(Path.TrimEnd('\\', '/')); return name + "  ·  " + Path; }
        }
        void FillProjects(ComboBox combo, string selected) {
            bool previous = initializing; initializing = true;
            try {
                combo.Items.Clear(); combo.Items.Add(new ProjectItem { Path = "" });
                foreach (string path in inventory.Select(x => x.ProjectPath).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)) combo.Items.Add(new ProjectItem { Path = path });
                combo.SelectedIndex = 0;
                for (int index = 0; index < combo.Items.Count; index++) if (String.Equals(((ProjectItem)combo.Items[index]).Path, selected, StringComparison.OrdinalIgnoreCase)) combo.SelectedIndex = index;
            } finally { initializing = previous; }
        }
        string SelectedProject() { return projectFilter == null || projectFilter.SelectedItem == null ? draft.recentProjectPath : ((ProjectItem)projectFilter.SelectedItem).Path; }
        void RefreshBindings() {
            if (bindings == null) return; bindings.BeginUpdate(); bindings.Items.Clear();
            if (bindingView == "recent") {
                int number = 1;
                foreach (RecentThread thread in new SessionSnapshot(inventory, SelectedProject()).Threads) {
                    var row = new ListViewItem(number <= 9 ? number.ToString() : "—"); row.SubItems.Add(thread.Title); row.SubItems.Add(thread.UpdatedLabel); row.Tag = thread; bindings.Items.Add(row); number++;
                }
                if (bindingNote != null) bindingNote.Text = "按最近活跃排列；1–9 对应当前列表前九条，更多会话可在浏览器中选择。";
            } else {
                var map = bindingView == "project" ? draft.projects : draft.sessions;
                foreach (var item in map.OrderBy(x => x.Key)) { var row = new ListViewItem((bindingView == "project" ? "*" : "") + item.Key); row.SubItems.Add(item.Value.label); row.SubItems.Add(String.IsNullOrWhiteSpace(item.Value.projectPath) ? item.Value.url.Substring(16, 8) + "…" : Path.GetFileName(item.Value.projectPath.TrimEnd('\\', '/'))); row.Tag = item.Key; bindings.Items.Add(row); }
                if (bindingNote != null) bindingNote.Text = bindingView == "project" ? "项目编号固定，打开该项目的一条预设会话。" : "固定会话在“固定编号”模式下使用；最近模式无需逐条维护编号。";
            }
            bindings.EndUpdate();
        }
        void EditSelected() {
            if (bindings.SelectedItems.Count == 0) return;
            if (bindingView == "recent") { RecentThread thread = (RecentThread)bindings.SelectedItems[0].Tag; EditBinding(null, thread); }
            else EditBinding(Convert.ToString(bindings.SelectedItems[0].Tag));
        }
        void EditBinding(string slot) { EditBinding(slot, null); }
        void EditBinding(string slot, RecentThread preferred) {
            bool project = bindingView == "project"; var map = project ? draft.projects : draft.sessions;
            Target existing = slot != null && map.ContainsKey(slot) ? map[slot] : null;
            using (var editor = new BindingEditor(project, map, slot, existing, inventory, preferred)) {
                if (editor.ShowDialog(this) != DialogResult.OK) return;
                if (slot != null) map.Remove(slot); map[editor.Slot] = editor.Result; Changed(); RefreshBindings();
            }
        }
        void RemoveSelected() {
            if (bindings.SelectedItems.Count == 0 || bindingView == "recent") return;
            string slot = Convert.ToString(bindings.SelectedItems[0].Tag); var map = bindingView == "project" ? draft.projects : draft.sessions;
            map.Remove(slot); Changed(); RefreshBindings();
        }
        Control BuildVoice() {
            var page = Page(); var voice = Card(page, "语音输入", "使用你已登录的 Codex / ChatGPT 听写服务，转写成文字草稿。");
            var enabled = new CheckBox { Text = "启用话机语音控制", Checked = draft.voiceEnabled, AutoSize = true, Margin = new Padding(0, 4, 0, 12) };
            enabled.CheckedChanged += delegate { draft.voiceEnabled = enabled.Checked; Changed(); }; voice.Controls.Add(enabled);
            var mode = new BrandComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 620 }; mode.Items.AddRange(new object[] {"听写 · 先生成文字，确认后发送", "实时语音对话 · 需要 Codex 当前任务支持"}); mode.SelectedIndex = draft.voiceMode == "dictation" ? 0 : 1;
            mode.SelectedIndexChanged += delegate { draft.voiceMode = mode.SelectedIndex == 0 ? "dictation" : "conversation"; Changed(); }; voice.Controls.Add(mode);
            voice.Controls.Add(DesktopTheme.Actions(Button("选择麦克风与声音输出", false, delegate { if (!preview) Process.Start(new ProcessStartInfo("ms-settings:sound") { UseShellExecute = true }); })));
            voice.Controls.Add(DesktopTheme.Label("音频设备跟随 Windows / Codex 的选择；不会强制替换系统默认设备。", 9, false, true));
            var notification = Card(page, "完成时提醒我", "任务通知只显示通用完成提示，保持你的内容留在 Codex。");
            var balloons = new CheckBox { Text = "显示操作与完成通知", Checked = draft.balloonNotifications, AutoSize = true, Margin = new Padding(0, 4, 0, 12) };
            var ring = new CheckBox { Text = "让 P240 短铃提示", Checked = draft.ringNotifications, AutoSize = true, Margin = new Padding(0, 0, 0, 12) };
            balloons.CheckedChanged += delegate { draft.balloonNotifications = balloons.Checked; Changed(); }; ring.CheckedChanged += delegate { draft.ringNotifications = ring.Checked; Changed(); }; notification.Controls.Add(balloons); notification.Controls.Add(ring);
            notification.Controls.Add(DesktopTheme.Actions(Button("测试当前提醒", false, delegate { if (!preview) notifyTest(draft); })));
            notification.Controls.Add(DesktopTheme.Label("检测模式及由 Dialvia 跟踪的语音期间不响铃。自动完成通知需先在“启动与集成”配置 Hook。", 9, false, true)); return page;
        }
        Control BuildStartup() {
            var page = Page(); var startup = Card(page, "随 Windows 登录启动", "安装到当前用户目录，关闭设置窗口后继续在托盘运行。");
            startupState = DesktopTheme.Label("", 12, true, false); startup.Controls.Add(startupState);
            startup.Controls.Add(DesktopTheme.Actions(Button("安装并开启自启", true, delegate { if (!preview) Installer.Install(); RefreshStartupState(); }), Button("关闭自启", false, delegate { if (!preview) Installer.SetAutoStart(false); RefreshStartupState(); })));
            startup.Controls.Add(DesktopTheme.Label("自启开关立即生效，不需要管理员权限。", 9, false, true));
            var integration = Card(page, "连接 Codex", "保留原有快捷键，冲突时显示说明；通知 Hook 需要首次审阅。 ");
            integration.Controls.Add(DesktopTheme.Actions(Button("配置语音快捷键", false, delegate { if (preview) return; UserIntegration.InstallBindings(AppPaths.CodexHome, AppPaths.Data, draft); MessageBox.Show(this, "快捷键已配置。若无响应，请等其他任务空闲后重开 Codex。", "Dialvia"); }), Button("配置任务完成通知", false, delegate { if (preview) return; UserIntegration.InstallHook(AppPaths.CodexHome, Program.Executable); MessageBox.Show(this, "请在 Codex Hooks 中审阅并信任 Dialvia task notification。", "Dialvia"); })));
            var advanced = Card(page, "本机资料与高级设置", "日常编号通过图形界面完成，JSON 仅用于高级调整。");
            advanced.Controls.Add(DesktopTheme.Actions(Button("打开资料目录", false, delegate { if (!preview) Process.Start(new ProcessStartInfo(AppPaths.Data) { UseShellExecute = true }); }), Button("高级：编辑 JSON", false, delegate { if (!preview) Process.Start("notepad.exe", AppPaths.Config); }), Button("项目主页", false, delegate { if (!preview) Process.Start(new ProcessStartInfo("https://github.com/Octo-o-o-o/Dialvia") { UseShellExecute = true }); })));
            return page;
        }
        void RefreshStartupState() { if (startupState != null) startupState.Text = preview ? "当前：未开启登录自启" : Installer.AutoStartEnabled ? "当前：已开启登录自启" : "当前：未开启登录自启"; }
    }
    public sealed class BindingEditor : Form {
        public string Slot { get; private set; }
        public Target Result { get; private set; }
        public BindingEditor(bool project, Dictionary<string, Target> occupied, string originalSlot, Target existing, IList<RecentThread> inventory, RecentThread preferred) {
            DesktopTheme.Apply(this); Text = project ? "绑定项目编号" : "固定常用会话"; ClientSize = new Size(670, 550); MinimumSize = Size; MaximizeBox = false;
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(26), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true }; Controls.Add(flow);
            flow.Controls.Add(DesktopTheme.Label(project ? "一个编号，回到一个项目" : "把常用会话留在固定位置", 19, true, false));
            flow.Controls.Add(DesktopTheme.Label("从本机列表选择目标，也可手动粘贴会话链接。", 10, false, true));
            var number = new BrandComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 588, Margin = new Padding(0, 4, 0, 12) };
            for (int item = 1; item <= 9; item++) if (!occupied.ContainsKey(item.ToString()) || item.ToString() == originalSlot) number.Items.Add(item.ToString());
            if (number.Items.Count == 0) throw new InvalidOperationException("九个编号已用完，请编辑已有绑定。"); number.SelectedItem = originalSlot; if (number.SelectedIndex < 0) number.SelectedIndex = 0;
            flow.Controls.Add(DesktopTheme.Label(project ? "项目编号（输入时加 *）" : "会话编号", 9, true, false)); flow.Controls.Add(number);
            var choices = new BrandComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 588, DropDownWidth = 850, Margin = new Padding(0, 0, 0, 12) };
            foreach (RecentThread thread in inventory) choices.Items.Add(thread);
            flow.Controls.Add(DesktopTheme.Label("最近的 Codex 会话", 9, true, false)); flow.Controls.Add(choices);
            var label = new TextBox { Width = 588, Text = existing == null ? "" : existing.label, Margin = new Padding(0, 0, 0, 12) };
            flow.Controls.Add(DesktopTheme.Label("显示名称", 9, true, false)); flow.Controls.Add(label);
            var url = new TextBox { Width = 588, Text = existing == null ? "" : existing.url, Margin = new Padding(0, 0, 0, 10) };
            flow.Controls.Add(DesktopTheme.Label("会话链接（没有本机索引时可粘贴）", 9, true, false)); flow.Controls.Add(url);
            string projectPath = existing == null ? "" : existing.projectPath;
            choices.SelectedIndexChanged += delegate {
                var selected = choices.SelectedItem as RecentThread; if (selected == null) return; url.Text = selected.Url; projectPath = selected.ProjectPath;
                if (String.IsNullOrWhiteSpace(label.Text)) label.Text = project ? Path.GetFileName((selected.ProjectPath ?? "").TrimEnd('\\', '/')) : selected.Title;
            };
            if (preferred != null) choices.SelectedItem = preferred;
            else if (existing != null) for (int index = 0; index < choices.Items.Count; index++) if (((RecentThread)choices.Items[index]).Url == existing.url) { choices.SelectedIndex = index; break; }
            var accept = new BrandButton("保存绑定", true); accept.Click += delegate {
                try {
                    var target = new Target { label = label.Text.Trim(), url = url.Text.Trim(), projectPath = projectPath };
                    var check = new Settings(); check.sessions.Add(Convert.ToString(number.SelectedItem), target); SettingsStore.Validate(check);
                    Slot = Convert.ToString(number.SelectedItem); Result = target; DialogResult = DialogResult.OK; Close();
                } catch (Exception ex) { MessageBox.Show(this, ex.Message, "请检查绑定"); }
            };
            flow.Controls.Add(DesktopTheme.Actions(accept)); AcceptButton = accept;
            DesktopTheme.ScaleWindow(this);
        }
    }
}
