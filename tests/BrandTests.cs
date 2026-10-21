using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Xml.Linq;

class BrandTests {
    static readonly int[] Sizes = {16,20,24,30,32,36,40,48,60,64,72,80,96,128,256};
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static double Luminance(Color c) { return new[] {c.R,c.G,c.B}.Select(v => {double s=v/255.0;return s<=0.04045?s/12.92:Math.Pow((s+0.055)/1.055,2.4);}).Zip(new[] {0.2126,0.7152,0.0722},(a,b)=>a*b).Sum(); }
    static double Contrast(Color a, Color b) { double x=Luminance(a),y=Luminance(b);return (Math.Max(x,y)+0.05)/(Math.Min(x,y)+0.05); }
    static void Main(string[] args) {
        try { Run(args); } catch(Exception error) { Console.Error.WriteLine(error.GetType().FullName + ": " + error.Message); Environment.ExitCode=1; }
    }
    static void Run(string[] args) {
        string assets=Path.Combine(args[0],"assets","brand"), report=Path.GetFullPath(args[1]);
        Directory.CreateDirectory(Path.GetDirectoryName(report));
        var records=new List<object>();
        foreach(string name in new[] {"dialvia","dialvia-tray-light","dialvia-tray-dark"}) {
            using(var reader=new BinaryReader(File.OpenRead(Path.Combine(assets,name+".ico")))) {
                Require(reader.ReadUInt16()==0 && reader.ReadUInt16()==1,"ICO header");
                Require(reader.ReadUInt16()==Sizes.Length,"ICO frame count");
                for(int index=0;index<Sizes.Length;index++) {
                    int width=reader.ReadByte(),height=reader.ReadByte();width=width==0?256:width;height=height==0?256:height;
                    reader.ReadByte();reader.ReadByte();reader.ReadUInt16();int depth=reader.ReadUInt16();uint length=reader.ReadUInt32(),offset=reader.ReadUInt32();
                    Require(width==Sizes[index] && height==width && depth==32,"Exact native ICO dimensions and depth");
                    long position=reader.BaseStream.Position;reader.BaseStream.Position=offset;byte[] frame=reader.ReadBytes((int)length);reader.BaseStream.Position=position;
                    Require(frame.SequenceEqual(File.ReadAllBytes(Path.Combine(assets,name+"-"+width+".png"))),"ICO frame matches its PNG source");
                    using(var stream=new MemoryStream(frame)) using(var bitmap=new Bitmap(stream)) {
                        Require(bitmap.Width==width && bitmap.Height==width,"PNG dimensions");
                        Require(bitmap.GetPixel(0,0).A==0 && bitmap.GetPixel(width-1,0).A==0 && bitmap.GetPixel(0,width-1).A==0 && bitmap.GetPixel(width-1,width-1).A==0,"Transparent corners");
                        Require(bitmap.GetPixel(width/2,width/2).A==0,"Transparent D counter; no plate");
                        int left=width,top=width,right=0,bottom=0,pixels=0;
                        for(int y=0;y<width;y++)for(int x=0;x<width;x++)if(bitmap.GetPixel(x,y).A>32){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);pixels++;}
                        Require((bottom-top+1.0)/width>=0.88 && (right-left+1.0)/width>=0.8,"Mark occupies the native icon canvas");
                        Require(pixels/(double)(width*width)<0.65,"No opaque background plate");
                        records.Add(new {asset=name,size=width,bounds=new[] {left,top,right-left+1,bottom-top+1},opaquePixels=pixels});
                    }
                    // Framework Icon can select 128 for a PNG-compressed 256 request.
                    // Shell 256 data is validated above; tray/title-bar requests are <=64.
                    if(width<=128) using(var icon=new Icon(Path.Combine(assets,name+".ico"),width,width)) Require(icon.Width==width && icon.Height==width,"Windows native icon frame selection: "+name+" requested "+width+", got "+icon.Width+"x"+icon.Height);
                }
            }
        }
        foreach(string file in new[] {"dialvia-icon.svg","dialvia-mark.svg","dialvia-mark-dark.svg"}) {
            var svg=XDocument.Load(Path.Combine(assets,file));
            Require((string)svg.Root.Attribute("viewBox")=="209 190 620 620","Shared square vector canvas");
            Require(svg.Descendants().Count(x=>x.Name.LocalName=="path")==2 && !svg.Descendants().Any(x=>x.Name.LocalName=="rect"||x.Name.LocalName=="image"),"Two original vector paths with no plate or raster");
        }
        foreach(int size in Sizes.Concat(new[] {512})) Require(File.ReadAllBytes(Path.Combine(assets,"dialvia-"+size+".png")).SequenceEqual(File.ReadAllBytes(Path.Combine(assets,"dialvia-tray-light-"+size+".png"))),"Shell and in-app/light-tray brand artwork must match: "+size);
        var mark=XDocument.Load(Path.Combine(assets,"dialvia-icon.svg")).Descendants().Where(x=>x.Name.LocalName=="path").ToArray();
        Require((string)mark[0].Attribute("fill")=="#202320" && (string)mark[1].Attribute("fill")=="#B76546","Two-tone shell SVG palette");
        double light=Contrast(ColorTranslator.FromHtml("#202320"),Color.White),dark=Contrast(ColorTranslator.FromHtml("#F7F5F0"),ColorTranslator.FromHtml("#202020"));
        Require(light>=3 && dark>=3,"Light/dark tray primary has 3:1 contrast on its matching background");
        Specimens(assets,Path.Combine(Path.GetDirectoryName(report),"icon-sizes.png"));
        if(args.Length>2 && File.Exists(args[2])) Comparison(assets,args[2],Path.Combine(Path.GetDirectoryName(report),"icon-before-after.png"));
        string result=new JavaScriptSerializer().Serialize(new {passed=true,iconFrames=records.Count,svgFiles=3,sizes=Sizes,trayContrastLight=light,trayContrastDark=dark,shellMatchesBrand=true,frames=records});
        File.WriteAllText(report,result);Console.WriteLine("Validated 45 transparent native icon frames and 3 SVGs.");
    }
    static void Comparison(string assets,string baseline,string destination) {
        using(var sheet=new Bitmap(580,310))using(var g=Graphics.FromImage(sheet))using(var font=new Font("Segoe UI",12))
        using(var old=ReadPngFrame(baseline,24))using(var current=new Bitmap(Path.Combine(assets,"dialvia-tray-light-24.png"))) {
            g.Clear(ColorTranslator.FromHtml("#D9DDE5"));g.DrawString("Tray artwork / native 24 px + 4x zoom",font,Brushes.Black,20,14);
            g.DrawString("Before",font,Brushes.Black,122,58);g.DrawString("After",font,Brushes.Black,393,58);
            g.DrawImageUnscaled(old,145,96);g.DrawImageUnscaled(current,410,96);
            g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            g.DrawImage(old,new Rectangle(108,155,96,96),0,0,24,24,GraphicsUnit.Pixel);g.DrawImage(current,new Rectangle(374,155,96,96),0,0,24,24,GraphicsUnit.Pixel);
            sheet.Save(destination,ImageFormat.Png);
        }
    }
    static Bitmap ReadPngFrame(string path,int size) {
        using(var reader=new BinaryReader(File.OpenRead(path))) {
            Require(reader.ReadUInt16()==0 && reader.ReadUInt16()==1,"Baseline ICO header");int count=reader.ReadUInt16();
            for(int i=0;i<count;i++) {
                int width=reader.ReadByte(),height=reader.ReadByte();reader.ReadUInt16();reader.ReadUInt32();uint length=reader.ReadUInt32(),offset=reader.ReadUInt32();
                if(width!=size || height!=size)continue;
                reader.BaseStream.Position=offset;
                using(var stream=new MemoryStream(reader.ReadBytes((int)length)))using(var bitmap=new Bitmap(stream))return new Bitmap(bitmap);
            }
        }
        throw new Exception("Baseline has no native PNG frame for the comparison");
    }
    static void Specimens(string assets,string destination) {
        int[] sizes={16,20,24,30,32,36,48,64};string[] names={"dialvia","dialvia-tray-light","dialvia-tray-dark"};
        using(var sheet=new Bitmap(1330,800))using(var g=Graphics.FromImage(sheet))using(var font=new Font("Segoe UI",11))using(var label=new Font("Segoe UI",10)) {
            g.Clear(ColorTranslator.FromHtml("#F7F5F0"));g.DrawString("Dialvia / transparent native sizes / original + 2x nearest-neighbour",font,Brushes.Black,20,16);
            for(int row=0;row<3;row++) {
                bool dark=row==2;int y=60+row*240;g.FillRectangle(dark?Brushes.Black:Brushes.White,0,y,1330,240);
                g.DrawString(row==0?"App / shell":row==1?"Tray / light":"Tray / dark",label,dark?Brushes.White:Brushes.Black,14,y+10);
                for(int col=0;col<sizes.Length;col++) {
                    int x=130+col*150,size=sizes[col];g.DrawString(size+" px",label,dark?Brushes.White:Brushes.Black,x+36,y+9);
                    using(var bitmap=new Bitmap(Path.Combine(assets,names[row]+"-"+size+".png"))) {
                        g.DrawImageUnscaled(bitmap,x+(130-size)/2,y+34);
                        g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
                        g.DrawImage(bitmap,new Rectangle(x+(130-size*2)/2,y+103,size*2,size*2),0,0,size,size,GraphicsUnit.Pixel);
                    }
                }
            }
            sheet.Save(destination,ImageFormat.Png);
        }
    }
}
