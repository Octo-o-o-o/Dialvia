using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

class ImportPlantronics : ITypeLibImporterNotifySink {
    [DllImport("oleaut32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    static extern void LoadTypeLibEx(string path, int kind, [MarshalAs(UnmanagedType.Interface)] out object library);
    public void ReportEvent(ImporterEventKind kind, int code, string text) { Console.WriteLine(text); }
    public Assembly ResolveRef(object value) { throw new NotSupportedException("Unexpected referenced type library."); }
    static void Main(string[] args) {
        if (args.Length != 1) throw new ArgumentException("Pass the installed PlantronicsCOM.dll path.");
        object library; LoadTypeLibEx(args[0], 2, out library);
        var converter = new TypeLibConverter();
        var assembly = converter.ConvertTypeLibToAssembly(library, "Interop.Plantronics.dll", TypeLibImporterFlags.None, new ImportPlantronics(), null, null, "Interop.Plantronics", null);
        assembly.Save("Interop.Plantronics.dll");
    }
}
