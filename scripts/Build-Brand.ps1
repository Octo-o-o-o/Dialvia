$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path $PSScriptRoot -Parent
$assets=Join-Path $root 'assets\brand'
New-Item -ItemType Directory -Path $assets -Force | Out-Null
$ink='M 252 238 C 252 225 263 214 276 214 L 464 214 C 540 214 602 234 652 270 L 572 378 C 544 360 508 350 464 350 L 390 350 L 390 650 L 464 650 C 508 650 544 640 572 622 L 652 730 C 602 766 540 786 464 786 L 276 786 C 263 786 252 775 252 762 Z'
$copper='M 690 306 C 754 356 786 420 786 500 C 786 580 754 644 690 694 L 610 584 C 636 558 650 530 650 500 C 650 470 636 442 610 416 Z'
$background='M 172 0 L 828 0 C 923 0 1000 77 1000 172 L 1000 828 C 1000 923 923 1000 828 1000 L 172 1000 C 77 1000 0 923 0 828 L 0 172 C 0 77 77 0 172 0 Z'
$svg='<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1000 1000"><rect width="1000" height="1000" rx="172" fill="#F7F5F0"/><path fill="#202320" d="'+$ink+'"/><path fill="#B76546" d="'+$copper+'"/></svg>'
$mark='<svg xmlns="http://www.w3.org/2000/svg" viewBox="180 160 680 680"><path fill="#202320" d="'+$ink+'"/><path fill="#B76546" d="'+$copper+'"/></svg>'
[IO.File]::WriteAllText((Join-Path $assets 'dialvia-icon.svg'),$svg,[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $assets 'dialvia-mark.svg'),$mark,[Text.UTF8Encoding]::new($false))
function Convert-VectorPath([string]$definition) {
    $tokens=@([regex]::Matches($definition,'[MLCZ]|-?\d+(?:\.\d+)?') | ForEach-Object {$_.Value})
    $position=0
    $path=[Drawing.Drawing2D.GraphicsPath]::new()
    $current=[Drawing.PointF]::new(0,0);$start=$current
    while($position -lt $tokens.Count){
        $command=$tokens[$position];$position++
        switch($command){
            'M' {$current=[Drawing.PointF]::new([single]$tokens[$position],[single]$tokens[$position+1]);$position+=2;$start=$current}
            'L' {$next=[Drawing.PointF]::new([single]$tokens[$position],[single]$tokens[$position+1]);$position+=2;$path.AddLine($current,$next);$current=$next}
            'C' {$first=[Drawing.PointF]::new([single]$tokens[$position],[single]$tokens[$position+1]);$second=[Drawing.PointF]::new([single]$tokens[$position+2],[single]$tokens[$position+3]);$next=[Drawing.PointF]::new([single]$tokens[$position+4],[single]$tokens[$position+5]);$position+=6;$path.AddBezier($current,$first,$second,$next);$current=$next}
            'Z' {$path.CloseFigure();$current=$start}
            default {throw ('Unsupported path command '+$command)}
        }
    }
    return $path
}
$paperBrush=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#F7F5F0'))
$inkBrush=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#202320'))
$copperBrush=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#B76546'))
$paperPath=Convert-VectorPath $background;$inkPath=Convert-VectorPath $ink;$copperPath=Convert-VectorPath $copper
$sizes=@(16,20,24,32,40,48,64,96,128,256)
$frames=[Collections.Generic.List[byte[]]]::new()
try {
    foreach($size in @($sizes)+@(512)) {
        $bitmap=[Drawing.Bitmap]::new($size,$size)
        $graphics=[Drawing.Graphics]::FromImage($bitmap)
        $stream=[IO.MemoryStream]::new()
        try {
            $graphics.SmoothingMode='AntiAlias';$graphics.PixelOffsetMode='HighQuality';$graphics.Clear([Drawing.Color]::Transparent)
            $graphics.ScaleTransform([single]($size/1000.0),[single]($size/1000.0))
            $graphics.FillPath($paperBrush,$paperPath);$graphics.FillPath($inkBrush,$inkPath);$graphics.FillPath($copperBrush,$copperPath)
            $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
            if($size -ne 512){$frames.Add($stream.ToArray())}
            if($size -in @(64,256,512)){$bitmap.Save((Join-Path $assets ('dialvia-'+$size+'.png')),[Drawing.Imaging.ImageFormat]::Png)}
        } finally {$stream.Dispose();$graphics.Dispose();$bitmap.Dispose()}
    }
    $file=[IO.File]::Create((Join-Path $assets 'dialvia.ico'));$writer=[IO.BinaryWriter]::new($file)
    try {
        $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$sizes.Count);$offset=6+16*$sizes.Count
        for($index=0;$index -lt $sizes.Count;$index++){
            $dimension=if($sizes[$index] -eq 256){0}else{$sizes[$index]}
            $writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0)
            $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frames[$index].Length);$writer.Write([uint32]$offset);$offset+=$frames[$index].Length
        }
        foreach($frame in $frames){$writer.Write($frame)}
    } finally {$writer.Dispose();$file.Dispose()}
} finally {foreach($item in @($paperBrush,$inkBrush,$copperBrush,$paperPath,$inkPath,$copperPath)){$item.Dispose()}}
