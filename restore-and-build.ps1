# 恢复并构建项目
Write-Host "=== 删除旧的 bin/obj ==="
Remove-Item -Recurse -Force "src/MoneyReCorder/bin" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "src/MoneyReCorder/obj" -ErrorAction SilentlyContinue

Write-Host "=== 恢复 NuGet 包 ==="
dotnet restore "src/MoneyReCorder/MoneyReCorder.csproj"

Write-Host "=== 编译项目 ==="
dotnet build "src/MoneyReCorder/MoneyReCorder.csproj" --no-restore
