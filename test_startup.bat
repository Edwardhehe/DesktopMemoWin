@echo off
echo 开机启动功能测试脚本
echo ========================

echo.
echo 1. 检查当前开机启动状态...
reg query "HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v DesktopMemo 2>nul
if %ERRORLEVEL% EQU 0 (
    echo ✓ 开机启动已启用
    echo.
    echo 注册表中的启动项：
    reg query "HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v DesktopMemo
) else (
    echo ✗ 开机启动未启用
)

echo.
echo 2. 检查可执行文件路径...
set "CURRENT_EXE=%~dp0DesktopMemo.exe"
if exist "%CURRENT_EXE%" (
    echo ✓ 找到可执行文件: %CURRENT_EXE%
) else (
    echo ✗ 未找到可执行文件: %CURRENT_EXE%
    
    REM 检查bin目录
    set "BIN_EXE=%~dp0bin\Release\net8.0-windows\DesktopMemo.exe"
    if exist "%BIN_EXE%" (
        echo ✓ 找到构建输出文件: %BIN_EXE%
    ) else (
        echo ✗ 未找到构建输出文件: %BIN_EXE%
    )
)

echo.
echo 3. 检查所有可能的启动项...
echo 当前用户启动项：
reg query "HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" 2>nul | findstr /i "DesktopMemo"
if %ERRORLEVEL% NEQ 0 (
    echo   (未找到DesktopMemo相关启动项)
)

echo.
echo 系统启动项：
reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" 2>nul | findstr /i "DesktopMemo"
if %ERRORLEVEL% NEQ 0 (
    echo   (未找到DesktopMemo相关启动项)
)

echo.
echo 4. 测试建议...
echo 如果开机启动不工作，请检查：
echo - 确保程序路径正确且文件存在
echo - 确保有足够权限修改注册表
echo - 尝试以管理员身份运行程序
echo - 检查杀毒软件是否阻止了注册表修改

echo.
pause
