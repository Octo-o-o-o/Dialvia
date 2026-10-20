using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Dialvia")]
[assembly: AssemblyDescription("Physical controls for Codex on Windows. P240 is the first supported adapter.")]
[assembly: AssemblyCompany("Dialvia contributors")]
[assembly: AssemblyProduct("Dialvia")]
[assembly: AssemblyVersion("0.1.0.0")]
[assembly: AssemblyFileVersion("0.1.0.0")]
[assembly: AssemblyInformationalVersion("0.1.0-alpha.1")]

namespace Dialvia {
    public static class Program {
        static Mutex instance;
        static Icon icon;
        static Bitmap brandImage;
        public static Bitmap BrandImage {
            get { if (brandImage == null) using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Dialvia.BrandImage"))
                using (Image value = Image.FromStream(stream)) brandImage = new Bitmap(value); return brandImage; }
        }
        public static string Executable { get { return Assembly.GetExecutingAssembly().Location; } }
        public static Icon AppIcon {
            get {
                if (icon == null) using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Dialvia.AppIcon"))
                    using (var value = new Icon(stream, 32, 32)) icon = (Icon)value.Clone();
                return icon;
            }
        }
        [STAThread] public static int Main(string[] args) {
            try { return Run(args); }
            catch (Exception ex) {
                AppPaths.Log("FATAL " + ex.Message);
                if (args.Length == 2 && (args[0] == "--render-preview" || args[0] == "--install-report")) { Directory.CreateDirectory(args[0] == "--render-preview" ? args[1] : Path.GetDirectoryName(Path.GetFullPath(args[1]))); File.WriteAllText(args[0] == "--render-preview" ? Path.Combine(args[1], "error.txt") : args[1], SettingsStore.Json.Serialize(new { error = ex.Message })); return 1; }
                if (Array.IndexOf(args, "--notify") < 0) MessageBox.Show(ex.Message, "Dialvia"); return 1;
            }
        }
        [MethodImpl(MethodImplOptions.NoInlining)] static int Run(string[] args) {
            if (args.Length > 0 && args[0] == "--notify") return Notifications.FromHook();
            if (args.Length == 2 && args[0] == "--render-preview") { GuiPreview.Render(Path.GetFullPath(args[1])); return 0; }
            if (args.Length == 2 && args[0] == "--inventory-check") {
                var threads = CodexInventory.Load(AppPaths.CodexHome);
                AtomicFile.Write(Path.GetFullPath(args[1]), SettingsStore.Json.Serialize(new { threads = threads.Count, readOnly = true })); return 0;
            }
            if (args.Length > 0 && args[0] == "--self-test") {
                if (args.Length != 2) throw new ArgumentException("Usage: Dialvia.exe --self-test <report.json>");
                var settings = new Settings(); SettingsStore.Validate(settings);
                if (CodexDesktop.NativeInputSize != 40 || DialCode.Kind("*2") != "project" || DialCode.Kind("1;calc") != "unknown") throw new Exception("Self-test failed.");
                AtomicFile.Write(Path.GetFullPath(args[1]), "{\"passed\":true,\"inputSize\":40,\"version\":\"0.1.0-alpha.1\"}"); return 0;
            }
            if (args.Length > 0 && args[0] == "--probe-device") {
                if (args.Length != 2) throw new ArgumentException("Usage: Dialvia.exe --probe-device <report.json>");
                return ProbeDevice(Path.GetFullPath(args[1]));
            }
            AppPaths.Initialize();
            if (args.Length > 0) switch (args[0]) {
                case "--install": Installer.Install(); MessageBox.Show("Installed: " + AppPaths.Data + "\nStart Dialvia from the Start menu.", "Dialvia"); return 0;
                case "--install-report":
                    if (args.Length != 2) throw new ArgumentException("Usage: Dialvia.exe --install-report <report.json>");
                    Installer.Install(); AtomicFile.Write(Path.GetFullPath(args[1]), SettingsStore.Json.Serialize(new { installed = true, data = AppPaths.Data, autoStart = Installer.AutoStartEnabled })); return 0;
                case "--uninstall": Installer.Uninstall(); MessageBox.Show("Startup and owned integrations removed. Config and Hub retained.", "Dialvia"); return 0;
                case "--settings": AtomicFile.Write(Path.Combine(AppPaths.Data, "settings.request"), ""); break;
                case "--stop": AtomicFile.Write(Path.Combine(AppPaths.Data, "stop.request"), ""); return 0;
                case "--test-notification": Notifications.Enqueue("Dialvia 通知测试"); return 0;
                default: throw new ArgumentException("Options: --install, --uninstall, --settings, --stop, --notify, --test-notification, --self-test <file>, --probe-device <file>");
            }
            bool created; instance = new Mutex(true, @"Local\Dialvia", out created); if (!created) return 0;
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ControllerForm(new CodexDesktop())); GC.KeepAlive(instance); return 0;
        }
        [MethodImpl(MethodImplOptions.NoInlining)] static int ProbeDevice(string report) {
            Application.EnableVisualStyles();
            using (var owner = new Form()) using (var adapter = new PlantronicsP240Adapter(owner, false)) {
                IntPtr handle = owner.Handle; adapter.Connect();
                AtomicFile.Write(report, SettingsStore.Json.Serialize(new { connected = adapter.Connected, product = adapter.ProductName, changesDefaultSoftphone = false }));
                return adapter.Connected ? 0 : 2;
            }
        }
    }
    public static class Notifications {
        public static void Enqueue(string message) {
            Directory.CreateDirectory(AppPaths.Inbox);
            string path = Path.Combine(AppPaths.Inbox, Guid.NewGuid().ToString("N") + ".json");
            AtomicFile.Write(path, SettingsStore.Json.Serialize(new { message = message }));
        }
        public static int FromHook() {
            try {
                var input = new StringBuilder(); var buffer = new char[4096]; int count;
                while ((count = Console.In.Read(buffer, 0, buffer.Length)) > 0) {
                    if (input.Length + count > 1048576) return 0; input.Append(buffer, 0, count);
                }
                var data = SettingsStore.Json.Deserialize<System.Collections.Generic.Dictionary<string, object>>(input.ToString()); object active;
                if (data != null && data.TryGetValue("stop_hook_active", out active) && Object.Equals(active, true)) return 0;
                Enqueue("Codex 已完成回复");
            } catch { }
            finally { Console.Out.WriteLine("{}"); }
            return 0;
        }
    }
    public static class Installer {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        static string StartupCommand { get { return "\"" + Path.Combine(AppPaths.Data, "Dialvia.exe") + "\""; } }
        public static bool AutoStartEnabled {
            get { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey)) return key != null && String.Equals(Convert.ToString(key.GetValue("Dialvia")), StartupCommand, StringComparison.OrdinalIgnoreCase); }
        }
        public static void SetAutoStart(bool enabled) {
            if (enabled) { Install(); return; }
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true)) if (key != null && String.Equals(Convert.ToString(key.GetValue("Dialvia")), StartupCommand, StringComparison.OrdinalIgnoreCase)) key.DeleteValue("Dialvia", false);
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern int GetCurrentPackageFullName(ref int length, StringBuilder name);
        public static void Install() {
            int length = 0;
            if (GetCurrentPackageFullName(ref length, null) != 15700)
                throw new InvalidOperationException("请从 Windows 资源管理器启动 Dialvia 后安装，避免 MSIX 应用目录虚拟化。");
            AppPaths.Initialize(); string destination = Path.Combine(AppPaths.Data, "Dialvia.exe");
            string interop = Path.Combine(Path.GetDirectoryName(Program.Executable), "Interop.Plantronics.dll");
            if (!File.Exists(interop)) throw new FileNotFoundException("请保留 ZIP 中的 Interop.Plantronics.dll，与 Dialvia.exe 放在一起。");
            if (!String.Equals(Program.Executable, destination, StringComparison.OrdinalIgnoreCase)) File.Copy(Program.Executable, destination, true);
            string installedInterop = Path.Combine(AppPaths.Data, "Interop.Plantronics.dll");
            if (!String.Equals(interop, installedInterop, StringComparison.OrdinalIgnoreCase)) File.Copy(interop, installedInterop, true);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey)) key.SetValue("Dialvia", "\"" + destination + "\"");
            string shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Dialvia.lnk");
            object shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
            object shortcut = shell.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] {shortcutPath});
            shortcut.GetType().InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] {destination});
            shortcut.GetType().InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] {AppPaths.Data});
            shortcut.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            Marshal.FinalReleaseComObject(shortcut); Marshal.FinalReleaseComObject(shell);
        }
        public static void Uninstall() {
            AtomicFile.Write(Path.Combine(AppPaths.Data, "stop.request"), "");
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true)) {
                if (key != null && String.Equals(Convert.ToString(key.GetValue("Dialvia")), "\"" + Path.Combine(AppPaths.Data, "Dialvia.exe") + "\"", StringComparison.OrdinalIgnoreCase)) key.DeleteValue("Dialvia", false);
            }
            UserIntegration.RemoveBindings(AppPaths.CodexHome, AppPaths.Data);
            UserIntegration.RemoveHook(AppPaths.CodexHome, Program.Executable);
            UserIntegration.RemoveHook(AppPaths.CodexHome, Path.Combine(AppPaths.Data, "Dialvia.exe"));
            string shortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Dialvia.lnk");
            if (File.Exists(shortcut)) File.Delete(shortcut);
        }
    }
}
