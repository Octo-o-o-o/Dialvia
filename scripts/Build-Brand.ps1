$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path $PSScriptRoot -Parent
$assets=Join-Path $root 'assets\brand'
New-Item -ItemType Directory -Path $assets -Force | Out-Null
$ink='M 252 238 C 252 225 263 214 276 214 L 464 214 C 540 214 602 234 652 270 L 572 378 C 544 360 508 350 464 350 L 390 350 L 390 650 L 464 650 C 508 650 544 640 572 622 L 652 730 C 602 766 540 786 464 786 L 276 786 C 263 786 252 775 252 762 Z'
$copper='M 690 306 C 754 356 786 420 786 500 C 786 580 754 644 690 694 L 610 584 C 636 558 650 530 650 500 C 650 470 636 442 610 416 Z'
$shellViewBox='161.5 142.5 715 715'
$compactViewBox='209 190 620 620'
$sizes=@(16,20,24,30,32,36,40,48,60,64,72,80,96,128,256)
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
function Write-IconSet([string]$name,[string]$primary,[string]$accent,[string]$svgName,[string]$viewBox) {
    $canvas=@($viewBox.Split(' ') | ForEach-Object {[single]::Parse($_,[Globalization.CultureInfo]::InvariantCulture)})
    $svg='<svg xmlns="http://www.w3.org/2000/svg" viewBox="'+$viewBox+'" role="img" aria-label="Dialvia"><title>Dialvia</title><path fill="'+$primary+'" d="'+$ink+'"/><path fill="'+$accent+'" d="'+$copper+'"/></svg>'
    [IO.File]::WriteAllText((Join-Path $assets $svgName),$svg,[Text.UTF8Encoding]::new($false))
    $inkBrush=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($primary))
    $copperBrush=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($accent))
    $inkPath=Convert-VectorPath $ink;$copperPath=Convert-VectorPath $copper
    $frames=[Collections.Generic.List[byte[]]]::new()
    try {
    foreach($size in @($sizes)+@(512)) {
        $bitmap=[Drawing.Bitmap]::new($size,$size)
        $graphics=[Drawing.Graphics]::FromImage($bitmap)
        $stream=[IO.MemoryStream]::new()
        try {
            $graphics.SmoothingMode='AntiAlias';$graphics.PixelOffsetMode='HighQuality';$graphics.Clear([Drawing.Color]::Transparent)
            $graphics.ScaleTransform([single]($size/$canvas[2]),[single]($size/$canvas[3]))
            $graphics.TranslateTransform(-$canvas[0],-$canvas[1])
            $graphics.FillPath($inkBrush,$inkPath);$graphics.FillPath($copperBrush,$copperPath)
            $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
            if($size -ne 512){$frames.Add($stream.ToArray())}
            $bitmap.Save((Join-Path $assets ($name+'-'+$size+'.png')),[Drawing.Imaging.ImageFormat]::Png)
        } finally {$stream.Dispose();$graphics.Dispose();$bitmap.Dispose()}
    }
    $file=[IO.File]::Create((Join-Path $assets ($name+'.ico')));$writer=[IO.BinaryWriter]::new($file)
    try {
        $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$sizes.Count);$offset=6+16*$sizes.Count
        for($index=0;$index -lt $sizes.Count;$index++){
            $dimension=if($sizes[$index] -eq 256){0}else{$sizes[$index]}
            $writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0)
            $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frames[$index].Length);$writer.Write([uint32]$offset);$offset+=$frames[$index].Length
        }
        foreach($frame in $frames){$writer.Write($frame)}
    } finally {$writer.Dispose();$file.Dispose()}
    } finally {foreach($item in @($inkBrush,$copperBrush,$inkPath,$copperPath)){$item.Dispose()}}
}
# Keep taskbar artwork at 80% height; compact tray/header artwork stays at 92%.
# Geometry and colors match, while each role has its own transparent canvas.
Write-IconSet 'dialvia' '#202320' '#B76546' 'dialvia-icon.svg' $shellViewBox
Write-IconSet 'dialvia-tray-light' '#202320' '#B76546' 'dialvia-mark.svg' $compactViewBox
Write-IconSet 'dialvia-tray-dark' '#F7F5F0' '#D28B67' 'dialvia-mark-dark.svg' $compactViewBox
Copy-Item -LiteralPath (Join-Path $assets 'dialvia-tray-light-256.png') -Destination (Join-Path $assets 'dialvia-mark-256.png') -Force
$manifest=[ordered]@{schemaVersion=2;viewBoxes=[ordered]@{shell=$shellViewBox;tray=$compactViewBox;brand=$compactViewBox};transparent=$true;glyphHeightFractions=[ordered]@{shell=(572.0/715.0);tray=(572.0/620.0);brand=(572.0/620.0)};sizes=$sizes;shell='dialvia.ico';trayLight='dialvia-tray-light.ico';trayDark='dialvia-tray-dark.ico';brand='dialvia-mark.svg'}
[IO.File]::WriteAllText((Join-Path $assets 'manifest.json'),($manifest|ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
