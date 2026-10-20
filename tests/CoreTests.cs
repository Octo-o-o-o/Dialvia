using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Dialvia;

class CoreTests {
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
            File.WriteAllText(bindingPath, "[{\"command\":\"unrelated\",\"key\":\"Ctrl+F1\"}]");
            UserIntegration.InstallBindings(codex, data, new Settings());
            string once = File.ReadAllText(bindingPath); UserIntegration.InstallBindings(codex, data, new Settings());
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
