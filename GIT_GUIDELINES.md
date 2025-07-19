# Git 版本控制指南

## 被版本控制的文件

### 源代码文件
- `*.cs` - C# 源代码文件
- `*.xaml` - WPF 界面文件
- `*.csproj` - 项目配置文件（不包含用户特定设置）
- `*.sln` - 解决方案文件

### 配置和资源文件
- `App.xaml` - 应用程序配置
- `memo.ico` - 应用程序图标
- `LICENSE.txt` - 许可证文件
- `README.md` - 项目文档
- `.gitignore` - Git忽略规则
- `.gitattributes` - Git属性配置

### 构建脚本
- `build.bat` - 构建脚本
- `create_installer.bat` - 安装包创建脚本

## 被忽略的文件和目录

### 构建输出
- `bin/` - 编译输出目录
- `obj/` - 中间文件目录
- `DesktopMemo_Final/` - 最终构建输出
- `DesktopMemo_Installer/` - 安装包输出

### 用户特定文件
- `*.csproj.user` - 用户特定的项目设置
- `Properties/PublishProfiles/` - 发布配置文件
- `*_wpftmp.csproj` - WPF临时项目文件

### 临时文件
- `*.tmp`, `*.temp` - 临时文件
- `*.log` - 日志文件
- `*.bak`, `*.backup` - 备份文件

### 数据库文件
- `*.db`, `*.sqlite`, `*.sqlite3` - 数据库文件

### IDE配置
- `.vs/` - Visual Studio配置
- `.vscode/` - VS Code配置
- `.idea/` - JetBrains IDE配置

### 系统文件
- `Thumbs.db` - Windows缩略图缓存
- `Desktop.ini` - Windows桌面配置
- `$RECYCLE.BIN/` - 回收站
- `.DS_Store` - macOS系统文件

## 最佳实践

### 提交前检查
```bash
# 查看当前状态
git status

# 查看将要提交的更改
git diff --staged

# 确保只提交必要的文件
git ls-files --others --ignored --exclude-standard
```

### 清理缓存
如果之前已经跟踪了不应该被跟踪的文件：
```bash
# 运行清理脚本
clean_git_cache.bat

# 或手动移除
git rm -r --cached bin/
git rm -r --cached obj/
```

### 提交规范
- 使用有意义的提交信息
- 每次提交只包含相关的更改
- 避免提交构建输出和临时文件
- 定期检查.gitignore是否需要更新

### 分支管理
- `master` - 主分支，包含稳定版本
- `develop` - 开发分支，用于日常开发
- `feature/*` - 功能分支，用于新功能开发
- `hotfix/*` - 热修复分支，用于紧急修复

## 常用命令

```bash
# 查看被忽略的文件
git ls-files --others --ignored --exclude-standard

# 查看被跟踪的文件
git ls-files

# 移除已跟踪但应该被忽略的文件
git rm --cached <file>

# 强制添加被忽略的文件（谨慎使用）
git add -f <file>

# 检查文件是否被忽略
git check-ignore <file>
```

## 注意事项

1. **不要提交敏感信息**：如数据库连接字符串、API密钥等
2. **保持.gitignore更新**：随着项目发展，及时更新忽略规则
3. **定期清理**：定期检查是否有不必要的文件被跟踪
4. **团队协作**：确保团队成员都遵循相同的版本控制规范
5. **备份重要数据**：版本控制不是备份，重要数据要单独备份

## 故障排除

### 文件仍然被跟踪
如果修改.gitignore后文件仍然被跟踪：
```bash
git rm --cached <file>
git commit -m "Remove tracked file that should be ignored"
```

### 恢复被误删的文件
```bash
git checkout HEAD -- <file>
```

### 查看文件历史
```bash
git log --follow <file>
```
