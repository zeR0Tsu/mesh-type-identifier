param([string]$Version)

$name = "com.zer0tsu.mesh-type-identifier"

# 读取版本号
if (-not $Version) {
    $pkg = Get-Content ".\package.json" | ConvertFrom-Json
    $Version = $pkg.version
}

$zipName = "$name-$Version.zip"
$buildDir = ".\Builds"
$stageDir = "$buildDir\_stage_$Version"

# 创建输出目录
if (-not (Test-Path $buildDir)) { New-Item -ItemType Directory -Path $buildDir -Force | Out-Null }

# 清理暂存目录
if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null

# 复制文件
$items = @(
    "package.json",
    "README.md",
    "CHANGELOG.md",
    "LICENSE",
    "Runtime\",
    "Editor\"
)

foreach ($item in $items) {
    $src = ".\$item"
    $dst = "$stageDir\$item"
    if (Test-Path $src) {
        $parent = Split-Path $dst -Parent
        if (!(Test-Path $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
        if ((Get-Item $src) -is [System.IO.DirectoryInfo]) {
            $null = New-Item -ItemType Directory -Path $dst -Force
            Copy-Item ".\$item*" "$dst" -Recurse -Force
        } else {
            Copy-Item $src $dst -Force
        }
    }
}

# 压缩
$compressPath = "$buildDir\$zipName"
if (Test-Path $compressPath) { Remove-Item $compressPath -Force }
Compress-Archive -Path "$stageDir\*" -DestinationPath $compressPath

# 清理
Remove-Item $stageDir -Recurse -Force

Write-Host "Build complete: $zipName"
Write-Host "Output: $compressPath"
