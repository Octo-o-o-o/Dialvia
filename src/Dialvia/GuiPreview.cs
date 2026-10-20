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
            var cases = new List<object>();
            using (var form = new SettingsWindow(settings, delegate {}, delegate { return "P240 已连接 · 检测模式"; }, delegate {}, delegate {}, delegate {}, DemoThreads, true)) {
                form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-25000, -25000); form.Show();
                string[] names = {"overview", "projects", "voice", "startup"};
                for (int index = 0; index < names.Length; index++) {
                    form.SelectPage(index); form.PerformLayout(); Application.DoEvents();
                    Capture(form, destination, names[index], cases);
                }
                form.Size = form.MinimumSize;
                for (int index = 0; index < names.Length; index++) { form.SelectPage(index); Capture(form, destination, names[index]+"-compact", cases); }
                form.Close();
                List<RecentThread> demo = DemoThreads();
                foreach (bool project in new[] {true,false}) using (var editor = new BindingEditor(project, new Dictionary<string, Target>(), null, null, demo, demo[0])) {
                    editor.StartPosition=FormStartPosition.Manual; editor.Location=new Point(-25000,-25000); editor.Show();
                    Capture(editor,destination,project?"binding-project":"binding-session",cases); editor.Close();
                }
                using (var empty = new SessionBrowserWindow("", delegate { return new List<RecentThread>(); }, delegate {})) {
                    empty.StartPosition=FormStartPosition.Manual; empty.Location=new Point(-25000,-25000); empty.Show(); Capture(empty,destination,"empty-sessions",cases); empty.Close();
                }
                using (var error = new SessionBrowserWindow("", delegate { throw new InvalidDataException("Preview fixture: index unavailable"); }, delegate {})) {
                    error.StartPosition=FormStartPosition.Manual; error.Location=new Point(-25000,-25000); error.Show(); Capture(error,destination,"error-sessions",cases); error.Close();
                }
            }
            using (var form = new SessionBrowserWindow(@"C:\Demo\Dialvia", DemoThreads, delegate {})) {
                form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-25000, -25000); form.Show(); form.PerformLayout(); Application.DoEvents();
                Capture(form,destination,"recent-sessions",cases);
                form.Close();
            }
            File.WriteAllText(Path.Combine(destination,"ui-audit.json"),SettingsStore.Json.Serialize(new {version=Program.Version,cases=cases}));
        }
        static void Capture(Form form, string destination, string name, List<object> cases) {
            form.PerformLayout(); Application.DoEvents();
            using (var bitmap = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bitmap, new Rectangle(0,0,form.Width,form.Height)); bitmap.Save(Path.Combine(destination,name+".png"),ImageFormat.Png); }
            var missingNames=new List<string>(); var overflows=new List<string>();
            Inspect(form,missingNames,overflows);
            using(Graphics graphics=Graphics.FromHwnd(form.Handle)) cases.Add(new {name=name,width=form.Width,height=form.Height,graphicsDpi=graphics.DpiX,missingInputNames=missingNames,horizontalOverflows=overflows});
        }
        static void Inspect(Control control,List<string> missing,List<string> overflows) {
            if (!control.Visible) return;
            if ((control is ComboBox || control is TextBox || control is ListView) && String.IsNullOrWhiteSpace(control.AccessibleName)) missing.Add(control.GetType().Name);
            var flow=control.Parent as FlowLayoutPanel;
            if(flow!=null && !flow.AutoScroll && control.Right>flow.ClientSize.Width-flow.Padding.Right+3) overflows.Add(control.GetType().Name+":"+control.Text);
            foreach(Control child in control.Controls) Inspect(child,missing,overflows);
        }
        public static void Interactive(string page) {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Form form;
            if(page=="settings") {
                var settings=new Settings(); settings.projects["1"]=new Target {label="Dialvia",url=DemoThreads()[0].Url,projectPath=@"C:\Demo\Dialvia"};
                form=new SettingsWindow(settings,delegate {},delegate {return "P240 示例 · 检测模式";},delegate {},delegate {},delegate {},DemoThreads,true);
            } else if(page=="sessions") form=new SessionBrowserWindow("",DemoThreads,delegate {});
            else throw new ArgumentException("Use settings or sessions for --preview-ui.");
            using(form) { form.Text += " · 示例预览"; Application.Run(form); }
        }
    }
}
