using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Dapper;
using DesktopMemo.Models;

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
            using var connection = new SQLiteConnection(_connectionString);
            var dateStr = date.ToString("yyyy-MM-dd");
            
            var sql = @"
                SELECT Id, Content, Date, IsCompleted, CreatedAt, CompletedAt, SortOrder
                FROM MemoItems 
                WHERE Date = @Date 
                ORDER BY IsCompleted ASC, SortOrder ASC, CreatedAt ASC";
            
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

        /// <summary>
        /// 添加备忘录
        /// </summary>
        /// <param name="memo">备忘录项目</param>
        public void AddMemo(MemoItem memo)
        {
            using var connection = new SQLiteConnection(_connectionString);
            
            var sql = @"
                INSERT INTO MemoItems (Content, Date, IsCompleted, CreatedAt, CompletedAt, SortOrder)
                VALUES (@Content, @Date, @IsCompleted, @CreatedAt, @CompletedAt, @SortOrder)";
            
            connection.Execute(sql, new
            {
                memo.Content,
                Date = memo.Date.ToString("yyyy-MM-dd"),
                memo.IsCompleted,
                CreatedAt = memo.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                CompletedAt = memo.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                memo.SortOrder
            });
        }

        /// <summary>
        /// 更新备忘录
        /// </summary>
        /// <param name="memo">备忘录项目</param>
        public void UpdateMemo(MemoItem memo)
        {
            using var connection = new SQLiteConnection(_connectionString);
            
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

        /// <summary>
        /// 删除备忘录
        /// </summary>
        /// <param name="id">备忘录ID</param>
        public void DeleteMemo(int id)
        {
            using var connection = new SQLiteConnection(_connectionString);
            
            var sql = "DELETE FROM MemoItems WHERE Id = @Id";
            connection.Execute(sql, new { Id = id });
        }

        /// <summary>
        /// 标记备忘录为已完成
        /// </summary>
        /// <param name="id">备忘录ID</param>
        public void MarkAsCompleted(int id)
        {
            using var connection = new SQLiteConnection(_connectionString);
            
            var sql = @"
                UPDATE MemoItems 
                SET IsCompleted = 1, CompletedAt = @CompletedAt, SortOrder = (SELECT COALESCE(MAX(SortOrder), 0) + 1 FROM MemoItems)
                WHERE Id = @Id";
            
            connection.Execute(sql, new
            {
                Id = id,
                CompletedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
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
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, _dbPath, true);
            }
        }

        /// <summary>
        /// 导出数据库文件
        /// </summary>
        /// <param name="targetPath">目标数据库文件路径</param>
        public void ExportDatabase(string targetPath)
        {
            if (File.Exists(_dbPath))
            {
                File.Copy(_dbPath, targetPath, true);
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