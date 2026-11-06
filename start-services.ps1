# SwiftRide - Script khởi chạy nhanh các microservices trên máy host
$baseDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$services = @(
    @{ Name = "Identity.API"; Dir = "$baseDir\src\Services\Identity\Identity.API" },
    @{ Name = "Trip.API"; Dir = "$baseDir\src\Services\Trip\Trip.API" },
    @{ Name = "Matching.API"; Dir = "$baseDir\src\Services\Matching\Matching.API" },
    @{ Name = "Payment.API"; Dir = "$baseDir\src\Services\Payment\Payment.API" },
    @{ Name = "Notification.API"; Dir = "$baseDir\src\Services\Notification\Notification.API" },
    @{ Name = "YARPAPiGw"; Dir = "$baseDir\src\ApiGateways\YARPAPiGw" }
)

foreach ($svc in $services) {
    Write-Host "Khởi chạy $($svc.Name)..."
    Start-Process -FilePath "dotnet" -ArgumentList "run --no-build" -WorkingDirectory $svc.Dir -WindowStyle Minimized
}

Write-Host "Tất cả 6 services đã được khởi chạy dưới nền!"
