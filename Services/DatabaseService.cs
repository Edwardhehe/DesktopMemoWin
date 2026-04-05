using Dapper;
using DesktopMemo.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace DesktopMemo.Services
{
    /// <summary>
    /// 数据库服务类，负责SQLite数据库操作
    /// </summary>
    public class DatabaseService
    {
        private readonly string _dbPath;
        private readonly string _connectionString;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DatabaseService()
        {
            // 数据库文件存储在用户文件夹
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

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private void InitializeDatabase()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // 创建备忘录表
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS MemoItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Content TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    IsCompleted INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    CompletedAt TEXT,
                    SortOrder INTEGER NOT NULL DEFAULT 0
                )";

            connection.Execute(createTableSql);
        }

        /// <summary>
        /// 获取指定日期的备忘录
        /// </summary>
        /// <param name="date">日期</param>
        /// <returns>备忘录列表</returns>
        public List<MemoItem> GetMemosByDate(DateTime date)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                var dateStr = date.ToString("yyyy-MM-dd");

                var sql = @"
                    SELECT Id, Content, Date, IsCompleted, CreatedAt, CompletedAt, SortOrder
                    FROM MemoItems 
                    WHERE Date = @Date 
                    ORDER BY IsCompleted ASC, CreatedAt ASC, SortOrder ASC";

                var memos = connection.Query<MemoItemDto>(sql, new { Date = dateStr }).Select(dto => new MemoItem
                {
                    Id = dto.Id,
                    Content = dto.Content,
                    Date = DateTime.Parse(dto.Date),
                    IsCompleted = dto.IsCompleted == 1,
                    CreatedAt = DateTime.Parse(dto.CreatedAt),
                    CompletedAt = string.IsNullOrEmpty(dto.CompletedAt) ? null : DateTime.Parse(dto.CompletedAt),
                    SortOrder = dto.SortOrder
                }).ToList();

                return memos;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取备忘录数据失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 添加备忘录
        /// </summary>
        /// <param name="memo">备忘录项目</param>
        public void AddMemo(MemoItem memo)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();

                var sql = @"
                    INSERT INTO MemoItems (Content, Date, IsCompleted, CreatedAt, CompletedAt, SortOrder)
                    VALUES (@Content, @Date, @IsCompleted, @CreatedAt, @CompletedAt, @SortOrder);
                    SELECT last_insert_rowid();";

                var newId = connection.ExecuteScalar<long>(sql, new
                {
                    memo.Content,
                    Date = memo.Date.ToString("yyyy-MM-dd"),
                    memo.IsCompleted,
                    CreatedAt = memo.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    CompletedAt = memo.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                    memo.SortOrder
                });

                memo.Id = (int)newId;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"添加备忘录失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新备忘录
        /// </summary>
        /// <param name="memo">备忘录项目</param>
        public void UpdateMemo(MemoItem memo)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var sql = @"
                UPDATE MemoItems 
                SET Content = @Content, Date = @Date, IsCompleted = @IsCompleted, 
                    CreatedAt = @CreatedAt, CompletedAt = @CompletedAt, SortOrder = @SortOrder
                WHERE Id = @Id";

            connection.Execute(sql, new
            {
                memo.Id,
                memo.Content,
                Date = memo.Date.ToString("yyyy-MM-dd"),
                memo.IsCompleted,
                CreatedAt = memo.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                CompletedAt = memo.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                memo.SortOrder
            });
        }

        public MemoItem? GetMemoById(int id)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();

                const string sql = @"
                    SELECT Id, Content, Date, IsCompleted, CreatedAt, CompletedAt, SortOrder
                    FROM MemoItems
                    WHERE Id = @Id";

                var dto = connection.QueryFirstOrDefault<MemoItemDto>(sql, new { Id = id });
                if (dto == null)
                {
                    return null;
                }

                return new MemoItem
                {
                    Id = dto.Id,
                    Content = dto.Content,
                    Date = DateTime.Parse(dto.Date),
                    IsCompleted = dto.IsCompleted == 1,
                    CreatedAt = DateTime.Parse(dto.CreatedAt),
                    CompletedAt = string.IsNullOrEmpty(dto.CompletedAt) ? null : DateTime.Parse(dto.CompletedAt),
                    SortOrder = dto.SortOrder
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取备忘录详情失败: {ex.Message}", ex);
            }
        }

        public void UpdateMemoSortOrders(IEnumerable<MemoItem> memos)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
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

        /// <summary>
        /// 删除备忘录
        /// </summary>
        /// <param name="id">备忘录ID</param>
        public void DeleteMemo(int id)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var sql = "DELETE FROM MemoItems WHERE Id = @Id";
            connection.Execute(sql, new { Id = id });
        }

        public void ClearAllMemos()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            connection.Execute("DELETE FROM MemoItems", transaction: transaction);
            connection.Execute("DELETE FROM sqlite_sequence WHERE name = 'MemoItems'", transaction: transaction);
            transaction.Commit();
        }

        /// <summary>
        /// 标记备忘录为已完成
        /// </summary>
        /// <param name="id">备忘录ID</param>
        public void MarkAsCompleted(int id)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // 先获取该备忘录的日期信息
            var memoSql = "SELECT Date FROM MemoItems WHERE Id = @Id";
            var memoDate = connection.QueryFirstOrDefault<string>(memoSql, new { Id = id });

            if (string.IsNullOrEmpty(memoDate))
                return;

            // 获取该日期下已完成项目的最大SortOrder
            var maxOrderSql = @"
                SELECT COALESCE(MAX(SortOrder), 0)
                FROM MemoItems
                WHERE Date = @Date AND IsCompleted = 1";
            var maxOrder = connection.QueryFirstOrDefault<int>(maxOrderSql, new { Date = memoDate });

            // 更新备忘录状态和排序
            var updateSql = @"
                UPDATE MemoItems
                SET IsCompleted = 1, CompletedAt = @CompletedAt, SortOrder = @MaxOrder + 1
                WHERE Id = @Id";

            connection.Execute(updateSql, new
            {
                Id = id,
                CompletedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                MaxOrder = maxOrder
            });
        }

        /// <summary>
        /// 获取数据库文件路径
        /// </summary>
        /// <returns>数据库文件路径</returns>
        public string GetDatabasePath()
        {
            return _dbPath;
        }

        /// <summary>
        /// 导入数据库文件
        /// </summary>
        /// <param name="sourcePath">源数据库文件路径</param>
        public void ImportDatabase(string sourcePath)
        {
            try
            {
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException("源数据库文件不存在", sourcePath);
                }

                // 验证源数据库文件是否为有效的SQLite数据库
                if (!IsValidSQLiteDatabase(sourcePath))
                {
                    throw new InvalidOperationException("源文件不是有效的SQLite数据库文件");
                }

                // 关闭所有数据库连接
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // 备份当前数据库
                string backupPath = _dbPath + ".backup";
                if (File.Exists(_dbPath))
                {
                    File.Copy(_dbPath, backupPath, true);
                }

                try
                {
                    // 复制新数据库文件
                    File.Copy(sourcePath, _dbPath, true);

                    // 验证新数据库
                    if (!IsValidSQLiteDatabase(_dbPath))
                    {
                        // 如果新数据库无效，恢复备份
                        if (File.Exists(backupPath))
                        {
                            File.Copy(backupPath, _dbPath, true);
                        }
                        throw new InvalidOperationException("导入的数据库文件无效，已恢复原数据库");
                    }

                    // 重新初始化数据库连接
                    InitializeDatabase();
                }
                catch
                {
                    // 如果导入失败，恢复备份
                    if (File.Exists(backupPath))
                    {
                        File.Copy(backupPath, _dbPath, true);
                    }
                    throw;
                }
                finally
                {
                    // 清理备份文件
                    if (File.Exists(backupPath))
                    {
                        try
                        {
                            File.Delete(backupPath);
                        }
                        catch
                        {
                            // 忽略清理备份文件的异常
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"导入数据库失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 导出数据库文件
        /// </summary>
        /// <param name="targetPath">目标数据库文件路径</param>
        public void ExportDatabase(string targetPath)
        {
            try
            {
                if (!File.Exists(_dbPath))
                {
                    throw new FileNotFoundException("数据库文件不存在", _dbPath);
                }

                // 确保目标目录存在
                var targetDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                // 复制数据库文件
                File.Copy(_dbPath, targetPath, true);

                // 验证导出的文件
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

        /// <summary>
        /// 验证SQLite数据库文件是否有效
        /// </summary>
        /// <param name="dbPath">数据库文件路径</param>
        /// <returns>是否为有效的SQLite数据库</returns>
        private bool IsValidSQLiteDatabase(string dbPath)
        {
            try
            {
                var connectionString = $"Data Source={dbPath};Version=3;";
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                // 检查表是否存在
                var tableExists = connection.QueryFirstOrDefault<int>(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='MemoItems'");

                return tableExists > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 重新加载数据库连接
        /// </summary>
        public void ReloadDatabase()
        {
            try
            {
                // 强制垃圾回收，释放所有数据库连接
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // 重新初始化数据库
                InitializeDatabase();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"重新加载数据库失败: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// 数据库传输对象，用于Dapper查询
    /// </summary>
    internal class MemoItemDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public int IsCompleted { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? CompletedAt { get; set; }
        public int SortOrder { get; set; }
    }
}
