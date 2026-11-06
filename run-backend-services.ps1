# SwiftRide - Unified Backend Runner
$baseDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$services = @(
    @{ Name = "Identity.API"; Project = "$baseDir\src\Services\Identity\Identity.API\Identity.API.csproj" },
    @{ Name = "Trip.API"; Project = "$baseDir\src\Services\Trip\Trip.API\Trip.API.csproj" },
    @{ Name = "Matching.API"; Project = "$baseDir\src\Services\Matching\Matching.API\Matching.API.csproj" },
    @{ Name = "Payment.API"; Project = "$baseDir\src\Services\Payment\Payment.API\Payment.API.csproj" },
    @{ Name = "Notification.API"; Project = "$baseDir\src\Services\Notification\Notification.API\Notification.API.csproj" },
    @{ Name = "YARPAPiGw"; Project = "$baseDir\src\ApiGateways\YARPAPiGw\YARPAPiGw.csproj" }
)

Write-Host "Khởi động tất cả các dịch vụ Backend SwiftRide..." -ForegroundColor Cyan

$processes = @()
foreach ($svc in $services) {
    Write-Host "  -> Bật $($svc.Name)..."
    $proc = Start-Process -FilePath "dotnet" -ArgumentList "run --project `"$($svc.Project)`" --no-build" -PassThru
    $processes += $proc
    Start-Sleep -Milliseconds 800
}

Write-Host "Tất cả các dịch vụ Backend đã sẵn sàng và đang hoạt động!" -ForegroundColor Green

# Giữ tiến trình runner chạy để giữ các service
try {
    while ($true) {
        Start-Sleep -Seconds 5
        # Kiểm tra xem có service nào bị thoát không
        $alive = $processes | Where-Object { -not $_.HasExited }
        if ($alive.Count -eq 0) {
            Write-Host "Tất cả các service đã dừng."
            break
        }
    }
} finally {
    Write-Host "Đang dọn dẹp các tiến trình..."
    foreach ($p in $processes) {
        if (-not $p.HasExited) {
            Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        }
    }
}
