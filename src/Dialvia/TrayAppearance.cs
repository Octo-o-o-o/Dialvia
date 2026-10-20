using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Dialvia {
    public static class TrayAppearance {
        [DllImport("user32.dll")] static extern uint GetDpiForSystem();
        [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern IntPtr FindWindow(string className, string windowName);
        [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr window);
        [DllImport("user32.dll")] static extern int GetSystemMetricsForDpi(int metric, uint dpi);
        public static int IconSize {
            get {
                int size = SystemInformation.SmallIconSize.Width;
                try {
                    IntPtr taskbar = FindWindow("Shell_TrayWnd", null);
                    uint dpi = taskbar == IntPtr.Zero ? GetDpiForSystem() : GetDpiForWindow(taskbar);
                    size = GetSystemMetricsForDpi(49, dpi == 0 ? GetDpiForSystem() : dpi);
                } catch (EntryPointNotFoundException) { }
                return Math.Max(16, Math.Min(64, size));
            }
        }
        public static bool Dark {
            get {
                if (SystemInformation.HighContrast) return SystemColors.WindowText.GetBrightness() > 0.5f;
                try {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false)) {
                        object value = key == null ? null : key.GetValue("SystemUsesLightTheme");
                        if (value is int) return (int)value == 0;
                    }
                } catch (Exception) { }
                return SystemColors.Control.GetBrightness() < 0.5f;
            }
        }
        public static Icon CreateIcon() { return CreateIcon(Dark, IconSize); }
        public static Icon CreateIcon(bool dark, int size) { return Program.LoadIcon(dark ? "Dialvia.TrayDarkIcon" : "Dialvia.TrayLightIcon", size); }
    }
}
