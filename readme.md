# DesktopMemoWin

桌面备忘录是一个基于 .NET 8 和 WPF 的 Windows 桌面便签工具，主界面直接以月历形式展示每天的待办事项，支持桌面常驻、系统托盘、回收站、优先级、置顶和数据备份恢复。

## 当前版本

- 版本：`v1.3.0`
- 平台：`Windows x64`
- 运行时：`.NET 8`
- 数据库：`SQLite`

## 主要功能

- 月历视图：每天用日期格子展示备忘录，支持快速新增、双击查看详情。
- 列表视图：按筛选条件查看备忘录，适合集中整理。
- 当日任务窗口：查看指定日期任务并直接操作完成状态、优先级、置顶。
- 新增与编辑：支持修改内容、日期、优先级、置顶状态。
- 完成态排序：未完成项目优先，已完成项目自动变色并排到后面。
- 回收站：支持软删除、恢复和清空回收站。
- 系统托盘：关闭主窗体后隐藏到托盘，不打断桌面使用。
- 开机启动：可在设置中启用。
- 数据管理：支持导出、导入和数据库备份恢复。

## 界面说明

### 主界面

- 顶部集成月份切换、视图切换、筛选和设置入口。
- 月历格子默认按 4 条任务的视觉密度布局，避免多余留白。
- 主界面会根据屏幕分辨率自动调整尺寸，尽量贴近内容高度。

### 弹窗

- 当日任务
- 新增备忘录
- 备忘录详情
- 设置

以上窗口都已统一为更紧凑的标题栏、字号和按钮尺寸，便于日常快速操作。

## 数据存储

- 数据库路径：`%USERPROFILE%\DesktopMemo\memo.db`
- 配置文件路径：`%USERPROFILE%\DesktopMemo\config.txt`

### MemoItems 表结构

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

### 排序规则

1. 未完成项目优先
2. 同状态下按 `SortOrder` 升序
3. 同排序下按 `CreatedAt` 升序

## 构建与运行

### 调试运行

```powershell
dotnet build DesktopMemo.csproj -c Debug
dotnet run --project DesktopMemo.csproj
```

### 发布构建

```powershell
dotnet build DesktopMemo.csproj -c Release --self-contained -r win-x64
```

发布后的可执行文件默认位于：

```text
bin\Release\net8.0-windows\win-x64\DesktopMemo.exe
```

## 项目结构

```text
DesktopMemoWin
├─ Models
├─ Services
├─ ViewModels
├─ Views
├─ Converters
└─ DesktopMemo.csproj
```

### 关键模块

- `Services/DatabaseService.cs`：SQLite 数据访问、导入导出、备份恢复
- `Services/SystemTrayService.cs`：系统托盘逻辑
- `Services/DesktopMonitorService.cs`：桌面显示状态监控
- `Services/StartupService.cs`：开机启动管理
- `ViewModels/MainViewModel.cs`：主业务逻辑与命令
- `Views/MainWindow.xaml`：主月历界面

## 安全检查

发布前已检查以下内容：

- 未发现硬编码 API Key、Token、私钥或密码
- 未将本地 `.codex/` 目录纳入版本库
- 未将 `AGENTS.md` 发布到仓库

## 版本记录

- `v1.3.0`
  - 收紧主界面和弹窗布局，减少空白区域
  - 统一字体层级和窗口尺寸
  - 修复右键菜单乱码与若干界面文案问题
  - 优化已完成任务排序与显示状态
  - 调整月历格子密度，提升不同分辨率下的显示效果
- `v1.2.0`
  - 添加完成时间跟踪
- `v1.1.0`
  - 添加排序字段
- `v1.0.0`
  - 初始版本
