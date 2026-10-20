using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace Dialvia {
    public sealed class ControllerForm : Form {
        Settings settings;
        readonly ICodexDesktop codex;
        IDeviceAdapter adapter;
        NotifyIcon tray;
        SettingsWindow settingsWindow;
        SessionBrowserWindow browser;
        SessionSnapshot recentSnapshot;
        string activeProjectPath = "";
        Timer reconnect, talkDelay, inboxTimer, ringTimer, voiceStartDelay;
        bool conversationActive, closing, nativeBrowsing;
        readonly DictationState dictation = new DictationState();
        bool VoiceBusy { get { return conversationActive || dictation.Active || (voiceStartDelay != null && voiceStartDelay.Enabled); } }
        int nativeDirection = 1;
        readonly DeferredGreenPresses pendingGreen = new DeferredGreenPresses();
        bool Browsing { get { return nativeBrowsing || (browser != null && !browser.IsDisposed); } }
        string deviceStatus = "等待 P240", lastAction = "";
        DateTime lastActionTime = DateTime.MinValue;
        public ControllerForm(ICodexDesktop desktop) {
            codex = desktop; Opacity = 0; ShowInTaskbar = false; WindowState = FormWindowState.Minimized; Icon = Program.AppIcon;
        }
        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            try { settings = SettingsStore.Load(AppPaths.Config); }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\n" + AppPaths.Config, "Dialvia 配置错误"); Close(); return; }
            tray = new NotifyIcon { Icon = Program.AppIcon, Text = "Dialvia · Codex", Visible = true };
            activeProjectPath = settings.recentProjectPath;
            var menu = new ContextMenuStrip { BackColor = DesktopTheme.Surface, ForeColor = DesktopTheme.Ink, Font = DesktopTheme.Ui };
            var state = menu.Items.Add(deviceStatus); state.Enabled = false;
            menu.Items.Add("打开控制中心", null, delegate { ShowSettings(0); });
            menu.Items.Add("进入会话切换", null, delegate { BeginBrowsing(0); });
            menu.Items.Add("退出会话切换", null, delegate { FinishBrowsing(); });
            menu.Items.Add("本机最近会话列表", null, delegate { ShowBrowser(activeProjectPath, 0); });
            menu.Items.Add("会话与项目", null, delegate { ShowSettings(1); });
            menu.Items.Add("语音与通知", null, delegate { ShowSettings(2); });
            menu.Items.Add("Codex 已停止录音：校正状态", null, delegate { ResetVoiceTracking(); });
            menu.Items.Add(new ToolStripSeparator());
            var startup = new ToolStripMenuItem("登录 Windows 时启动");
            startup.Click += delegate { try { Installer.SetAutoStart(!Installer.AutoStartEnabled); startup.Checked = Installer.AutoStartEnabled; } catch (Exception ex) { Hint(ex.Message); } }; menu.Items.Add(startup);
            var detection = new ToolStripMenuItem("检测模式");
            detection.Click += delegate { if (VoiceBusy) { Hint("请先停止语音。"); return; } settings.dryRun = !settings.dryRun; SettingsStore.Save(AppPaths.Config, settings); detection.Checked = settings.dryRun; }; menu.Items.Add(detection);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("打开运行日志", null, delegate { Process.Start("notepad.exe", Path.Combine(AppPaths.Data, "dialvia.log")); });
            menu.Items.Add("退出 Dialvia", null, delegate { Close(); });
            menu.Opening += delegate { state.Text = deviceStatus; startup.Checked = Installer.AutoStartEnabled; detection.Checked = settings.dryRun; };
            tray.ContextMenuStrip = menu;
            tray.MouseClick += delegate(object sender, MouseEventArgs click) { if (click.Button == MouseButtons.Left) ShowSettings(0); };
            talkDelay = new Timer { Interval = 40 };
            talkDelay.Tick += delegate {
                if (pendingGreen.TakeReady(DateTime.UtcNow)) HandleBareButton();
                if (pendingGreen.Count == 0) talkDelay.Stop();
            };
            voiceStartDelay = new Timer { Interval = 180 };
            voiceStartDelay.Tick += delegate {
                voiceStartDelay.Stop();
                DateTime requestedUtc = DateTime.UtcNow;
                if (!SendShortcut("dictate")) return;
                dictation.StartRequested(requestedUtc); DeviceAction(delegate { adapter.SetVoiceState(true); });
                Hint("已请求当前聊天听写；请确认输入框出现录音，再说话。结束按绿键，看到文字后 00＋绿键发送。");
            };
            ringTimer = new Timer { Interval = 1200 }; ringTimer.Tick += delegate { ringTimer.Stop(); DeviceAction(delegate { adapter.Ring(false); }); };
            adapter = new PlantronicsP240Adapter(this, true);
            adapter.DialRequested += Dispatch;
            adapter.StatusChanged += delegate(string status) { deviceStatus = status; string tip = "Dialvia · " + status; tray.Text = tip.Length > 63 ? tip.Substring(0, 63) : tip; };
            adapter.ButtonPressed += delegate(DeviceButton button) {
                if (button == DeviceButton.ToggleVoice) { pendingGreen.Press(DateTime.UtcNow); talkDelay.Start(); }
                else if (button == DeviceButton.StartVoice) Hint("开始听写请按 0＋绿键。"); else EndVoice();
            };
            reconnect = new Timer { Interval = 5000 };
            reconnect.Tick += delegate {
                string stop = Path.Combine(AppPaths.Data, "stop.request");
                if (File.Exists(stop)) { File.Delete(stop); Close(); return; }
                if (!adapter.Connected) adapter.Connect();
            }; reconnect.Start();
            inboxTimer = new Timer { Interval = 1000 }; inboxTimer.Tick += delegate { ReconcileVoice(); ReadInbox(); }; inboxTimer.Start();
            adapter.Connect();
            if (settings.dryRun) Hint("检测模式：按键只显示提示。配置编号并关闭检测模式后启用控制。");
            string welcome = Path.Combine(AppPaths.Data, "welcome.seen");
            if (!File.Exists(welcome)) { AtomicFile.Write(welcome, "1"); BeginInvoke(new Action(delegate { ShowSettings(0); })); }
        }
        void Hint(string text) {
            AppPaths.Log(text);
            if (text.Length > 180) text = text.Substring(0, 180);
            if (tray != null && settings.balloonNotifications) { tray.BalloonTipTitle = "Dialvia"; tray.BalloonTipText = text; tray.ShowBalloonTip(2500); }
        }
        void DeviceAction(Action action) { try { action(); } catch (Exception ex) { AppPaths.Log("DEVICE_ACTION " + ex.Message); } }
        bool Once(string action) {
            DateTime now = DateTime.UtcNow;
            if (lastAction == action && (now - lastActionTime).TotalMilliseconds < 600) return false;
            lastAction = action; lastActionTime = now; return true;
        }
        void Dispatch(string code) {
            CancelPendingGreen(); code = (code ?? "").Trim(); string kind = DialCode.Kind(code, Browsing);
            AppPaths.Log("DIAL " + code + " " + kind);
            if (kind == "browse" || kind == "previous" || kind == "next") {
                BeginBrowsing(kind == "previous" ? -1 : kind == "next" ? 1 : 0);
                DeviceAction(delegate { adapter.ClearDialDisplay(); }); return;
            }
            if (kind == "finish") { FinishBrowsing(); DeviceAction(delegate { adapter.ClearDialDisplay(); }); return; }
            if (kind == "send") {
                if (!Once("send-draft")) return;
                FinishBrowsing(false); string message;
                ReconcileVoice();
                if (DraftSubmission.Request(codex, AppPaths.CodexHome, VoiceBusy || MicrophoneActivity.Read().State == CaptureState.Active, settings.dryRun, out message)) AppPaths.Log("SEND_REQUEST composer.submit");
                Hint(message); DeviceAction(delegate { adapter.ClearDialDisplay(); }); return;
            }
            if (kind == "start") {
                if (code == "0") StartVoice();
                else HandleBareButton(); return;
            }
            if (kind == "end") { EndVoice(); return; }
            if (kind == "mute") { SendShortcut("mute"); return; }
            if (kind == "unknown") { Hint("22 进入会话切换；2 上／8 下／5 退出；项目 *1–*9。输入后按绿色键。"); return; }
            Target target; string slot = code.Substring(code.Length - 1);
            if (kind == "session" && settings.sessionRouting == "recent") {
                try {
                    if (recentSnapshot == null) recentSnapshot = new SessionSnapshot(CodexInventory.Load(AppPaths.CodexHome), activeProjectPath);
                    RecentThread recent = recentSnapshot.Slot(Int32.Parse(slot));
                    if (recent == null) { Hint("当前项目没有这条最近会话，请打开会话浏览器。"); return; }
                    OpenTarget(new Target { label = recent.Title, url = recent.Url, projectPath = recent.ProjectPath });
                } catch (Exception ex) { Hint(ex.Message); }
                return;
            }
            if (!(kind == "project" ? settings.projects : settings.sessions).TryGetValue(slot, out target)) { Hint("此编号尚未设置，请打开“会话与项目”。"); return; }
            if (kind == "project") {
                activeProjectPath = target.projectPath ?? "";
                if (activeProjectPath == "") { try { var match = CodexInventory.Load(AppPaths.CodexHome).FirstOrDefault(x => x.Url == target.url); if (match != null) activeProjectPath = match.ProjectPath; } catch { } }
                recentSnapshot = null;
            }
            OpenTarget(target);
        }
        void OpenTarget(Target target) {
            if (!Once(target.url)) return;
            FinishBrowsing(false);
            if (settings.dryRun) { Hint("检测成功：" + target.label); return; }
            if (VoiceBusy && !EndVoice()) return;
            try { codex.OpenThread(target.url); Hint("进入：" + target.label); DeviceAction(delegate { adapter.ClearDialDisplay(); }); }
            catch (Exception ex) { Hint("切换失败：" + ex.Message); }
        }
        void ShowBrowser(string project, int movement) {
            CancelPendingGreen();
            if (VoiceBusy && !EndVoice()) return;
            nativeBrowsing = false;
            try {
                if (browser == null || browser.IsDisposed) {
                    browser = new SessionBrowserWindow(project, delegate { return CodexInventory.Load(AppPaths.CodexHome); }, delegate(RecentThread thread) { OpenTarget(new Target { label = thread.Title, url = thread.Url, projectPath = thread.ProjectPath }); });
                    browser.SelectionContextChanged += delegate { recentSnapshot = browser.Snapshot; activeProjectPath = browser.ProjectPath; };
                    recentSnapshot = browser.Snapshot; activeProjectPath = browser.ProjectPath;
                }
                browser.Show(); browser.Activate(); if (movement != 0) browser.MoveSelection(movement);
            } catch (Exception ex) { Hint(ex.Message); }
        }
        void BeginBrowsing(int movement) {
            if (settings.browseMode == "list" || (browser != null && !browser.IsDisposed)) { ShowBrowser(activeProjectPath, movement); return; }
            if (VoiceBusy && !EndVoice()) return;
            if (browser != null && !browser.IsDisposed) browser.Close();
            try {
                string error;
                if (!settings.dryRun && !codex.Focus(out error)) { Hint(error); return; }
                bool entering = !nativeBrowsing; int oldDirection = nativeDirection; nativeBrowsing = true;
                if (entering) nativeDirection = 1;
                if (movement != 0) nativeDirection = movement > 0 ? 1 : -1;
                if (entering || oldDirection != nativeDirection) Hint("绿色键继续向" + (nativeDirection > 0 ? "下" : "上") + "切换；2＋绿向上，8＋绿向下，5＋绿退出。");
                tray.Text = "Dialvia · 绿键" + (nativeDirection > 0 ? "下一条" : "上一条") + " · 2上 / 8下 / 5退出";
                if (movement == 0) return;
                string chord = UserIntegration.SidebarShortcut(AppPaths.CodexHome, movement > 0);
                if (settings.dryRun) { Hint("检测成功：Codex " + (movement > 0 ? "下一条" : "上一条") + " → " + chord); return; }
                if (!codex.Shortcut(chord, out error)) { Hint(error); return; }
                AppPaths.Log("NATIVE_NAV " + (movement > 0 ? "nextThread" : "previousThread"));
            } catch (Exception ex) { Hint(ex.Message); }
        }
        void ConfirmBrowsing() {
            if (nativeBrowsing) { BeginBrowsing(nativeDirection); DeviceAction(delegate { adapter.ClearDialDisplay(); }); }
            else if (browser != null && !browser.IsDisposed) browser.Confirm();
        }
        void CancelPendingGreen() { pendingGreen.Cancel(); if (talkDelay != null) talkDelay.Stop(); }
        void FinishBrowsing() { FinishBrowsing(true); }
        void FinishBrowsing(bool announce) {
            CancelPendingGreen();
            bool wasBrowsing = Browsing; nativeBrowsing = false;
            if (browser != null && !browser.IsDisposed) browser.Close();
            if (tray != null) { string tip = "Dialvia · " + deviceStatus; tray.Text = tip.Length > 63 ? tip.Substring(0, 63) : tip; }
            if (wasBrowsing && announce) Hint("已退出会话切换，保留当前 Codex 会话。");
        }
        bool SendShortcut(string action) {
            string chord = settings.shortcuts[action];
            if (settings.dryRun) { Hint("检测成功：" + action + " → " + chord); return true; }
            string error; if (codex.Shortcut(chord, out error)) return true;
            Hint(error); return false;
        }
        void StartVoice() {
            CancelPendingGreen(); ReconcileVoice(); if (VoiceBusy || !Once("start")) return;
            FinishBrowsing(false);
            if (!settings.voiceEnabled) { Hint("语音控制已在设置中关闭。"); return; }
            if (settings.voiceMode == "dictation" && !settings.dryRun) {
                if (MicrophoneActivity.Read().State == CaptureState.Active) { Hint("Codex 正在使用麦克风；请先在界面结束当前录音，再按 0＋绿键。"); return; }
                try {
                    string error;
                    if (!codex.Shortcut(UserIntegration.FocusComposerShortcut(AppPaths.CodexHome), out error)) { Hint(error); return; }
                    // Let the native focus command reach the renderer before requesting global dictation.
                    voiceStartDelay.Start();
                } catch (Exception ex) { Hint(ex.Message); }
                return;
            }
            if (!SendShortcut(settings.voiceMode == "dictation" ? "dictate" : "start") || settings.dryRun) return;
            conversationActive = true; DeviceAction(delegate { adapter.SetVoiceState(true); });
        }
        void HandleBareButton() {
            ReconcileVoice(); string action = BareVoiceButton.Action(Browsing, VoiceBusy);
            if (action == "browse") ConfirmBrowsing(); else if (action == "stop") EndVoice();
            else Hint("开始听写：0＋绿键；发送已检查的草稿：00＋绿键。");
        }
        void ReconcileVoice() {
            if (!dictation.Active) return;
            if (dictation.Observe(MicrophoneActivity.Read())) {
                DeviceAction(delegate { adapter.SetVoiceState(false); });
                AppPaths.Log("VOICE_SYNC microphone inactive; no toggle sent");
            }
        }
        bool EndVoice() {
            CancelPendingGreen(); if (voiceStartDelay != null) voiceStartDelay.Stop();
            if (Browsing) { FinishBrowsing(); return true; } ReconcileVoice();
            if (dictation.Active) {
                if (!dictation.CanRequestStop(MicrophoneActivity.Read(), DateTime.UtcNow)) { Hint("停止请求正在确认；请等文字出现。界面已停止而状态未同步时，可在托盘校正状态。"); return false; }
                if (!SendShortcut("dictate")) return false;
                dictation.StopRequested(DateTime.UtcNow);
                AppPaths.Log("VOICE_STOP_REQUEST awaiting capture confirmation");
                Hint("已请求结束听写；等录音停止、文字出现后，再按 00＋绿键发送。"); return false;
            }
            if (!conversationActive) return true;
            if (!SendShortcut("end")) return false;
            conversationActive = false; DeviceAction(delegate { adapter.SetVoiceState(false); }); return true;
        }
        void ResetVoiceTracking() {
            CancelPendingGreen(); voiceStartDelay.Stop(); conversationActive = false; dictation.Reset(); DeviceAction(delegate { adapter.SetVoiceState(false); });
            Hint("已校正为停止录音状态，没有向 Codex 发送快捷键。");
        }
        void ShowSettings(int page) {
            if (settingsWindow == null || settingsWindow.IsDisposed) {
                settingsWindow = new SettingsWindow(settings, delegate(Settings updated) {
                    if (VoiceBusy) throw new InvalidOperationException("请先停止当前语音。");
                    FinishBrowsing(false);
                    SettingsStore.Save(AppPaths.Config, updated);
                    settings = SettingsStore.Json.Deserialize<Settings>(SettingsStore.Json.Serialize(updated));
                    activeProjectPath = settings.recentProjectPath; recentSnapshot = null; Hint("设置已保存。");
                }, delegate { return deviceStatus; }, delegate(Settings proposed) {
                    Settings previous = settings;
                    try { settings = proposed; NotifyTask("Dialvia 通知测试"); } finally { settings = previous; }
                }, ResetVoiceTracking, delegate(string project) { ShowBrowser(project, 0); }, delegate { return CodexInventory.Load(AppPaths.CodexHome); }, false);
            }
            settingsWindow.Show(); settingsWindow.SelectPage(page); settingsWindow.Activate();
        }
        void NotifyTask(string message) {
            Hint(message); AppPaths.Log("TASK_NOTIFICATION");
            if (settings.ringNotifications && !VoiceBusy && !settings.dryRun) DeviceAction(delegate { adapter.Ring(true); ringTimer.Stop(); ringTimer.Start(); });
        }
        void ReadInbox() {
            foreach (string file in Directory.GetFiles(AppPaths.Inbox, "*.json").Take(5)) {
                try {
                    var info = new FileInfo(file); if (info.Length > 16384 || DateTime.UtcNow - info.LastWriteTimeUtc > TimeSpan.FromMinutes(2)) continue;
                    var payload = SettingsStore.Json.Deserialize<System.Collections.Generic.Dictionary<string, object>>(File.ReadAllText(file)); object value;
                    string message = payload.TryGetValue("message", out value) ? Convert.ToString(value) : "Codex 已完成回复";
                    NotifyTask(message.Length > 180 ? message.Substring(0, 180) : message);
                } catch (Exception ex) { AppPaths.Log("NOTIFY " + ex.Message); }
                finally { try { File.Delete(file); } catch { } }
            }
            string request = Path.Combine(AppPaths.Data, "settings.request");
            if (File.Exists(request)) { File.Delete(request); ShowSettings(0); }
        }
        protected override void OnFormClosing(FormClosingEventArgs e) {
            if (!closing && VoiceBusy && !EndVoice()) { e.Cancel = true; Hint("请先在 Codex 中结束录音，再退出 Dialvia。"); return; }
            closing = true; base.OnFormClosing(e);
        }
        protected override void OnFormClosed(FormClosedEventArgs e) {
            foreach (Timer timer in new[] { reconnect, talkDelay, inboxTimer, ringTimer, voiceStartDelay }) if (timer != null) timer.Dispose();
            if (browser != null) browser.Dispose(); if (settingsWindow != null) settingsWindow.Dispose();
            if (adapter != null) adapter.Dispose(); if (tray != null) { tray.Visible = false; tray.Dispose(); }
            base.OnFormClosed(e);
        }
    }
}
