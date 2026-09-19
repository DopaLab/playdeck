# Build Windows icon resources from the existing artwork. No redesign or generated pixels.
Add-Type -AssemblyName System.Drawing
$source=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot '../src/Playdeck/Assets/icon.png'))
$frames=[Collections.Generic.List[byte[]]]::new()
$sizes=@(16,20,24,32,40,48,64,128,256)
try {
 foreach($size in $sizes){
  $bitmap=[Drawing.Bitmap]::new($size,$size,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $graphics=[Drawing.Graphics]::FromImage($bitmap)
  $graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $graphics.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
  $graphics.CompositingQuality=[Drawing.Drawing2D.CompositingQuality]::HighQuality
  # Remove 3.75 percent padding per edge; retain the complete rounded metal outline.
  $inset=[int]($source.Width*0.0375)
  $graphics.DrawImage($source,[Drawing.Rectangle]::new(0,0,$size,$size),[Drawing.Rectangle]::new($inset,$inset,$source.Width-2*$inset,$source.Height-2*$inset),[Drawing.GraphicsUnit]::Pixel)
  $stream=[IO.MemoryStream]::new();$bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png);$frames.Add($stream.ToArray())
  $stream.Dispose();$graphics.Dispose();$bitmap.Dispose()
 }
 $output=[IO.File]::Create((Join-Path $PSScriptRoot '../src/Playdeck/app.ico'));$writer=[IO.BinaryWriter]::new($output)
 try {
  $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$sizes.Count);$offset=6+16*$sizes.Count
  for($i=0;$i -lt $sizes.Count;$i++){$s=$sizes[$i];$d=if($s -eq 256){0}else{$s};$writer.Write([byte]$d);$writer.Write([byte]$d);$writer.Write([byte]0);$writer.Write([byte]0);$writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frames[$i].Length);$writer.Write([uint32]$offset);$offset+=$frames[$i].Length}
  foreach($frame in $frames){$writer.Write($frame)}
 }finally{$writer.Dispose();$output.Dispose()}
}finally{$source.Dispose()}
