# 桌面备忘录 DesktopMemoWin

`DesktopMemoWin` 是一个基于 WPF 的 Windows 桌面备忘录应用。它以月历为主界面，把每天的待办直接贴在桌面上，同时支持侧边便签、当日任务管理、回收站、系统托盘和开机启动等功能。

## 当前版本

- 版本：`v1.4.2`
- 平台：`Windows x64`
- 运行时：`.NET 8`
- 数据库：`SQLite`

## 主要功能

- 月历视图：按日期展示备忘录，支持快速新增、完成、置顶、优先级
- 列表视图：支持今日、逾期、本周、全部、回收站等常用视图
- 当日任务窗口：专注查看和管理某一天的任务
- 侧边便签：默认紧贴主界面，可自由拖动、缩放，支持滚动
- 便签自动保存：每次编辑即时写入文本文件，关闭再打开内容不丢失
- 完成状态：已完成条目自动变色并排到后面
- 系统托盘：双击托盘可恢复窗口，主界面与便签联动显示和隐藏
- 数据管理：支持导入、导出、备份、恢复
- 开机启动：可在界面中直接开关

## 数据存储位置

- 数据库：`%USERPROFILE%\DesktopMemo\memo.db`
- 配置文件：`%USERPROFILE%\DesktopMemo\config.txt`
- 便签布局：`%USERPROFILE%\DesktopMemo\sticky-note-layout.json`
- 便签内容：`%USERPROFILE%\DesktopMemo\sticky-note-content.txt`

## 数据表结构

```sql
CREATE TABLE IF NOT EXISTS MemoItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Content TEXT NOT NULL,
    Date TEXT NOT NULL,
    IsCompleted INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    CompletedAt TEXT,
    SortOrder INTEGER NOT NULL DEFAULT 0
);
```

## 构建与运行

### 调试模式

```powershell
dotnet build DesktopMemo.csproj -c Debug
dotnet run --project DesktopMemo.csproj
```

### 发布模式

```powershell
dotnet publish DesktopMemo.csproj -c Release -r win-x64 --self-contained true
```

默认发布后的可执行文件：

```text
bin\Release\net8.0-windows\win-x64\publish\DesktopMemo.exe
```

## 项目结构

- `Models`：数据模型
- `Services`：数据库、托盘、桌面监听、开机启动、便签状态等服务
- `ViewModels`：主界面业务逻辑
- `Views`：主窗口、当日任务、便签、设置、编辑窗口
- `Converters`：界面绑定转换器

## 核心模块

- `Services/DatabaseService.cs`：SQLite 读写、导入导出、备份恢复
- `Services/SystemTrayService.cs`：系统托盘行为
- `Services/DesktopMonitorService.cs`：桌面显示状态监听
- `Services/StartupService.cs`：开机启动注册
- `Services/StickyNoteStateService.cs`：侧边便签布局与内容持久化
- `ViewModels/MainViewModel.cs`：主界面数据与命令
- `Views/MainWindow.xaml`：月历主界面
- `Views/StickyNoteWindow.xaml`：侧边便签窗口

## 本次版本更新

### v1.4.2

- 修复侧边便签内容在关闭程序后丢失的问题，改为每次编辑即时保存纯文本
- 新增多处保存触发点：编辑时、窗口隐藏时、关闭时、托盘退出时
- 便签内容持久化文件改为 `sticky-note-content.txt`（BOM-free UTF-8）
- 简化便签界面，移除图片相关提示（纯文字便签）
- 精简 `StickyNoteStateService`，去除不稳定的格式序列化，改用纯文本 I/O

### v1.4.1

- 修复主界面点击右上角 `X` 后，便签与主界面托盘联动不一致的问题
- 修复双击系统托盘图标时，主界面恢复但便签未同步恢复的问题
- 优化主界面与便签的成组显示和隐藏行为
- 保留并强化便签自动保存逻辑
- 修正项目元数据中的乱码标题与说明

### v1.4.0

- 新增紧贴主界面的侧边便签窗口
- 新增便签相对位置与大小记忆
- 新增便签粘贴文字与图片支持
- 为今日、逾期、本周等列表视图新增快速添加按钮
- 优化便签首次显示与贴边逻辑

## 敏感信息检查

发布前已对仓库进行常见敏感信息扫描，结果如下：

- 未发现硬编码 API Key
- 未发现硬编码 Token
- 未发现私钥文件内容
- `.codex/` 未纳入版本控制
- `AGENTS.md` 未纳入版本控制
