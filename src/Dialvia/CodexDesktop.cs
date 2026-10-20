using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Dialvia {
    public static class ShortcutKeys {
        public static ushort[] Parse(string chord) {
            if (String.IsNullOrWhiteSpace(chord)) throw new ArgumentException("Empty shortcut.");
            var keys = new List<ushort>(); bool functionKey = false;
            foreach (string token in chord.Split('+')) {
                ushort key;
                switch (token.ToUpperInvariant()) {
                    case "CTRL": key = 0x11; break; case "ALT": key = 0x12; break; case "SHIFT": key = 0x10; break;
                    case "LEFT": key = 0x25; if (functionKey) throw new ArgumentException("Only one action key is allowed."); functionKey = true; break;
                    case "RIGHT": key = 0x27; if (functionKey) throw new ArgumentException("Only one action key is allowed."); functionKey = true; break;
                    case "L": key = 0x4c; if (functionKey) throw new ArgumentException("Only one action key is allowed."); functionKey = true; break;
                    case "[": key = 0xdb; if (functionKey) throw new ArgumentException("Only one action key is allowed."); functionKey = true; break;
                    case "]": key = 0xdd; if (functionKey) throw new ArgumentException("Only one action key is allowed."); functionKey = true; break;
                    default: int number;
                        if (!token.StartsWith("F", StringComparison.OrdinalIgnoreCase) || !Int32.TryParse(token.Substring(1), out number) || number < 1 || number > 24 || functionKey)
                            throw new ArgumentException("Use Ctrl/Alt/Shift plus one F1–F24, Left/Right, L, [ or ] key.");
                        key = (ushort)(0x6f + number); functionKey = true; break;
                }
                if (keys.Contains(key)) throw new ArgumentException("Duplicate shortcut key."); keys.Add(key);
            }
            if (!functionKey || keys.Count < 2) throw new ArgumentException("Shortcut requires a modifier and one function key.");
            return keys.Where(x => x == 0x10 || x == 0x11 || x == 0x12).Concat(keys.Where(x => x != 0x10 && x != 0x11 && x != 0x12)).ToArray();
        }
    }
    public interface ICodexDesktop { bool Focus(out string error); bool Shortcut(string chord, out string error); void OpenThread(string url); }
    public sealed class CodexDesktop : ICodexDesktop {
        [StructLayout(LayoutKind.Sequential)] struct Input { public uint type; public InputUnion data; }
        [StructLayout(LayoutKind.Explicit)] struct InputUnion { [FieldOffset(0)] public Keyboard keyboard; [FieldOffset(0)] public Mouse mouse; }
        [StructLayout(LayoutKind.Sequential)] struct Keyboard { public ushort vk, scan; public uint flags, time; public UIntPtr extra; }
        [StructLayout(LayoutKind.Sequential)] struct Mouse { public int x, y; public uint data, flags, time; public UIntPtr extra; }
        [DllImport("user32.dll")] static extern uint SendInput(uint count, Input[] inputs, int size);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] static extern bool ShowWindowAsync(IntPtr window, int state);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
        public static int NativeInputSize { get { return Marshal.SizeOf(typeof(Input)); } }
        public void OpenThread(string url) {
            Guid id;
            if (!url.StartsWith("codex://threads/", StringComparison.Ordinal) || !Guid.TryParseExact(url.Substring(16), "D", out id))
                throw new ArgumentException("Only Codex thread links are allowed.");
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        public bool Focus(out string error) {
            error = null; Process target = null;
            foreach (Process process in Process.GetProcessesByName("ChatGPT")) {
                try {
                    if (process.MainWindowHandle != IntPtr.Zero && process.MainModule.FileName.IndexOf("OpenAI.Codex_", StringComparison.OrdinalIgnoreCase) >= 0) { target = process; break; }
                } catch { }
            }
            if (target == null) { error = "请先打开 Codex 窗口。"; return false; }
            // SW_RESTORE also unmaximizes a normal visible window. Restore only when minimized.
            if (IsIconic(target.MainWindowHandle)) ShowWindowAsync(target.MainWindowHandle, 9);
            SetForegroundWindow(target.MainWindowHandle);
            uint pid; GetWindowThreadProcessId(GetForegroundWindow(), out pid);
            if (pid != target.Id) { error = "请先点击 Codex 窗口，再按话机键。"; return false; }
            return true;
        }
        public bool Shortcut(string chord, out string error) {
            ushort[] keys = ShortcutKeys.Parse(chord);
            if (!Focus(out error)) return false;
            foreach (int modifier in new[] {0x10, 0x11, 0x12, 0x5b, 0x5c})
                if ((GetAsyncKeyState(modifier) & 0x8000) != 0) { error = "请松开键盘修饰键后重试。"; return false; }
            var inputs = new List<Input>();
            foreach (ushort key in keys) inputs.Add(new Input { type = 1, data = new InputUnion { keyboard = new Keyboard { vk = key } } });
            for (int i = keys.Length - 1; i >= 0; i--) inputs.Add(new Input { type = 1, data = new InputUnion { keyboard = new Keyboard { vk = keys[i], flags = 2 } } });
            Input[] presses = inputs.Take(keys.Length).ToArray(), releases = inputs.Skip(keys.Length).ToArray();
            uint pressed = 0, released = 0;
            try {
                pressed = SendInput((uint)presses.Length, presses, NativeInputSize);
                // Codex receives WM_HOTKEY and native release events through separate paths.
                // A real hold lets its press callback run before the release is observed.
                if (pressed == presses.Length) Thread.Sleep(80);
            } finally { released = SendInput((uint)releases.Length, releases, NativeInputSize); }
            if (pressed != presses.Length || released != releases.Length) {
                error = "Windows 未完整接收快捷键。"; return false;
            }
            AppPaths.Log("SHORTCUT " + chord); return true;
        }
    }
}
