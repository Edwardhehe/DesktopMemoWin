@echo off
echo 正在构建桌面备忘录应用程序...

REM 清理之前的构建
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

REM 还原NuGet包
dotnet restore

REM 构建应用程序
dotnet build -c Release

if %ERRORLEVEL% EQU 0 (
    echo 构建成功！
    echo 可执行文件位置: bin\Release\net8.0-windows\DesktopMemo.exe
) else (
    echo 构建失败！
    pause
) 