using Dapper;
using DesktopMemo.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DesktopMemo.Services
{
    /// <summary>
    /// SQLite 数据库服务。
    /// </summary>
    public class DatabaseService
    {
        private const string MemoSelectColumns = @"
            Id,
            Content,
            Date,
            IsCompleted,
            CreatedAt,
            CompletedAt,
            SortOrder,
            Priority,
            IsPinned,
            IsDeleted";

        private const string ActiveMemoOrderBy = @"
            ORDER BY IsCompleted ASC, IsPinned DESC, Priority DESC, Date ASC, SortOrder ASC, CreatedAt ASC";

        private const string RecycleMemoOrderBy = @"
            ORDER BY Date DESC, IsPinned DESC, Priority DESC, CreatedAt DESC";

        private readonly string _dbPath;
        private readonly string _connectionString;

        public DatabaseService()
        {
            var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appFolder = Path.Combine(userFolder, "DesktopMemo");

            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            _dbPath = Path.Combine(appFolder, "memo.db");
            _connectionString = $"Data Source={_dbPath};Version=3;";

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = OpenConnection();

            const string createTableSql = @"
                CREATE TABLE IF NOT EXISTS MemoItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Content TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    IsCompleted INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    CompletedAt TEXT,
                    SortOrder INTEGER NOT NULL DEFAULT 0,
                    Priority INTEGER NOT NULL DEFAULT 0,
                    IsPinned INTEGER NOT NULL DEFAULT 0,
                    IsDeleted INTEGER NOT NULL DEFAULT 0
                )";

            connection.Execute(createTableSql);
            EnsureColumnExists(connection, "Priority", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumnExists(connection, "IsPinned", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumnExists(connection, "IsDeleted", "INTEGER NOT NULL DEFAULT 0");
        }

        private static void EnsureColumnExists(SQLiteConnection connection, string columnName, string columnDefinition)
        {
            var columns = connection.Query<TableInfo>("PRAGMA table_info(MemoItems)").Select(c => c.Name).ToList();
            if (columns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            connection.Execute($"ALTER TABLE MemoItems ADD COLUMN {columnName} {columnDefinition}");
        }

        public List<MemoItem> GetMemosByDate(DateTime date, bool includeDeleted = false)
        {
            try
            {
                using var connection = OpenConnection();
                var dateStr = date.ToString("yyyy-MM-dd");
                var sql = $@"
                    SELECT {MemoSelectColumns}
                    FROM MemoItems
                    WHERE Date = @Date {(includeDeleted ? string.Empty : "AND IsDeleted = 0")}
                    {ActiveMemoOrderBy}";

                return connection.Query<MemoItemDto>(sql, new { Date = dateStr }).Select(MapMemo).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取日期备忘录失败: {ex.Message}", ex);
            }
        }

        public List<MemoItem> GetAllMemos(bool includeDeleted = false)
        {
            try
            {
                using var connection = OpenConnection();
                var sql = $@"
                    SELECT {MemoSelectColumns}
                    FROM MemoItems
                    {(includeDeleted ? string.Empty : "WHERE IsDeleted = 0")}
                    {ActiveMemoOrderBy}";

                return connection.Query<MemoItemDto>(sql).Select(MapMemo).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取全部备忘录失败: {ex.Message}", ex);
            }
        }

        public List<MemoItem> GetRecycleBinMemos()
        {
            try
            {
                using var connection = OpenConnection();
                var sql = $@"
                    SELECT {MemoSelectColumns}
                    FROM MemoItems
                    WHERE IsDeleted = 1
                    {RecycleMemoOrderBy}";

                return connection.Query<MemoItemDto>(sql).Select(MapMemo).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取回收站备忘录失败: {ex.Message}", ex);
            }
        }

        public void AddMemo(MemoItem memo)
        {
            try
            {
                using var connection = OpenConnection();

                const string sql = @"
                    INSERT INTO MemoItems (Content, Date, IsCompleted, CreatedAt, CompletedAt, SortOrder, Priority, IsPinned, IsDeleted)
                    VALUES (@Content, @Date, @IsCompleted, @CreatedAt, @CompletedAt, @SortOrder, @Priority, @IsPinned, @IsDeleted);
                    SELECT last_insert_rowid();";

                var newId = connection.ExecuteScalar<long>(sql, new
                {
                    memo.Content,
                    Date = memo.Date.ToString("yyyy-MM-dd"),
                    IsCompleted = memo.IsCompleted ? 1 : 0,
                    CreatedAt = memo.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    CompletedAt = memo.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                    memo.SortOrder,
                    memo.Priority,
                    IsPinned = memo.IsPinned ? 1 : 0,
                    IsDeleted = memo.IsDeleted ? 1 : 0
                });

                memo.Id = (int)newId;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"添加备忘录失败: {ex.Message}", ex);
            }
        }

        public void UpdateMemo(MemoItem memo)
        {
            using var connection = OpenConnection();

            const string sql = @"
                UPDATE MemoItems
                SET Content = @Content,
                    Date = @Date,
                    IsCompleted = @IsCompleted,
                    CreatedAt = @CreatedAt,
                    CompletedAt = @CompletedAt,
                    SortOrder = @SortOrder,
                    Priority = @Priority,
                    IsPinned = @IsPinned,
                    IsDeleted = @IsDeleted
                WHERE Id = @Id";

            connection.Execute(sql, new
            {
                memo.Id,
                memo.Content,
                Date = memo.Date.ToString("yyyy-MM-dd"),
                IsCompleted = memo.IsCompleted ? 1 : 0,
                CreatedAt = memo.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                CompletedAt = memo.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                memo.SortOrder,
                memo.Priority,
                IsPinned = memo.IsPinned ? 1 : 0,
                IsDeleted = memo.IsDeleted ? 1 : 0
            });
        }

        public MemoItem? GetMemoById(int id)
        {
            try
            {
                using var connection = OpenConnection();
                var sql = $@"
                    SELECT {MemoSelectColumns}
                    FROM MemoItems
                    WHERE Id = @Id";

                var dto = connection.QueryFirstOrDefault<MemoItemDto>(sql, new { Id = id });
                return dto == null ? null : MapMemo(dto);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取备忘录详情失败: {ex.Message}", ex);
            }
        }

        public void UpdateMemoSortOrders(IEnumerable<MemoItem> memos)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            const string sql = @"
                UPDATE MemoItems
                SET SortOrder = @SortOrder
                WHERE Id = @Id";

            foreach (var memo in memos.Where(m => m.Id > 0))
            {
                connection.Execute(sql, new { memo.Id, memo.SortOrder }, transaction);
            }

            transaction.Commit();
        }

        public void DeleteMemo(int id)
        {
            using var connection = OpenConnection();
            connection.Execute("UPDATE MemoItems SET IsDeleted = 1 WHERE Id = @Id", new { Id = id });
        }

        public void RestoreMemo(int id)
        {
            using var connection = OpenConnection();
            connection.Execute("UPDATE MemoItems SET IsDeleted = 0 WHERE Id = @Id", new { Id = id });
        }

        public void PermanentlyDeleteMemo(int id)
        {
            using var connection = OpenConnection();
            connection.Execute("DELETE FROM MemoItems WHERE Id = @Id", new { Id = id });
        }

        public int PurgeDeletedMemos()
        {
            using var connection = OpenConnection();
            return connection.Execute("DELETE FROM MemoItems WHERE IsDeleted = 1");
        }

        public void ClearAllMemos()
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            connection.Execute("DELETE FROM MemoItems", transaction: transaction);
            connection.Execute("DELETE FROM sqlite_sequence WHERE name = 'MemoItems'", transaction: transaction);

            transaction.Commit();
        }

        public void MarkAsCompleted(int id)
        {
            using var connection = OpenConnection();
            const string sql = @"
                UPDATE MemoItems
                SET IsCompleted = 1,
                    CompletedAt = @CompletedAt
                WHERE Id = @Id";

            connection.Execute(sql, new
            {
                Id = id,
                CompletedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        public void SetPinned(int id, bool isPinned)
        {
            using var connection = OpenConnection();
            connection.Execute("UPDATE MemoItems SET IsPinned = @IsPinned WHERE Id = @Id", new
            {
                Id = id,
                IsPinned = isPinned ? 1 : 0
            });
        }

        public void SetPriority(int id, int priority)
        {
            using var connection = OpenConnection();
            connection.Execute("UPDATE MemoItems SET Priority = @Priority WHERE Id = @Id", new
            {
                Id = id,
                Priority = priority
            });
        }

        public string GetDatabasePath()
        {
            return _dbPath;
        }

        public void ImportDatabase(string sourcePath)
        {
            try
            {
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException("源数据库文件不存在", sourcePath);
                }

                if (!IsValidSQLiteDatabase(sourcePath))
                {
                    throw new InvalidOperationException("源文件不是有效的 SQLite 数据库文件");
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                var backupPath = _dbPath + ".backup";
                if (File.Exists(_dbPath))
                {
                    File.Copy(_dbPath, backupPath, true);
                }

                try
                {
                    File.Copy(sourcePath, _dbPath, true);

                    if (!IsValidSQLiteDatabase(_dbPath))
                    {
                        if (File.Exists(backupPath))
                        {
                            File.Copy(backupPath, _dbPath, true);
                        }

                        throw new InvalidOperationException("导入后的数据库文件无效，已恢复原数据库");
                    }

                    InitializeDatabase();
                }
                catch
                {
                    if (File.Exists(backupPath))
                    {
                        File.Copy(backupPath, _dbPath, true);
                    }

                    throw;
                }
                finally
                {
                    if (File.Exists(backupPath))
                    {
                        try
                        {
                            File.Delete(backupPath);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"导入数据库失败: {ex.Message}", ex);
            }
        }

        public void ExportDatabase(string targetPath)
        {
            try
            {
                if (!File.Exists(_dbPath))
                {
                    throw new FileNotFoundException("数据库文件不存在", _dbPath);
                }

                var targetDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.Copy(_dbPath, targetPath, true);

                if (!IsValidSQLiteDatabase(targetPath))
                {
                    throw new InvalidOperationException("导出的数据库文件无效");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"导出数据库失败: {ex.Message}", ex);
            }
        }

        private bool IsValidSQLiteDatabase(string dbPath)
        {
            try
            {
                var connectionString = $"Data Source={dbPath};Version=3;";
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                var tableExists = connection.QueryFirstOrDefault<int>(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='MemoItems'");

                return tableExists > 0;
            }
            catch
            {
                return false;
            }
        }

        public void ReloadDatabase()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                InitializeDatabase();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"重新加载数据库失败: {ex.Message}", ex);
            }
        }

        private SQLiteConnection OpenConnection()
        {
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private static MemoItem MapMemo(MemoItemDto dto)
        {
            return new MemoItem
            {
                Id = dto.Id,
                Content = dto.Content,
                Date = ParseDate(dto.Date),
                IsCompleted = dto.IsCompleted == 1,
                CreatedAt = ParseDateTime(dto.CreatedAt),
                CompletedAt = string.IsNullOrWhiteSpace(dto.CompletedAt) ? null : ParseDateTime(dto.CompletedAt),
                SortOrder = dto.SortOrder,
                Priority = dto.Priority,
                IsPinned = dto.IsPinned == 1,
                IsDeleted = dto.IsDeleted == 1
            };
        }

        private static DateTime ParseDate(string value)
        {
            return DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static DateTime ParseDateTime(string value)
        {
            return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }
    }

    internal class MemoItemDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public int IsCompleted { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? CompletedAt { get; set; }
        public int SortOrder { get; set; }
        public int Priority { get; set; }
        public int IsPinned { get; set; }
        public int IsDeleted { get; set; }
    }

    internal class TableInfo
    {
        public string Name { get; set; } = string.Empty;
    }
}
