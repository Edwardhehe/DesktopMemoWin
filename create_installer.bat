@echo off
echo 正在创建桌面备忘录安装包...

REM 设置变量
set "SOURCE_DIR=bin\Release\net8.0-windows\win-x64\publish"
set "INSTALLER_DIR=DesktopMemo_Installer"
set "EXE_NAME=DesktopMemo.exe"

REM 创建安装包目录
if exist %INSTALLER_DIR% rmdir /s /q %INSTALLER_DIR%
mkdir %INSTALLER_DIR%

REM 复制可执行文件
echo 复制主程序文件...
copy "%SOURCE_DIR%\%EXE_NAME%" "%INSTALLER_DIR%\"

REM 复制图标文件
echo 复制图标文件...
copy "memo.ico" "%INSTALLER_DIR%\"

REM 创建安装说明
echo 创建安装说明...
(
echo 桌面备忘录 - 安装说明
echo ========================
echo.
echo 1. 将 DesktopMemo.exe 复制到您想要的目录
echo 2. 双击运行 DesktopMemo.exe 启动程序
echo 3. 程序会自动创建数据库文件在用户目录下
echo 4. 首次运行时会自动添加到系统托盘
echo.
echo 功能特点：
echo - 半透明桌面备忘录
echo - 日历式界面
echo - 支持待办事项管理
echo - 系统托盘运行
echo - 开机自启动选项
echo.
echo 注意事项：
echo - 需要 Windows 10 或更高版本
echo - 需要 .NET 8.0 运行时（已包含在程序中）
echo - 建议将程序放在固定目录，避免移动
echo.
echo 技术支持：
echo 如有问题，请查看程序内的帮助信息
) > "%INSTALLER_DIR%\安装说明.txt"

REM 创建桌面快捷方式脚本
echo 创建桌面快捷方式脚本...
(
echo @echo off
echo echo 正在创建桌面快捷方式...
echo.
echo REM 获取当前目录
echo set "CURRENT_DIR=%%~dp0"
echo.
echo REM 创建桌面快捷方式
echo powershell -Command "$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut^('%%USERPROFILE%%\Desktop\桌面备忘录.lnk'^); $Shortcut.TargetPath = '%%CURRENT_DIR%%DesktopMemo.exe'; $Shortcut.WorkingDirectory = '%%CURRENT_DIR%%'; $Shortcut.IconLocation = '%%CURRENT_DIR%%memo.ico'; $Shortcut.Save^(^)"
echo.
echo echo 桌面快捷方式创建完成！
echo pause
) > "%INSTALLER_DIR%\创建桌面快捷方式.bat"

echo.
echo 安装包创建完成！
echo 安装包位置: %INSTALLER_DIR%
echo.
echo 包含文件：
echo - DesktopMemo.exe ^(主程序^)
echo - memo.ico ^(程序图标^)
echo - 安装说明.txt ^(安装说明^)
echo - 创建桌面快捷方式.bat ^(快捷方式创建脚本^)
echo.
echo 使用方法：
echo 1. 将 %INSTALLER_DIR% 文件夹复制到目标计算机
echo 2. 运行 DesktopMemo.exe 启动程序
echo 3. 如需桌面快捷方式，运行"创建桌面快捷方式.bat"
echo.
pause 