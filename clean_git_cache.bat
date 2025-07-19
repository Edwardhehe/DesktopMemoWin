@echo off
echo 正在清理Git缓存中不应该被跟踪的文件...

REM 移除已经被跟踪但现在应该被忽略的文件
git rm -r --cached bin/ 2>nul
git rm -r --cached obj/ 2>nul
git rm -r --cached DesktopMemo_Final/ 2>nul
git rm -r --cached DesktopMemo_Installer/ 2>nul
git rm -r --cached Properties/PublishProfiles/ 2>nul
git rm --cached *.csproj.user 2>nul
git rm --cached *_wpftmp.csproj 2>nul

echo Git缓存清理完成！
echo 现在只有必要的源代码文件会被版本控制。

pause
