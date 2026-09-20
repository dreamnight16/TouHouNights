Add-Type -AssemblyName System.Drawing
function Crop($src,$dst,$x,$y,$w,$h,$z){
  $img=[System.Drawing.Image]::FromFile($src)
  $dw=[int]($w*$z); $dh=[int]($h*$z)
  $bmp=New-Object -ArgumentList @([int]$dw,[int]$dh) -TypeName System.Drawing.Bitmap
  $g=[System.Drawing.Graphics]::FromImage($bmp)
  $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
  $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
  $g.DrawImage($img,(New-Object System.Drawing.Rectangle 0,0,$dw,$dh),(New-Object System.Drawing.Rectangle $x,$y,$w,$h),[System.Drawing.GraphicsUnit]::Pixel)
  $bmp.Save($dst,[System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose();$bmp.Dispose();$img.Dispose()
  Write-Output "$dst done"
}
Crop ".reports/battle-ui.png" ".reports/crop-missiontag.png" 20 10 250 90 3
Crop ".reports/battle-ui.png" ".reports/crop-gold.png" 630 535 250 55 4
Crop ".reports/battle-ui-pause.png" ".reports/crop-pause.png" 130 150 400 420 2
