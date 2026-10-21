using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;

class WindowsIconTests {
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern uint ExtractIconEx(string file,int index,out IntPtr large,out IntPtr small,uint count);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
    [DllImport("user32.dll")] static extern bool DrawIconEx(IntPtr dc,int x,int y,IntPtr icon,int width,int height,uint step,IntPtr brush,uint flags);
    static readonly Color Ink=ColorTranslator.FromHtml("#202320"), Copper=ColorTranslator.FromHtml("#B76546");
    static bool Near(Color a,Color b) {return Math.Abs(a.R-b.R)<=8 && Math.Abs(a.G-b.G)<=8 && Math.Abs(a.B-b.B)<=8;}
    static object Render(IntPtr handle,int size,string name,string output,Color primary,Color accent,Color background) {
        using(var bitmap=new Bitmap(size,size,PixelFormat.Format32bppArgb)) {
            using(var graphics=Graphics.FromImage(bitmap)) {
                graphics.Clear(background);IntPtr dc=graphics.GetHdc();
                try {if(!DrawIconEx(dc,0,0,handle,size,size,0,IntPtr.Zero,3))throw new Exception("Native icon drawing failed: "+name);}finally{graphics.ReleaseHdc(dc);}
            }
            int ink=0,copper=0;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){Color pixel=bitmap.GetPixel(x,y);if(Near(pixel,primary))ink++;if(Near(pixel,accent))copper++;}
            bitmap.Save(Path.Combine(output,name+".png"),ImageFormat.Png);
            if(ink<size*size*0.15 || copper<size*size*0.04)throw new Exception("Missing two-tone brand palette in "+name+" (primary="+ink+", accent="+copper+")");
            if(!Near(bitmap.GetPixel(0,0),background))throw new Exception("Unexpected icon background in "+name);
            return new {name=name,size=size,primaryPixels=ink,accentPixels=copper};
        }
    }
    static int Main(string[] args) {
        try {
            string file=Path.GetFullPath(args[0]),output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
            var records=new List<object>();IntPtr large,small;
            if(ExtractIconEx(file,0,out large,out small,1)==0)throw new Exception("EXE has no native shell icon");
            try {
                using(var icon=Icon.FromHandle(large))records.Add(Render(large,icon.Width,"shell-large",output,Ink,Copper,Color.White));
                using(var icon=Icon.FromHandle(small))records.Add(Render(small,icon.Width,"shell-small",output,Ink,Copper,Color.White));
            }finally{if(large!=IntPtr.Zero)DestroyIcon(large);if(small!=IntPtr.Zero)DestroyIcon(small);}
            Assembly assembly=Assembly.LoadFile(file);
            foreach(int size in new[] {16,24,36,48})using(Stream stream=assembly.GetManifestResourceStream("Dialvia.AppIcon"))using(var icon=new Icon(stream,size,size))
                records.Add(Render(icon.Handle,size,"window-"+size,output,Ink,Copper,Color.White));
            using(Stream stream=assembly.GetManifestResourceStream("Dialvia.TrayLightIcon"))using(var icon=new Icon(stream,24,24))
                records.Add(Render(icon.Handle,24,"tray-light",output,Ink,Copper,Color.White));
            using(Stream stream=assembly.GetManifestResourceStream("Dialvia.TrayDarkIcon"))using(var icon=new Icon(stream,24,24))
                records.Add(Render(icon.Handle,24,"tray-dark",output,ColorTranslator.FromHtml("#F7F5F0"),ColorTranslator.FromHtml("#D28B67"),ColorTranslator.FromHtml("#202020")));
            File.WriteAllText(Path.Combine(output,"result.json"),new JavaScriptSerializer().Serialize(new {passed=true,version=assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion,renders=records}));
            Console.WriteLine("Validated 8 native Windows icon renders from EXE shell and embedded window/tray resources.");return 0;
        }catch(Exception error){Console.Error.WriteLine(error.GetType().FullName+": "+error.Message);return 1;}
    }
}
