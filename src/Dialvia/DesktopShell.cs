using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;

namespace Dialvia {
    public static class DesktopShell {
        public const string AppId = "Dialvia.Desktop";
        [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern int SetCurrentProcessExplicitAppUserModelID(string id);
        [DllImport("shell32.dll")] static extern int GetCurrentProcessExplicitAppUserModelID(out IntPtr id);
        [DllImport("ole32.dll")] static extern int PropVariantClear(ref PropVariant value);
        [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern void SHChangeNotify(uint change,uint flags,string first,IntPtr second);
        [StructLayout(LayoutKind.Sequential,Pack=4)] struct PropertyKey {
            public Guid Format; public uint Id;
            public PropertyKey(uint id) {Format=new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");Id=id;}
        }
        [StructLayout(LayoutKind.Explicit,Size=24)] struct PropVariant {
            [FieldOffset(0)] public ushort Type;
            [FieldOffset(8)] public IntPtr Pointer;
        }
        [ComImport,Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IPropertyStore {
            [PreserveSig] int GetCount(out uint count);
            [PreserveSig] int GetAt(uint index,out PropertyKey key);
            [PreserveSig] int GetValue(ref PropertyKey key,out PropVariant value);
            [PreserveSig] int SetValue(ref PropertyKey key,ref PropVariant value);
            [PreserveSig] int Commit();
        }
        public static void InitializeProcess(bool preview) {Marshal.ThrowExceptionForHR(SetCurrentProcessExplicitAppUserModelID(preview?AppId+".Preview":AppId));}
        public static string ProcessAppId {
            get {IntPtr value;Marshal.ThrowExceptionForHR(GetCurrentProcessExplicitAppUserModelID(out value));try{return Marshal.PtrToStringUni(value);}finally{Marshal.FreeCoTaskMem(value);}}
        }
        public static string ShortcutPath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"Dialvia.lnk");}}
        public static byte[] IconBytes {
            get {using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Dialvia.AppIcon"))using(var copy=new MemoryStream()){stream.CopyTo(copy);return copy.ToArray();}}
        }
        static string Hash(byte[] bytes) {using(var algorithm=SHA256.Create())return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        public static string IconPath(string data) {return Path.Combine(data,"icons","dialvia-"+Hash(IconBytes)+".ico");}
        public static string EnsureIcon(string data) {
            string path=IconPath(data);byte[] bytes=IconBytes;Directory.CreateDirectory(Path.GetDirectoryName(path));
            if(!File.Exists(path) || Hash(File.ReadAllBytes(path))!=Hash(bytes)) {File.WriteAllBytes(path,bytes);SHChangeNotify(0x00000002,0x0005,path,IntPtr.Zero);}
            return path;
        }
        public static void ConfigureShortcut(string path,string executable,string directory,string icon) {
            object shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")),shortcut=null;
            try {
                shortcut=shell.GetType().InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});
                foreach(var item in new[] {new[]{"TargetPath",executable},new[]{"WorkingDirectory",directory},new[]{"Arguments","--settings"},new[]{"IconLocation",icon+",0"}})
                    shortcut.GetType().InvokeMember(item[0],BindingFlags.SetProperty,null,shortcut,new object[]{item[1]});
                shortcut.GetType().InvokeMember("Save",BindingFlags.InvokeMethod,null,shortcut,null);
            }finally{if(shortcut!=null)Marshal.FinalReleaseComObject(shortcut);Marshal.FinalReleaseComObject(shell);}
            object link=Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("00021401-0000-0000-C000-000000000046")));
            try {
                var file=(IPersistFile)link;file.Load(path,2);var store=(IPropertyStore)link;var key=new PropertyKey(5);
                var value=new PropVariant {Type=31,Pointer=Marshal.StringToCoTaskMemUni(AppId)};
                try{Marshal.ThrowExceptionForHR(store.SetValue(ref key,ref value));Marshal.ThrowExceptionForHR(store.Commit());file.Save(path,true);}finally{Marshal.FreeCoTaskMem(value.Pointer);}
            }finally{Marshal.FinalReleaseComObject(link);}
            SHChangeNotify(0x00002000,0x0005,path,IntPtr.Zero);
        }
        public static string ReadShortcutAppId(string path) {
            object link=Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("00021401-0000-0000-C000-000000000046")));
            try {
                ((IPersistFile)link).Load(path,0);var key=new PropertyKey(5);PropVariant value;
                Marshal.ThrowExceptionForHR(((IPropertyStore)link).GetValue(ref key,out value));
                try{return value.Type==31?Marshal.PtrToStringUni(value.Pointer):"";}finally{PropVariantClear(ref value);}
            }finally{Marshal.FinalReleaseComObject(link);}
        }
        public static object InspectInstalled() {
            string path=ShortcutPath,icon=IconPath(AppPaths.Data),actual="",target="",arguments="";
            object shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")),shortcut=null;
            try {
                shortcut=shell.GetType().InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});
                actual=Convert.ToString(shortcut.GetType().InvokeMember("IconLocation",BindingFlags.GetProperty,null,shortcut,null));
                target=Convert.ToString(shortcut.GetType().InvokeMember("TargetPath",BindingFlags.GetProperty,null,shortcut,null));
                arguments=Convert.ToString(shortcut.GetType().InvokeMember("Arguments",BindingFlags.GetProperty,null,shortcut,null));
            }finally{if(shortcut!=null)Marshal.FinalReleaseComObject(shortcut);Marshal.FinalReleaseComObject(shell);}
            string shortcutId=File.Exists(path)?ReadShortcutAppId(path):"";
            bool iconMatches=File.Exists(icon) && Hash(File.ReadAllBytes(icon))==Hash(IconBytes);
            bool passed=ProcessAppId==AppId && shortcutId==AppId && String.Equals(actual,icon+",0",StringComparison.OrdinalIgnoreCase) && String.Equals(target,Path.Combine(AppPaths.Data,"Dialvia.exe"),StringComparison.OrdinalIgnoreCase) && arguments=="--settings" && iconMatches;
            return new {passed=passed,version=Program.Version,processAppId=ProcessAppId,shortcutAppId=shortcutId,iconPath=icon,shortcutIcon=actual,target=target,arguments=arguments,iconMatchesEmbedded=iconMatches};
        }
    }
}
