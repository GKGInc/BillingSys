# Generates favicon.png (48x48) and images/icon-120.png (120x120) matching Wrigley compact mark colors.
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

function New-WrigleyIcon {
    param([int]$Size)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::FromArgb(7, 41, 96))

    $scale = $Size / 64.0
    $sx = { param($x) [int]$x * $scale }
    $sy = { param($y) [int]$y * $scale }

    # Crown (simplified polygon)
    $crown = New-Object System.Drawing.Drawing2D.GraphicsPath
    $crown.AddLines(@(
        [System.Drawing.Point]::new([int](10 * $scale), [int](28 * $scale)),
        [System.Drawing.Point]::new([int](16 * $scale), [int](8 * $scale)),
        [System.Drawing.Point]::new([int](22 * $scale), [int](16 * $scale)),
        [System.Drawing.Point]::new([int](32 * $scale), [int](4 * $scale)),
        [System.Drawing.Point]::new([int](42 * $scale), [int](16 * $scale)),
        [System.Drawing.Point]::new([int](48 * $scale), [int](8 * $scale)),
        [System.Drawing.Point]::new([int](54 * $scale), [int](28 * $scale))
    ))
    $crown.CloseFigure()
    $g.FillPath([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(7, 41, 96)), $crown)

    # Gold crown band
    $bandH = [Math]::Max(2, [int](5 * $scale))
    $bandY = [int](24 * $scale)
    $g.FillRectangle(
        [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 157, 0)),
        [int](12 * $scale), $bandY, [int](40 * $scale), $bandH)

    # Gold dots on crown
    $gold = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 157, 0))
    $r = [Math]::Max(2.0, 2.5 * $scale)
    foreach ($pt in @(
        @(20, 12), @(32, 6), @(44, 12)
    )) {
        $gx = [int]($pt[0] * $scale) - $r
        $gy = [int]($pt[1] * $scale) - $r
        $g.FillEllipse($gold, $gx, $gy, 2 * $r, 2 * $r)
    }

    # Ears
    $ear = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(7, 41, 96))
    $g.FillEllipse($ear, [int](4 * $scale), [int](32 * $scale), [int](20 * $scale), [int](24 * $scale))
    $g.FillEllipse($ear, [int](40 * $scale), [int](32 * $scale), [int](20 * $scale), [int](24 * $scale))

    # Head
    $g.FillEllipse($ear, [int](12 * $scale), [int](36 * $scale), [int](40 * $scale), [int](34 * $scale))

    # Muzzle (white)
    $w = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 255, 255))
    $g.FillEllipse($w, [int](21 * $scale), [int](44 * $scale), [int](22 * $scale), [int](18 * $scale))

    # Nose
    $g.FillEllipse($ear, [int](28 * $scale), [int](46 * $scale), [int](8 * $scale), [int](7 * $scale))

    $g.Dispose()
    return $bmp
}

$root = Split-Path -Parent $PSScriptRoot
$www = Join-Path $root "src\BillingSys.Client\wwwroot"
$img = Join-Path $www "images"
if (-not (Test-Path $img)) { New-Item -ItemType Directory -Path $img | Out-Null }

$fav = New-WrigleyIcon -Size 48
$fav.Save((Join-Path $www "favicon.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$fav.Dispose()

$icon = New-WrigleyIcon -Size 120
$icon.Save((Join-Path $img "icon-120.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$icon.Dispose()

Write-Host "Wrote favicon.png and images/icon-120.png"
