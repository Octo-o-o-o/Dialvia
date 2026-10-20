using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dialvia {
    public static class UserIntegration {
        public static readonly Dictionary<string, string> Commands = new Dictionary<string, string> {
            {"dictate", "globalDictationSingleTap"}, {"start", "composer.startVoiceMode"},
            {"end", "realtimeVoice.endCall"}, {"mute", "realtimeVoice.toggleMicrophoneMute"}, {"send", "composer.submit"}
        };
        static List<Dictionary<string, object>> Rows(object value) {
            var result = new List<Dictionary<string, object>>();
            if (value == null) return result;
            var array = value as IEnumerable;
            if (array == null || value is string || value is IDictionary) throw new InvalidDataException("Expected a JSON array.");
            foreach (object row in array) {
                var map = row as Dictionary<string, object>;
                if (map == null) throw new InvalidDataException("Expected a JSON object in array."); result.Add(map);
            }
            return result;
        }
        static string Text(Dictionary<string, object> item, string key) { object value; return item.TryGetValue(key, out value) ? Convert.ToString(value) : null; }
        public static string SidebarShortcut(string codexHome, bool next) {
            return ResolveCommandShortcut(codexHome, next ? "nextThread" : "previousThread", next ? "Ctrl+Alt+]" : "Ctrl+Alt+[");
        }
        public static string SubmitShortcut(string codexHome) { return ResolveCommandShortcut(codexHome, "composer.submit", null); }
        public static string FocusComposerShortcut(string codexHome) { return ResolveCommandShortcut(codexHome, "focusMainChat", "Alt+L"); }
        static string ResolveCommandShortcut(string codexHome, string command, string fallback) {
            string path = Path.Combine(codexHome, "keybindings.json");
            var rows = File.Exists(path) ? Rows(SettingsStore.Json.DeserializeObject(File.ReadAllText(path))) : new List<Dictionary<string, object>>();
            var configured = rows.Where(x => Text(x, "command") == command).ToList();
            if (configured.Count > 0) {
                foreach (var row in configured) {
                    string chord = Text(row, "key");
                    if (String.IsNullOrWhiteSpace(chord)) continue;
                    try { ShortcutKeys.Parse(chord); } catch (ArgumentException) { continue; }
                    if (rows.Any(x => Text(x, "command") != command && SameShortcut(Text(x, "key"), chord)))
                        throw new InvalidOperationException("Codex 快捷键同时绑定了其他命令，请先解决冲突：" + chord);
                    return chord;
                }
                throw new InvalidOperationException("Codex 的 " + command + " 已禁用或使用不兼容快捷键；请为它设置修饰键＋F键或左右方向键。");
            }
            if (fallback == null) throw new InvalidOperationException("请先在 Dialvia“启动与集成”配置听写与发送快捷键。");
            if (rows.Any(x => SameShortcut(Text(x, "key"), fallback)))
                throw new InvalidOperationException("Codex 的默认命令快捷键已被其他命令占用：" + fallback);
            return fallback;
        }
        static bool SameShortcut(string first, string second) {
            try { return ShortcutKeys.Parse(first).OrderBy(x => x).SequenceEqual(ShortcutKeys.Parse(second).OrderBy(x => x)); }
            catch (ArgumentException) { return false; }
        }
        public static void InstallBindings(string codexHome, string data, Settings settings) {
            SettingsStore.Validate(settings);
            string path = Path.Combine(codexHome, "keybindings.json"), manifest = Path.Combine(data, "bindings-owned.json");
            var rows = File.Exists(path) ? Rows(SettingsStore.Json.DeserializeObject(File.ReadAllText(path))) : new List<Dictionary<string, object>>();
            var owned = File.Exists(manifest) ? Rows(SettingsStore.Json.DeserializeObject(File.ReadAllText(manifest))) : new List<Dictionary<string, object>>();
            var additions = new List<Dictionary<string, object>>();
            foreach (var item in Commands) {
                string command = item.Value, chord = item.Key == "send" ? settings.sendShortcut : settings.shortcuts[item.Key];
                var existing = rows.Where(x => Text(x, "command") == command).ToList();
                if (existing.Count > 0) {
                    if (!existing.Any(x => String.Equals(Text(x, "key"), chord, StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException("保留已有快捷键：" + command + "。请先使 Dialvia 配置与它一致。");
                    continue;
                }
                if (rows.Any(x => String.Equals(Text(x, "key"), chord, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("快捷键已被其他命令使用：" + chord);
                additions.Add(new Dictionary<string, object> { {"command", command}, {"key", chord} });
            }
            if (additions.Count == 0) return;
            Directory.CreateDirectory(data);
            string snapshot = Path.Combine(data, "keybindings.before-dialvia.json");
            if (!File.Exists(snapshot)) AtomicFile.Write(snapshot, SettingsStore.Json.Serialize(rows));
            // Persist ownership first so an interrupted installation can still be cleaned up.
            owned.AddRange(additions); AtomicFile.Write(manifest, SettingsStore.Json.Serialize(owned));
            rows.AddRange(additions); AtomicFile.Write(path, SettingsStore.Json.Serialize(rows));
        }
        public static void RemoveBindings(string codexHome, string data) {
            string path = Path.Combine(codexHome, "keybindings.json"), manifest = Path.Combine(data, "bindings-owned.json");
            if (!File.Exists(path) || !File.Exists(manifest)) return;
            var rows = Rows(SettingsStore.Json.DeserializeObject(File.ReadAllText(path)));
            var owned = Rows(SettingsStore.Json.DeserializeObject(File.ReadAllText(manifest)));
            rows.RemoveAll(x => owned.Any(y => Text(x, "command") == Text(y, "command") && Text(x, "key") == Text(y, "key")));
            AtomicFile.Write(path, SettingsStore.Json.Serialize(rows)); AtomicFile.Write(manifest, "[]");
        }
        public static string HookCommand(string executable) { return "\"" + Path.GetFullPath(executable) + "\" --notify"; }
        public static void InstallHook(string codexHome, string executable) {
            string path = Path.Combine(codexHome, "hooks.json");
            var state = File.Exists(path) ? SettingsStore.Json.DeserializeObject(File.ReadAllText(path)) as Dictionary<string, object> : new Dictionary<string, object>();
            if (state == null) throw new InvalidDataException("hooks.json must be an object.");
            object value;
            var hooks = state.TryGetValue("hooks", out value) ? value as Dictionary<string, object> : new Dictionary<string, object>();
            if (hooks == null) throw new InvalidDataException("hooks must be an object.");
            var groups = hooks.TryGetValue("Stop", out value) ? Rows(value) : new List<Dictionary<string, object>>();
            string command = HookCommand(executable);
            foreach (var group in groups) if (group.TryGetValue("hooks", out value) && Rows(value).Any(x => Text(x, "command") == command)) return;
            var handler = new Dictionary<string, object> { {"type", "command"}, {"command", command}, {"timeout", 5}, {"async", true}, {"statusMessage", "Dialvia task notification"} };
            groups.Add(new Dictionary<string, object> { {"hooks", new[] {handler}} }); hooks["Stop"] = groups; state["hooks"] = hooks;
            if (File.Exists(path) && !File.Exists(path + ".before-dialvia")) File.Copy(path, path + ".before-dialvia");
            AtomicFile.Write(path, SettingsStore.Json.Serialize(state));
        }
        public static void RemoveHook(string codexHome, string executable) {
            string path = Path.Combine(codexHome, "hooks.json"); if (!File.Exists(path)) return;
            var state = SettingsStore.Json.DeserializeObject(File.ReadAllText(path)) as Dictionary<string, object>;
            object value; if (state == null || !state.TryGetValue("hooks", out value)) return;
            var hooks = value as Dictionary<string, object>; if (hooks == null || !hooks.TryGetValue("Stop", out value)) return;
            var groups = Rows(value); string command = HookCommand(executable);
            foreach (var group in groups) if (group.TryGetValue("hooks", out value)) {
                var handlers = Rows(value); handlers.RemoveAll(x => Text(x, "command") == command); group["hooks"] = handlers;
            }
            groups.RemoveAll(x => x.ContainsKey("hooks") && Rows(x["hooks"]).Count == 0);
            hooks["Stop"] = groups; AtomicFile.Write(path, SettingsStore.Json.Serialize(state));
        }
    }
    public static class DraftSubmission {
        public static bool Request(ICodexDesktop codex, string codexHome, bool recording, bool detection, out string message) {
            if (recording) { message = "请先按绿色键或 *0＋绿键结束听写，等文字出现后再按 00＋绿键发送。"; return false; }
            try {
                string chord = UserIntegration.SubmitShortcut(codexHome);
                if (detection) { message = "检测成功：发送当前草稿 → " + chord; return false; }
                string error;
                if (!codex.Shortcut(chord, out error)) { message = error; return false; }
                message = "已触发 Codex 的发送草稿操作，请查看会话。"; return true;
            } catch (Exception error) { message = error.Message; return false; }
        }
    }
}
