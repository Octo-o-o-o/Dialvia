using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Dialvia;

class CoreTests {
    sealed class FakeDesktop : ICodexDesktop {
        public int Calls; public string Last; public bool Accept = true;
        public bool Focus(out string error) { error = null; return true; }
        public bool Shortcut(string chord, out string error) { Calls++; Last = chord; error = Accept ? null : "focus refused"; return Accept; }
        public void OpenThread(string url) { throw new Exception("Unexpected target navigation"); }
    }
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_open_v2(byte[] path, out IntPtr database, int flags, IntPtr vfs);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_exec(IntPtr database, byte[] sql, IntPtr callback, IntPtr argument, IntPtr error);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_close(IntPtr database);
    static void FixtureDatabase(string path, string sql) {
        IntPtr database; if (sqlite3_open_v2(Encoding.UTF8.GetBytes(path+"\0"), out database, 6, IntPtr.Zero) != 0) throw new Exception("fixture open");
        try { if (sqlite3_exec(database, Encoding.UTF8.GetBytes(sql+"\0"), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) != 0) throw new Exception("fixture schema"); }
        finally { sqlite3_close(database); }
    }
    static int passed;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; }
    static void Reject(Action action, string name) { try { action(); } catch (ArgumentException) { passed++; return; } catch (InvalidDataException) { passed++; return; } catch (InvalidOperationException) { passed++; return; } throw new Exception("Not rejected: " + name); }
    static void Main(string[] args) {
        SettingsStore.Validate(new Settings());
        Check(new Settings().sessionRouting == "recent", "recent sessions are the public default");
        Check(new Settings().browseMode == "codex", "handset browsing uses native Codex commands by default");
        Check(DialCode.Kind("00") == "send" && DialCode.Kind("00", true) == "send", "explicit draft submission independent of browsing");
        var legacy = SettingsStore.Json.Deserialize<Settings>("{\"schemaVersion\":1}"); SettingsStore.Validate(legacy);
        Check(legacy.sendShortcut == "Ctrl+Alt+Shift+F12", "legacy settings get the dedicated submit shortcut");
        Check(DialCode.Kind("22") == "browse" && DialCode.Kind("23") == "finish", "numeric enter and unconditional exit");
        Check(DialCode.Kind("2", true) == "previous" && DialCode.Kind("8", true) == "next" && DialCode.Kind("5", true) == "finish", "numeric navigation context");
        Check(DialCode.Kind("2") == "session" && DialCode.Kind("8") == "session" && DialCode.Kind("5") == "session", "numbered destinations preserved outside browsing");
        Check(DialCode.Kind("0", true) == "start" && DialCode.Kind("*2", true) == "project", "voice and projects remain available during browsing");
        Check(ShortcutKeys.Parse("Ctrl+Alt+[").Contains((ushort)0xdb) && ShortcutKeys.Parse("Ctrl+Alt+]").Contains((ushort)0xdd), "native sidebar OEM bracket chords");
        Reject(delegate { ShortcutKeys.Parse("Ctrl+Left+F8"); }, "mixed action keys");
        var queued = new DeferredGreenPresses(); DateTime origin = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Check(BareVoiceButton.Action(false, false) == "idle", "unidentified bare green/red cannot start recording while idle");
        Check(BareVoiceButton.Action(false, true) == "stop" && BareVoiceButton.Action(true, false) == "browse", "bare button preserves stop and continuous browsing");
        Check(ShortcutKeys.Parse("Alt+L").SequenceEqual(new ushort[] {0x12, 0x4c}), "native main composer focus chord");
        Check(MicrophoneActivity.FromFileTimes(0, 0).State == CaptureState.Unknown && MicrophoneActivity.FromFileTimes(Int64.MaxValue, 0).State == CaptureState.Unknown, "missing and corrupt capture metadata remain unknown");
        Check(MicrophoneActivity.FromFileTimes(origin.ToFileTimeUtc(), 0).State == CaptureState.Active, "ongoing capture is recognized without accessing audio");
        var stopped = MicrophoneActivity.FromFileTimes(origin.ToFileTimeUtc(), origin.AddSeconds(1).ToFileTimeUtc());
        Check(stopped.ConfirmsStopped(origin), "a confirmed stop after start avoids a blind toggle");
        Check(!stopped.ConfirmsStopped(origin.AddSeconds(5)), "old inactive metadata can never clear a newly requested start");
        Check(!MicrophoneActivity.FromFileTimes(origin.ToFileTimeUtc(), origin.AddSeconds(-1).ToFileTimeUtc()).ConfirmsStopped(origin), "inconsistent metadata cannot assert stopped");
        var lifecycle = new DictationState();
        DateTime request = origin.AddSeconds(5); var liveCapture = MicrophoneActivity.FromFileTimes(request.ToFileTimeUtc(), 0);
        lifecycle.StartRequested(request);
        Check(!lifecycle.Observe(stopped) && lifecycle.Active, "stale metadata leaves a new dictation tracked");
        Check(lifecycle.CanRequestStop(liveCapture, request.AddSeconds(10)), "first stop remains available after delayed microphone startup");
        lifecycle.StopRequested(request.AddSeconds(10));
        Check(lifecycle.Active && lifecycle.StopPending, "dispatching a stop is not confirmation of stopped recording");
        Check(!lifecycle.CanRequestStop(liveCapture, request.AddSeconds(11)), "rapid duplicate stops cannot reopen recording");
        Check(lifecycle.CanRequestStop(liveCapture, request.AddSeconds(13)), "explicit retry only after delay and known continuing capture");
        Check(!lifecycle.CanRequestStop(MicrophoneActivity.FromFileTimes(0,0), request.AddSeconds(13)), "unknown capture cannot authorize a repeat toggle");
        Check(!lifecycle.Observe(stopped) && lifecycle.Active, "old stop cannot clear a pending stop request");
        Check(lifecycle.Observe(MicrophoneActivity.FromFileTimes(request.ToFileTimeUtc(),request.AddSeconds(14).ToFileTimeUtc())) && !lifecycle.Active && !lifecycle.StopPending, "fresh stop confirmation clears active and pending state");
        queued.Press(origin); queued.Press(origin.AddMilliseconds(80)); queued.Press(origin.AddMilliseconds(160));
        Check(!queued.TakeReady(origin.AddMilliseconds(249)), "green dispatch waits for a buffered dial request");
        Check(queued.TakeReady(origin.AddMilliseconds(250)) && queued.Count == 2, "rapid presses retain the first action");
        Check(!queued.TakeReady(origin.AddMilliseconds(250)) && queued.TakeReady(origin.AddMilliseconds(330)) && queued.TakeReady(origin.AddMilliseconds(410)) && queued.Count == 0, "three rapid green presses produce three actions");
        queued.Press(origin); queued.Cancel(); Check(!queued.TakeReady(origin.AddSeconds(1)), "dial codes and mode exit cancel pending green actions");
        Check(ShortcutKeys.Parse("]+Ctrl+Alt").Last() == 0xdd, "modifiers are pressed before the action even for reordered custom chords");
        var recent = new List<RecentThread> {
            new RecentThread {Id="11111111-1111-4111-8111-111111111111",Title="first",ProjectPath=@"C:\Demo\Project",UpdatedAt=20},
            new RecentThread {Id="22222222-2222-4222-8222-222222222222",Title="second",ProjectPath=@"c:\demo\project\child",UpdatedAt=30},
            new RecentThread {Id="33333333-3333-4333-8333-333333333333",Title="other",ProjectPath=@"C:\Demo\Project2",UpdatedAt=40}
        };
        var frozen = new SessionSnapshot(recent, @"C:\Demo\Project");
        Check(frozen.Threads.Count == 2 && frozen.Slot(1).Title == "second", "project scoped most recently active ordering");
        recent[0].UpdatedAt=100;
        Check(frozen.Slot(1).Title == "second", "opening a thread does not renumber an existing snapshot");
        Check(new SessionSnapshot(recent, @"C:\Demo\Project").Slot(1).Title == "first", "explicit refresh updates recency");
        Check(frozen.Slot(0) == null && frozen.Slot(9) == null, "absent snapshot slots do not resolve");
        Check(DialCode.Kind("*#") == "browse" && DialCode.Kind("*") == "previous" && DialCode.Kind("#") == "next", "handset browsing commands");
        Check(DialCode.Kind("*2") == "project" && DialCode.Kind("12") == "project" && DialCode.Kind("#9") == "session", "number routing");
        Check(DialCode.Kind("*0") == "end" && DialCode.Kind("#0") == "mute" && DialCode.Kind("0") == "start", "reserved codes");
        foreach (string value in new[] {"1;calc", "https://example.com", "*10", "-1", "99", "#00"}) Check(DialCode.Kind(value) == "unknown", "unknown dial code");
        Check(CodexDesktop.NativeInputSize == (IntPtr.Size == 8 ? 40 : 28), "native SendInput layout");
        Reject(delegate { ShortcutKeys.Parse("Ctrl+F8+F9"); }, "multiple action keys");
        Reject(delegate { ShortcutKeys.Parse("Ctrl+Ctrl+F8"); }, "duplicate modifier");
        Reject(delegate { ShortcutKeys.Parse("Ctrl+Enter"); }, "unsupported chord");
        var invalid = new Settings(); invalid.voiceMode = "external"; Reject(delegate { SettingsStore.Validate(invalid); }, "unknown voice mode");
        invalid = new Settings(); invalid.device = "any-hid"; Reject(delegate { SettingsStore.Validate(invalid); }, "unimplemented device");
        invalid = new Settings(); invalid.browseMode = "external"; Reject(delegate { SettingsStore.Validate(invalid); }, "unimplemented browse mode");
        invalid = new Settings(); invalid.sessions["1"] = new Target { label = "test", url = "file:///C:/Windows/System32/calc.exe" };
        Reject(delegate { SettingsStore.Validate(invalid); }, "arbitrary URI");
        invalid.sessions["1"].url = "codex://threads/------------------------------------";
        Reject(delegate { SettingsStore.Validate(invalid); }, "invalid GUID");
        string root = Path.Combine(Path.GetTempPath(), "dialvia-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            string codex = Path.Combine(root, "codex"), data = Path.Combine(root, "data"); Directory.CreateDirectory(codex);
            Check(CodexInventory.Load(codex).Count == 0 && !File.Exists(Path.Combine(codex, "state_5.sqlite")), "inventory discovery never creates a missing database");
            string databasePath = Path.Combine(codex, "state_5.sqlite");
            FixtureDatabase(databasePath, "CREATE TABLE threads(id TEXT,title TEXT,cwd TEXT,updated_at INTEGER,archived INTEGER); INSERT INTO threads VALUES('11111111-1111-4111-8111-111111111111','中文会话','C:\\Demo',20,0),('22222222-2222-4222-8222-222222222222','archived','C:\\Demo',30,1),('invalid','bad','C:\\Demo',40,0);");
            byte[] databaseBefore = File.ReadAllBytes(databasePath); DateTime modifiedBefore = File.GetLastWriteTimeUtc(databasePath);
            var discovered = CodexInventory.Load(codex);
            Check(discovered.Count == 1 && discovered[0].Title == "中文会话", "inventory accepts Unicode and excludes archived or invalid IDs");
            Check(databaseBefore.SequenceEqual(File.ReadAllBytes(databasePath)) && modifiedBefore == File.GetLastWriteTimeUtc(databasePath), "inventory leaves database bytes and modified time unchanged");
            Check(!File.Exists(databasePath+"-journal") && !File.Exists(databasePath+"-wal"), "inventory creates no write journal");
            File.Delete(databasePath); FixtureDatabase(databasePath, "CREATE TABLE threads(id TEXT);");
            Reject(delegate { CodexInventory.Load(codex); }, "incompatible inventory schema has a manual-link fallback");
            string bindingPath = Path.Combine(codex, "keybindings.json");
            Check(UserIntegration.SidebarShortcut(codex, false) == "Ctrl+Alt+[" && UserIntegration.SidebarShortcut(codex, true) == "Ctrl+Alt+]", "sidebar navigation uses sidebar commands rather than recent tab cycling");
            Check(UserIntegration.FocusComposerShortcut(codex) == "Alt+L", "focus the current main composer before global dictation");
            File.WriteAllText(bindingPath, "[{\"command\":\"focusMainChat\",\"key\":null}]");
            Reject(delegate { UserIntegration.FocusComposerShortcut(codex); }, "disabled composer focus cannot fall back into global overlay");
            File.WriteAllText(bindingPath, "[{\"command\":\"other\",\"key\":\"Alt+L\"}]");
            Reject(delegate { UserIntegration.FocusComposerShortcut(codex); }, "composer focus collision is refused");
            File.WriteAllText(bindingPath, "[{\"command\":\"focusMainChat\",\"key\":\"Ctrl+F4\"}]");
            Check(UserIntegration.FocusComposerShortcut(codex) == "Ctrl+F4", "custom compatible composer focus is respected");
            File.Delete(bindingPath);
            Reject(delegate { UserIntegration.SubmitShortcut(codex); }, "submit has no blind Enter or unconfigured fallback");
            File.WriteAllText(bindingPath, "[{\"command\":\"previousThread\",\"key\":\"Ctrl+Shift+F3\"}]");
            string navigationBefore = File.ReadAllText(bindingPath);
            Check(UserIntegration.SidebarShortcut(codex, false) == "Ctrl+Shift+F3" && File.ReadAllText(bindingPath) == navigationBefore, "honor custom sidebar binding without mutation");
            File.WriteAllText(bindingPath, "[{\"command\":\"previousThread\",\"key\":null}]");
            Reject(delegate { UserIntegration.SidebarShortcut(codex, false); }, "disabled sidebar binding never falls back");
            File.WriteAllText(bindingPath, "[{\"command\":\"other\",\"key\":\"Alt+Ctrl+[\"}]");
            Reject(delegate { UserIntegration.SidebarShortcut(codex, false); }, "default sidebar collision regardless of modifier order");
            File.WriteAllText(bindingPath, "[{\"command\":\"previousThread\",\"key\":\"Ctrl+F3\"},{\"command\":\"other\",\"key\":\"Ctrl+F3\"}]");
            Reject(delegate { UserIntegration.SidebarShortcut(codex, false); }, "explicit sidebar duplicate collision");
            File.WriteAllText(bindingPath, "[{\"command\":\"composer.submit\",\"key\":null}]");
            Reject(delegate { UserIntegration.SubmitShortcut(codex); }, "disabled submit is respected");
            File.WriteAllText(bindingPath, "[{\"command\":\"composer.submit\",\"key\":\"Ctrl+F12\"},{\"command\":\"other\",\"key\":\"Ctrl+F12\"}]");
            Reject(delegate { UserIntegration.SubmitShortcut(codex); }, "submit collision is refused");
            File.WriteAllText(bindingPath, "[{\"command\":\"unrelated\",\"key\":\"Ctrl+F1\"}]");
            UserIntegration.InstallBindings(codex, data, new Settings());
            string once = File.ReadAllText(bindingPath); UserIntegration.InstallBindings(codex, data, new Settings());
            Check(once.Contains("composer.submit") && UserIntegration.SubmitShortcut(codex) == "Ctrl+Alt+Shift+F12", "setup owns a dedicated native composer submit binding");
            var fake = new FakeDesktop(); string submitMessage;
            Check(!DraftSubmission.Request(fake, codex, true, false, out submitMessage) && fake.Calls == 0, "recording must finish before any send command");
            Check(!DraftSubmission.Request(fake, codex, false, true, out submitMessage) && fake.Calls == 0, "detection mode cannot submit a draft");
            Check(DraftSubmission.Request(fake, codex, false, false, out submitMessage) && fake.Calls == 1 && fake.Last == "Ctrl+Alt+Shift+F12", "explicit request dispatches the dedicated submit command once");
            fake.Accept = false;
            Check(!DraftSubmission.Request(fake, codex, false, false, out submitMessage) && submitMessage == "focus refused", "foreground refusal is returned without fallback keys");
            Check(File.ReadAllText(bindingPath) == once, "idempotent shortcut install");
            Check(once.Contains("unrelated"), "preserve existing shortcuts");
            File.WriteAllText(bindingPath, once.Replace("Ctrl+Alt+Shift+F8", "Ctrl+Alt+Shift+F7"));
            UserIntegration.RemoveBindings(codex, data);
            Check(File.ReadAllText(bindingPath).Contains("F7") && File.ReadAllText(bindingPath).Contains("unrelated"), "preserve user-edited shortcut on removal");
            File.WriteAllText(bindingPath, "[{\"command\":\"other\",\"key\":\"Ctrl+Alt+Shift+F8\"}]");
            string before = File.ReadAllText(bindingPath);
            Reject(delegate { UserIntegration.InstallBindings(codex, data, new Settings()); }, "shortcut collision");
            Check(File.ReadAllText(bindingPath) == before, "collision leaves file unchanged");
            File.WriteAllText(bindingPath, "[{\"command\":\"globalDictationSingleTap\",\"key\":\"Ctrl+F2\"}]");
            before = File.ReadAllText(bindingPath); Reject(delegate { UserIntegration.InstallBindings(codex, data, new Settings()); }, "existing custom command");
            Check(File.ReadAllText(bindingPath) == before, "custom binding preserved");
            string hooks = Path.Combine(codex, "hooks.json");
            File.WriteAllText(hooks, "{\"description\":\"keep\",\"hooks\":{\"Stop\":[{\"matcher\":\"preserve\",\"hooks\":[{\"type\":\"command\",\"command\":\"existing.exe\"}]}],\"SessionStart\":[]}}");
            string exe = Path.Combine(root, "Dialvia.exe"); UserIntegration.InstallHook(codex, exe); once = File.ReadAllText(hooks);
            UserIntegration.InstallHook(codex, exe); Check(File.ReadAllText(hooks) == once, "idempotent hook install");
            Check(once.Contains("existing.exe") && once.Contains("preserve") && once.Contains("SessionStart"), "preserve hook groups and metadata");
            UserIntegration.RemoveHook(codex, exe); string after = File.ReadAllText(hooks);
            Check(after.Contains("existing.exe") && !after.Contains("--notify"), "remove exact owned hook only");
            Check(!File.Exists(Path.Combine(codex, "config.toml")), "legacy notify never modified");
            string config = Path.Combine(data, "settings.json"); SettingsStore.Save(config, new Settings());
            Check(SettingsStore.Load(config).dryRun, "safe public default");
            var bad = new Settings(); bad.projects["0"] = new Target { label = "bad", url = "codex://threads/11111111-1111-4111-8111-111111111111" };
            before = File.ReadAllText(config); Reject(delegate { SettingsStore.Save(config, bad); }, "invalid target");
            Check(File.ReadAllText(config) == before, "invalid settings do not replace working config");
        } finally { Directory.Delete(root, true); }
        string report = SettingsStore.Json.Serialize(new { passed = true, assertions = passed });
        if (args.Length == 1) File.WriteAllText(args[0], report);
        Console.WriteLine(report);
    }
}
