# DesktopMemoWin

DesktopMemoWin is a lightweight WPF desktop memo app for Windows. It keeps a calendar-style memo board on the desktop, supports fast daily task management, and now includes a sticky side note panel that can stay attached to the main window.

## Current Version

- Version: `v1.4.0`
- Platform: `Windows x64`
- Runtime: `.NET 8`
- Database: `SQLite`

## Features

- Monthly calendar view for daily memos
- List views for today, overdue, this week, recycle bin, and more
- Daily task window for focused one-day management
- Quick add buttons in list views
- Completed items move to the end and use a different visual style
- Priority and pin support
- Recycle bin with restore and clear actions
- System tray integration
- Startup on boot option
- Data import, export, backup, and restore
- Sticky side note panel
  - docked beside the main window by default
  - resizable and draggable
  - remembers relative size and position
  - supports scrolling
  - supports pasted text and images

## Storage Paths

- Database: `%USERPROFILE%\DesktopMemo\memo.db`
- Config: `%USERPROFILE%\DesktopMemo\config.txt`
- Sticky note layout: `%USERPROFILE%\DesktopMemo\sticky-note-layout.json`
- Sticky note content: `%USERPROFILE%\DesktopMemo\sticky-note-content.xamlpkg`

## Database Schema

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

## Build and Run

### Debug

```powershell
dotnet build DesktopMemo.csproj -c Debug
dotnet run --project DesktopMemo.csproj
```

### Release

```powershell
dotnet publish DesktopMemo.csproj -c Release -r win-x64 --self-contained true
```

Default published executable:

```text
bin\Release\net8.0-windows\win-x64\publish\DesktopMemo.exe
```

## Project Structure

- `Models`
- `Services`
- `ViewModels`
- `Views`
- `Converters`
- `DesktopMemo.csproj`

## Key Modules

- `Services/DatabaseService.cs`: SQLite access, import/export, backup/restore
- `Services/SystemTrayService.cs`: tray icon behavior
- `Services/DesktopMonitorService.cs`: desktop visibility monitoring
- `Services/StartupService.cs`: startup registration
- `Services/StickyNoteStateService.cs`: sticky note persistence
- `ViewModels/MainViewModel.cs`: main app logic
- `Views/MainWindow.xaml`: main calendar UI
- `Views/StickyNoteWindow.xaml`: sticky side note UI

## Security Check

This release was checked for common sensitive information patterns before publishing:

- no hard-coded API keys found
- no hard-coded tokens found
- no private keys found
- `.codex/` is excluded from version control
- `AGENTS.md` is excluded from version control

## Changelog

### v1.4.0

- Added a sticky side note window attached to the main window
- Added sticky note size and relative position persistence
- Added sticky note support for pasted images and text
- Added quick add buttons to list-based views
- Improved first-launch sticky note display behavior
- Adjusted sticky note docking so it sits flush against the main window

### v1.3.0

- Tightened the main window and dialog layouts
- Unified typography across windows
- Fixed multiple garbled UI texts
- Improved completed-task sorting and styling
- Improved calendar density on different display sizes
