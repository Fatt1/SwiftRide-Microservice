# SwiftRide - Script dừng các microservices trên máy host
Get-Process | Where-Object { $_.ProcessName -match "YARPAPiGw|Identity\.API|Trip\.API|Matching\.API|Payment\.API|Notification\.API" } | Stop-Process -Force -ErrorAction SilentlyContinue
Get-Process -Name dotnet -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "Đã dừng toàn bộ các tiến trình backend .NET!"
