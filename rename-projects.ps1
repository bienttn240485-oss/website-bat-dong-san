$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$solution = Join-Path $root "RealEstateManagement.slnx"

$projects = @(
    @{
        OldFolder = "src\FootballBooking.Application"
        NewFolder = "src\RealEstateManagement.Application"
        OldProject = "FootballBooking.Application.csproj"
        NewProject = "RealEstateManagement.Application.csproj"
    },
    @{
        OldFolder = "src\FootballBooking.Infrastructure"
        NewFolder = "src\RealEstateManagement.Infrastructure"
        OldProject = "FootballBooking.Infrastructure.csproj"
        NewProject = "RealEstateManagement.Infrastructure.csproj"
    },
    @{
        OldFolder = "src\FootballBooking.Web"
        NewFolder = "src\RealEstateManagement.Web"
        OldProject = "FootballBooking.Web.csproj"
        NewProject = "RealEstateManagement.Web.csproj"
    },
    @{
        OldFolder = "tests\FootballBooking.Tests"
        NewFolder = "tests\RealEstateManagement.Tests"
        OldProject = "FootballBooking.Tests.csproj"
        NewProject = "RealEstateManagement.Tests.csproj"
    }
)

foreach ($project in $projects) {
    $oldFolder = Join-Path $root $project.OldFolder
    $newFolder = Join-Path $root $project.NewFolder

    if (Test-Path $oldFolder) {
        Write-Host "Copy: $($project.OldFolder) -> $($project.NewFolder)"

        robocopy $oldFolder $newFolder /E /XD bin obj | Out-Host

        if ($LASTEXITCODE -ge 8) {
            throw "Robocopy thất bại với mã $LASTEXITCODE"
        }
    }

    $oldProjectInNewFolder = Join-Path $newFolder $project.OldProject
    $newProjectPath = Join-Path $newFolder $project.NewProject

    if ((Test-Path $oldProjectInNewFolder) -and !(Test-Path $newProjectPath)) {
        Rename-Item $oldProjectInNewFolder $project.NewProject
    }
}

Write-Host "Sửa toàn bộ ProjectReference..."

Get-ChildItem $root -Recurse -Filter "*.csproj" |
    Where-Object {
        $_.FullName -notmatch "\\bin\\" -and
        $_.FullName -notmatch "\\obj\\"
    } |
    ForEach-Object {
        $path = $_.FullName
        $content = Get-Content $path -Raw

        $content = $content.Replace(
            "FootballBooking.Application\FootballBooking.Application.csproj",
            "RealEstateManagement.Application\RealEstateManagement.Application.csproj"
        )

        $content = $content.Replace(
            "FootballBooking.Infrastructure\FootballBooking.Infrastructure.csproj",
            "RealEstateManagement.Infrastructure\RealEstateManagement.Infrastructure.csproj"
        )

        $content = $content.Replace(
            "FootballBooking.Web\FootballBooking.Web.csproj",
            "RealEstateManagement.Web\RealEstateManagement.Web.csproj"
        )

        $content = $content.Replace(
            "FootballBooking.Tests\FootballBooking.Tests.csproj",
            "RealEstateManagement.Tests\RealEstateManagement.Tests.csproj"
        )

        Set-Content $path $content -Encoding UTF8
    }

Write-Host "Cập nhật solution..."

foreach ($project in $projects) {
    $oldProjectPath = Join-Path $project.OldFolder $project.OldProject
    $newProjectPath = Join-Path $project.NewFolder $project.NewProject

    dotnet sln $solution remove $oldProjectPath 2>$null
    dotnet sln $solution add $newProjectPath
}

Write-Host "Build kiểm tra..."

dotnet build $solution

if ($LASTEXITCODE -ne 0) {
    throw "Build thất bại. Chưa xóa các thư mục cũ."
}

Write-Host ""
Write-Host "Hoàn tất đổi tên project."
Write-Host "Build succeeded."
Write-Host "Các thư mục FootballBooking cũ chưa bị xóa để tránh mất dữ liệu."