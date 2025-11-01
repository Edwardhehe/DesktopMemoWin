# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Build and Run
```bash
# Build using the provided script (recommended)
.\build.bat

# Build using dotnet CLI
dotnet build DesktopMemo.csproj -c Release

# Run the application
dotnet run

# Create installer package
.\create_installer.bat
```

### Development Workflow
```bash
# Restore dependencies
dotnet restore

# Clean build artifacts
dotnet clean

# Build in debug mode
dotnet build -c Debug

# Build for release
dotnet build -c Release --self-contained -r win-x64
```

## Architecture Overview

This is a **WPF desktop memo application** built with .NET 8 that displays a calendar-based memo system on the Windows desktop. The application follows **MVVM architecture pattern** with clear separation of concerns.

### Core Components

#### Data Layer
- **Database**: SQLite database stored in `%USERPROFILE%\DesktopMemo\memo.db`
- **ORM**: Dapper for database operations
- **Model**: [`MemoItem.cs`](Models/MemoItem.cs) - Main data entity with INotifyPropertyChanged

#### Service Layer
- **[`DatabaseService.cs`](Services/DatabaseService.cs)**: Handles all SQLite CRUD operations, data import/export, and backup/restore
- **[`DesktopMonitorService.cs`](Services/DesktopMonitorService.cs)**: Monitors desktop visibility to show/hide application intelligently
- **[`SystemTrayService.cs`](Services/SystemTrayService.cs)**: Manages system tray integration with context menus
- **[`StartupService.cs`](Services/StartupService.cs)**: Handles Windows startup registry management

#### ViewModel Layer
- **[`MainViewModel.cs`](ViewModels/MainViewModel.cs)**: Primary viewmodel containing:
  - Calendar data management with `CalendarDayViewModel` collections
  - Command implementations for memo operations (add, delete, complete, etc.)
  - Desktop monitoring integration
  - Configuration management (background color, startup settings)

#### View Layer
- **[`MainWindow.xaml`](Views/MainWindow.xaml)**: Main calendar interface showing monthly memo grid
- **[`DailyTasksWindow.xaml`](Views/DailyTasksWindow.xaml)**: Dedicated window for managing single-day tasks
- **[`MemoDetailWindow.xaml`](Views/MemoDetailWindow.xaml)**: Memo detail viewer with metadata
- **[`MemoInputDialog.xaml`](Views/MemoInputDialog.xaml)**: Dialog for creating/editing memos
- **[`SettingsWindow.xaml`](Views/SettingsWindow.xaml)**: Application settings and data management

### Key Architecture Patterns

#### MVVM Implementation
- ViewModels implement `INotifyPropertyChanged` for data binding
- Commands use `RelayCommand` and `RelayCommand<T>` implementations
- Views bind to ViewModels through DataContext properties

#### Data Flow
1. **User Actions** → Commands in ViewModel → Service calls → Database operations
2. **Database Changes** → Service events → ViewModel updates → UI refreshes via data binding
3. **Desktop State Changes** → DesktopMonitorService events → ViewModel properties → UI visibility changes

#### Memory Management
- [`App.xaml.cs`](App.xaml.cs) provides application-level exception handling
- Services implement proper cleanup patterns
- Event subscription cleanup in application exit

## Key Features

### Smart Desktop Display
- Application only shows when user returns to desktop (via DesktopMonitorService)
- Minimizes to system tray when not on desktop
- Prevents interference with other applications

### Calendar-Based Organization
- Monthly calendar view with memo items in date cells
- Automatic memo sorting (uncompleted items first, then completed)
- Date-based memo grouping and filtering

### Data Management
- SQLite database with automatic schema initialization
- Backup/restore functionality through DatabaseService
- Configuration persistence in user profile directory

## Development Guidelines

### Adding New Features
1. **Data Changes**: Extend `MemoItem` model and update `DatabaseService` methods
2. **UI Changes**: Create new XAML views and corresponding ViewModels following MVVM pattern
3. **Business Logic**: Implement in appropriate Service classes or ViewModels
4. **Commands**: Use `RelayCommand` or `RelayCommand<T>` for user interactions

### Database Modifications
- Update schema in `DatabaseService.InitializeDatabase()`
- Modify corresponding DTO classes and SQL queries
- Test with existing data migration scenarios

### Exception Handling
- Follow the existing pattern in `App.xaml.cs` for application-level exceptions
- Service methods should throw meaningful exceptions with context
- ViewModel methods catch exceptions and provide user feedback

### Testing and Debugging
- Debug output is sent to `System.Diagnostics.Debug.WriteLine()`
- Database path: `%USERPROFILE%\DesktopMemo\memo.db`
- Config file: `%USERPROFILE%\DesktopMemo\config.txt`

## File Structure Notes

### Generated Files
- XAML generated files (*.g.cs, *.g.i.cs) are in `obj/` directories
- These are auto-generated and should not be manually edited

### Build Artifacts
- Release builds target `win-x64` platform
- Self-contained deployment includes .NET 8 runtime
- Final executable: `bin\Release\net8.0-windows\win-x64\DesktopMemo.exe`

### Dependencies
- **Microsoft.Data.Sqlite**: SQLite database provider
- **Dapper**: Lightweight ORM for database operations
- **System.Data.SQLite**: Additional SQLite support
- **WPF & WindowsForms**: UI frameworks (WPF primary, WindowsForms for system integration)