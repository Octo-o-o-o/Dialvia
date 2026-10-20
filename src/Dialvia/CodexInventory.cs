using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Dialvia {
    public sealed class RecentThread {
        public string Id, Title, ProjectPath;
        public long UpdatedAt;
        public string Url { get { return "codex://threads/" + Id; } }
        public override string ToString() { return Title + "  ·  " + Path.GetFileName((ProjectPath ?? "").TrimEnd('\\', '/')); }
        public string UpdatedLabel {
            get {
                DateTime value; try { value = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(UpdatedAt).ToLocalTime(); }
                catch { return "时间未知"; }
                if (value.Date == DateTime.Today) return "今天 " + value.ToString("HH:mm");
                if (value.Date == DateTime.Today.AddDays(-1)) return "昨天 " + value.ToString("HH:mm");
                return value.ToString("MM-dd HH:mm");
            }
        }
    }
    public static class CodexInventory {
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] static extern int sqlite3_open_v2(byte[] path, out IntPtr database, int flags, IntPtr vfs);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] static extern int sqlite3_close(IntPtr database);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] static extern int sqlite3_busy_timeout(IntPtr database, int milliseconds);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] static extern int sqlite3_prepare_v2(IntPtr database, byte[] sql, int length, out IntPtr statement, IntPtr tail);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] static extern int sqlite3_step(IntPtr statement);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] static extern int sqlite3_finalize(IntPtr statement);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] static extern IntPtr sqlite3_column_text16(IntPtr statement, int column);
        static byte[] Utf8(string text) { return Encoding.UTF8.GetBytes(text + "\0"); }
        static List<string[]> Query(IntPtr database, string sql, int columns) {
            IntPtr statement = IntPtr.Zero;
            try {
                if (sqlite3_prepare_v2(database, Utf8(sql), -1, out statement, IntPtr.Zero) != 0) throw new InvalidDataException("本机 Codex 会话索引格式已变化，可使用手动会话链接。");
                var rows = new List<string[]>(); int result;
                while ((result = sqlite3_step(statement)) == 100) {
                    var row = new string[columns];
                    for (int column = 0; column < columns; column++) row[column] = Marshal.PtrToStringUni(sqlite3_column_text16(statement, column)) ?? "";
                    rows.Add(row);
                }
                if (result != 101) throw new IOException("会话索引暂时忙，请稍后刷新。"); return rows;
            } finally { if (statement != IntPtr.Zero) sqlite3_finalize(statement); }
        }
        public static List<RecentThread> Load(string codexHome) {
            string path = Path.Combine(codexHome, "state_5.sqlite");
            if (!File.Exists(path)) return new List<RecentThread>();
            IntPtr database = IntPtr.Zero;
            try {
                // READONLY: no database creation, migration, writes or transcript access.
                if (sqlite3_open_v2(Utf8(path), out database, 1, IntPtr.Zero) != 0) throw new IOException("无法只读打开本机 Codex 会话索引。");
                sqlite3_busy_timeout(database, 150);
                var columns = Query(database, "PRAGMA table_info(threads)", 6).Select(x => x[1]).ToList();
                if (!new[] {"id", "title", "cwd", "updated_at"}.All(columns.Contains))
                    throw new InvalidDataException("此版本的 Codex 索引暂不兼容，请手动粘贴会话链接。");
                string sql = "SELECT id,title,cwd,updated_at FROM threads" + (columns.Contains("archived") ? " WHERE archived=0" : "") + " ORDER BY updated_at DESC,id LIMIT 200";
                var threads = new List<RecentThread>();
                foreach (string[] row in Query(database, sql, 4)) {
                    Guid id; long updated;
                    if (!Guid.TryParseExact(row[0], "D", out id) || !Int64.TryParse(row[3], out updated)) continue;
                    threads.Add(new RecentThread { Id = row[0], Title = String.IsNullOrWhiteSpace(row[1]) ? "未命名会话" : row[1], ProjectPath = row[2], UpdatedAt = updated });
                }
                return threads;
            } finally { if (database != IntPtr.Zero) sqlite3_close(database); }
        }
    }
    public sealed class SessionSnapshot {
        readonly List<RecentThread> threads;
        public IList<RecentThread> Threads { get { return threads.AsReadOnly(); } }
        public SessionSnapshot(IEnumerable<RecentThread> inventory, string project) {
            threads = inventory.Where(x => InProject(x.ProjectPath, project)).OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
        }
        public static bool InProject(string cwd, string project) {
            if (String.IsNullOrWhiteSpace(project)) return true;
            string root = project.TrimEnd('\\', '/'); string path = (cwd ?? "").TrimEnd('\\', '/');
            return String.Equals(path, root, StringComparison.OrdinalIgnoreCase) || path.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase) || path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase);
        }
        public RecentThread Slot(int number) { return number > 0 && number <= 9 && number <= threads.Count ? threads[number - 1] : null; }
    }
}
