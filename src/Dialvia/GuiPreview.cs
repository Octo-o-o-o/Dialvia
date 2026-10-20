using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Dialvia {
    public static class GuiPreview {
        public static List<RecentThread> DemoThreads() {
            long now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            return new List<RecentThread> {
                new RecentThread {Id="11111111-1111-4111-8111-111111111111",Title="检查 Windows 版本的交互细节",ProjectPath=@"C:\Demo\Dialvia",UpdatedAt=now-120},
                new RecentThread {Id="22222222-2222-4222-8222-222222222222",Title="整理本周的界面设计",ProjectPath=@"C:\Demo\Dialvia",UpdatedAt=now-1800},
                new RecentThread {Id="33333333-3333-4333-8333-333333333333",Title="实现设备状态提示",ProjectPath=@"C:\Demo\ExampleApp",UpdatedAt=now-90000}
            };
        }
        public static void Render(string destination) {
            Directory.CreateDirectory(destination); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            var settings = new Settings {recentProjectPath=@"C:\Demo\Dialvia"};
            settings.projects["1"] = new Target {label="Dialvia",url="codex://threads/11111111-1111-4111-8111-111111111111",projectPath=@"C:\Demo\Dialvia"};
            settings.projects["2"] = new Target {label="Example App",url="codex://threads/33333333-3333-4333-8333-333333333333",projectPath=@"C:\Demo\ExampleApp"};
            using (var form = new SettingsWindow(settings, delegate {}, delegate { return "P240 已连接 · 检测模式"; }, delegate {}, delegate {}, DemoThreads, true)) {
                form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-25000, -25000); form.Show();
                using (Graphics graphics = Graphics.FromHwnd(form.Handle)) File.WriteAllText(Path.Combine(destination, "metrics.json"), SettingsStore.Json.Serialize(new { deviceDpi = form.DeviceDpi, graphicsDpi = graphics.DpiX, font = form.Font.SizeInPoints, scaleWidth = form.CurrentAutoScaleDimensions.Width, width = form.Width, height = form.Height }));
                string[] names = {"overview", "projects", "voice", "startup"};
                for (int index = 0; index < names.Length; index++) {
                    form.SelectPage(index); form.PerformLayout(); Application.DoEvents();
                    using (var bitmap = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height)); bitmap.Save(Path.Combine(destination, names[index]+".png"), ImageFormat.Png); }
                }
                form.Close();
            }
            using (var form = new SessionBrowserWindow(@"C:\Demo\Dialvia", DemoThreads, delegate {})) {
                form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-25000, -25000); form.Show(); form.PerformLayout(); Application.DoEvents();
                using (var bitmap = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height)); bitmap.Save(Path.Combine(destination, "recent-sessions.png"), ImageFormat.Png); }
                form.Close();
            }
        }
    }
}
