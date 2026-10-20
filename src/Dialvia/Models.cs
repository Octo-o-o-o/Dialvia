using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace Dialvia {
    public sealed class Target { public string label; public string url; public string projectPath; }
    public sealed class Settings {
        public int schemaVersion = 1;
        public string device = "plantronics-p240";
        public bool dryRun = true;
        public string voiceMode = "dictation";
        public string sessionRouting = "recent";
        public string browseMode = "codex";
        public string recentProjectPath = "";
        public bool voiceEnabled = true, balloonNotifications = true, ringNotifications = false;
        public Dictionary<string, string> shortcuts = new Dictionary<string, string> {
            {"dictate", "Ctrl+Alt+Shift+F8"}, {"start", "Ctrl+Alt+Shift+F9"},
            {"end", "Ctrl+Alt+Shift+F10"}, {"mute", "Ctrl+Alt+Shift+F11"}
        };
        public Dictionary<string, Target> sessions = new Dictionary<string, Target>();
        public Dictionary<string, Target> projects = new Dictionary<string, Target>();
    }
    public static class SettingsStore {
        public static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        public static Settings Load(string path) {
            Settings value = Json.Deserialize<Settings>(File.ReadAllText(path)); Validate(value); return value;
        }
        public static void Validate(Settings value) {
            if (value == null || value.schemaVersion != 1 || value.device != "plantronics-p240")
                throw new InvalidDataException("Unsupported configuration or device profile.");
            if (value.voiceMode != "dictation" && value.voiceMode != "conversation")
                throw new InvalidDataException("voiceMode must be dictation or conversation.");
            if (value.sessionRouting != "recent" && value.sessionRouting != "fixed")
                throw new InvalidDataException("sessionRouting must be recent or fixed.");
            if (value.browseMode != "codex" && value.browseMode != "list")
                throw new InvalidDataException("browseMode must be codex or list.");
            if (value.sessions == null || value.projects == null || value.shortcuts == null)
                throw new InvalidDataException("Missing target or shortcut configuration.");
            foreach (var map in new[] {value.sessions, value.projects}) foreach (var item in map) {
                Guid id;
                if (!Regex.IsMatch(item.Key, "^[1-9]$") || item.Value == null ||
                    String.IsNullOrWhiteSpace(item.Value.label) || item.Value.label.Length > 120 ||
                    !Regex.IsMatch(item.Value.url ?? "", "^codex://threads/[0-9a-fA-F-]{36}$") ||
                    !Guid.TryParse(item.Value.url.Substring(16), out id))
                    throw new InvalidDataException("Targets require a 1–9 slot, label and Codex thread link.");
            }
            foreach (string key in new[] {"dictate", "start", "end", "mute"}) {
                string chord;
                if (!value.shortcuts.TryGetValue(key, out chord)) throw new InvalidDataException("Missing shortcut: " + key);
                ShortcutKeys.Parse(chord);
            }
        }
        public static void Save(string path, Settings value) {
            Validate(value); AtomicFile.Write(path, Json.Serialize(value));
        }
    }
    public static class DialCode {
        public static string Kind(string code) {
            return Kind(code, false);
        }
        public static string Kind(string code, bool browsing) {
            code = (code ?? "").Trim();
            if (browsing) {
                if (code == "2") return "previous";
                if (code == "8") return "next";
                if (code == "5") return "finish";
            }
            if (code == "" || code == "0") return "start";
            if (code == "*0") return "end";
            if (code == "#0") return "mute";
            if (code == "*#" || code == "22") return "browse";
            if (code == "23") return "finish";
            if (code == "*") return "previous";
            if (code == "#") return "next";
            if (Regex.IsMatch(code, "^\\*[1-9]$") || Regex.IsMatch(code, "^1[1-9]$")) return "project";
            if (Regex.IsMatch(code, "^#?[1-9]$")) return "session";
            return "unknown";
        }
    }
    public sealed class DeferredGreenPresses {
        readonly Queue<DateTime> presses = new Queue<DateTime>();
        public int Count { get { return presses.Count; } }
        public void Press(DateTime at) { if (presses.Count < 32) presses.Enqueue(at); }
        public void Cancel() { presses.Clear(); }
        public bool TakeReady(DateTime now) {
            if (presses.Count == 0 || (now - presses.Peek()).TotalMilliseconds < 250) return false;
            presses.Dequeue(); return true;
        }
    }
    public static class AtomicFile {
        public static void Write(string path, string content) {
            string directory = Path.GetDirectoryName(Path.GetFullPath(path)); Directory.CreateDirectory(directory);
            string pending = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(pending, content, new System.Text.UTF8Encoding(false));
            try {
                if (File.Exists(path)) File.Replace(pending, path, null);
                else File.Move(pending, path);
            } finally { if (File.Exists(pending)) File.Delete(pending); }
        }
    }
    public static class AppPaths {
        public static readonly string Data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dialvia");
        // Documents is shared with a packaged Codex process; LocalAppData can be virtualized by MSIX.
        public static readonly string Inbox = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Codex\Dialvia\notifications");
        public static readonly string Config = Path.Combine(Data, "config.json");
        public static string CodexHome {
            get { string configured = Environment.GetEnvironmentVariable("CODEX_HOME");
                return String.IsNullOrWhiteSpace(configured) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex") : Path.GetFullPath(configured); }
        }
        public static void Initialize() {
            Directory.CreateDirectory(Data); Directory.CreateDirectory(Inbox);
            if (!File.Exists(Config)) SettingsStore.Save(Config, new Settings());
        }
        public static void Log(string text) {
            try {
                string path = Path.Combine(Data, "dialvia.log");
                if (File.Exists(path) && new FileInfo(path).Length > 1048576) { File.Copy(path, path + ".previous", true); File.WriteAllText(path, ""); }
                File.AppendAllText(path, DateTime.Now.ToString("o") + " " + text + Environment.NewLine);
            } catch { }
        }
    }
}
