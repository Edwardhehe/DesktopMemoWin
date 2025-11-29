# 桌面备忘录数据库格式说明

## 概述

桌面备忘录应用程序使用SQLite数据库存储所有备忘录数据。本文档详细说明了数据库结构、字段定义和数据格式，以便其他UI程序能够正确读取和操作数据。

## 数据库信息

- **数据库类型**: SQLite 3
- **数据库文件位置**: `%USERPROFILE%\DesktopMemo\memo.db`
- **数据库名称**: 无特定名称，使用文件名作为标识
- **编码**: UTF-8

## 数据库表结构

### MemoItems 表

存储所有备忘录项目的主表。

#### 表结构定义

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

#### 字段详细说明

| 字段名 | 数据类型 | 约束 | 说明 |
|--------|----------|------|------|
| `Id` | INTEGER | PRIMARY KEY AUTOINCREMENT | 备忘录唯一标识符，自增主键 |
| `Content` | TEXT | NOT NULL | 备忘录内容文本 |
| `Date` | TEXT | NOT NULL | 备忘录日期，格式：`yyyy-MM-dd` |
| `IsCompleted` | INTEGER | NOT NULL DEFAULT 0 | 完成状态：0=未完成，1=已完成 |
| `CreatedAt` | TEXT | NOT NULL | 创建时间，格式：`yyyy-MM-dd HH:mm:ss` |
| `CompletedAt` | TEXT | NULL | 完成时间，格式：`yyyy-MM-dd HH:mm:ss`，未完成时为NULL |
| `SortOrder` | INTEGER | NOT NULL DEFAULT 0 | 排序顺序，用于控制备忘录显示顺序 |

## 数据格式规范

### 日期时间格式

- **日期字段**: `Date` - `yyyy-MM-dd` 格式
  - 示例：`2024-01-15`

- **时间字段**: `CreatedAt`, `CompletedAt` - `yyyy-MM-dd HH:mm:ss` 格式
  - 示例：`2024-01-15 14:30:25`

### 布尔值处理

- `IsCompleted` 字段使用 INTEGER 类型：
  - `0` = 未完成 (false)
  - `1` = 已完成 (true)

### 排序规则

备忘录按以下规则排序：
1. **完成状态优先**：未完成项目在前，已完成项目在后
2. **排序顺序**：相同状态下，按 `SortOrder` 升序排列
3. **创建时间**：相同排序顺序下，按 `CreatedAt` 升序排列

## SQL 查询示例

### 基础查询

```sql
-- 获取所有备忘录
SELECT Id, Content, Date, IsCompleted, CreatedAt, CompletedAt, SortOrder
FROM MemoItems
ORDER BY IsCompleted ASC, SortOrder ASC, CreatedAt ASC;
```

### 按日期查询

```sql
-- 获取指定日期的所有备忘录
SELECT Id, Content, Date, IsCompleted, CreatedAt, CompletedAt, SortOrder
FROM MemoItems
WHERE Date = '2024-01-15'
ORDER BY IsCompleted ASC, SortOrder ASC, CreatedAt ASC;
```

### 获取今日备忘录

```sql
-- 获取今日所有备忘录
SELECT Id, Content, Date, IsCompleted, CreatedAt, CompletedAt, SortOrder
FROM MemoItems
WHERE Date = date('now')
ORDER BY IsCompleted ASC, SortOrder ASC, CreatedAt ASC;
```

### 获取本周备忘录

```sql
-- 获取本周所有备忘录
SELECT Id, Content, Date, IsCompleted, CreatedAt, CompletedAt, SortOrder
FROM MemoItems
WHERE Date >= date('now', 'weekday 0', '-6 days')
AND Date <= date('now', 'weekday 0')
ORDER BY Date ASC, IsCompleted ASC, SortOrder ASC;
```

### 获取未完成备忘录

```sql
-- 获取所有未完成的备忘录
SELECT Id, Content, Date, IsCompleted, CreatedAt, CompletedAt, SortOrder
FROM MemoItems
WHERE IsCompleted = 0
ORDER BY Date ASC, SortOrder ASC, CreatedAt ASC;
```

## 数据操作示例

### 插入新备忘录

```sql
INSERT INTO MemoItems (Content, Date, IsCompleted, CreatedAt, CompletedAt, SortOrder)
VALUES ('完成项目报告', '2024-01-15', 0, '2024-01-15 09:00:00', NULL, 0);
```

### 更新备忘录内容

```sql
UPDATE MemoItems
SET Content = '完成季度财务报告'
WHERE Id = 1;
```

### 标记备忘录为已完成

```sql
UPDATE MemoItems
SET IsCompleted = 1, CompletedAt = '2024-01-15 16:30:00'
WHERE Id = 1;
```

### 删除备忘录

```sql
DELETE FROM MemoItems
WHERE Id = 1;
```

### 更新排序顺序

```sql
-- 批量更新排序顺序
UPDATE MemoItems
SET SortOrder = CASE Id
    WHEN 1 THEN 0
    WHEN 2 THEN 1
    WHEN 3 THEN 2
    ELSE SortOrder
END
WHERE Id IN (1, 2, 3);
```

## 编程语言示例

### C# 示例

```csharp
using System.Data.SQLite;

// 读取备忘录
string connectionString = "Data Source=memo.db;Version=3;";
using (var connection = new SQLiteConnection(connectionString))
{
    connection.Open();

    string sql = @"
        SELECT Id, Content, Date, IsCompleted, CreatedAt, CompletedAt, SortOrder
        FROM MemoItems
        ORDER BY IsCompleted ASC, SortOrder ASC, CreatedAt ASC";

    using (var command = new SQLiteCommand(sql, connection))
    using (var reader = command.ExecuteReader())
    {
        while (reader.Read())
        {
            int id = reader.GetInt32(0);
            string content = reader.GetString(1);
            DateTime date = DateTime.Parse(reader.GetString(2));
            bool isCompleted = reader.GetInt32(3) == 1;
            DateTime createdAt = DateTime.Parse(reader.GetString(4));
            DateTime? completedAt = reader.IsDBNull(5) ? (DateTime?)null : DateTime.Parse(reader.GetString(5));
            int sortOrder = reader.GetInt32(6);

            Console.WriteLine($"ID: {id}, 内容: {content}, 日期: {date:yyyy-MM-dd}, 完成: {isCompleted}");
        }
    }
}
```

### Python 示例

```python
import sqlite3
from datetime import datetime

# 连接数据库
conn = sqlite3.connect('memo.db')
cursor = conn.cursor()

# 查询备忘录
cursor.execute("""
    SELECT Id, Content, Date, IsCompleted, CreatedAt, CompletedAt, SortOrder
    FROM MemoItems
    ORDER BY IsCompleted ASC, SortOrder ASC, CreatedAt ASC
""")

for row in cursor.fetchall():
    id, content, date_str, is_completed, created_at_str, completed_at_str, sort_order = row

    # 转换数据类型
    date = datetime.strptime(date_str, '%Y-%m-%d').date()
    created_at = datetime.strptime(created_at_str, '%Y-%m-%d %H:%M:%S')
    completed_at = datetime.strptime(completed_at_str, '%Y-%m-%d %H:%M:%S') if completed_at_str else None

    print(f"ID: {id}, 内容: {content}, 日期: {date}, 完成: {bool(is_completed)}")

conn.close()
```

### JavaScript (Node.js) 示例

```javascript
const sqlite3 = require('sqlite3').verbose();
const db = new sqlite3.Database('memo.db');

// 查询备忘录
db.all(`
    SELECT Id, Content, Date, IsCompleted, CreatedAt, CompletedAt, SortOrder
    FROM MemoItems
    ORDER BY IsCompleted ASC, SortOrder ASC, CreatedAt ASC
`, (err, rows) => {
    if (err) {
        console.error(err);
        return;
    }

    rows.forEach(row => {
        const memo = {
            id: row.Id,
            content: row.Content,
            date: row.Date,
            isCompleted: row.IsCompleted === 1,
            createdAt: row.CreatedAt,
            completedAt: row.CompletedAt,
            sortOrder: row.SortOrder
        };
        console.log(memo);
    });
});

db.close();
```

## 注意事项

1. **数据库访问**: 数据库文件可能被桌面备忘录应用程序锁定，建议在访问前确保应用程序已关闭
2. **并发访问**: 不建议多个程序同时写入数据库，可能导致数据损坏
3. **备份**: 在进行任何数据操作前，建议先备份数据库文件
4. **字符编码**: 所有文本数据使用UTF-8编码
5. **事务处理**: 对于重要操作，建议使用数据库事务确保数据一致性

## 数据库验证

检查数据库文件是否有效的SQL：

```sql
-- 检查表是否存在
SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='MemoItems';

-- 检查表结构
PRAGMA table_info(MemoItems);

-- 检查数据完整性
SELECT COUNT(*) FROM MemoItems;
```

## 版本历史

- **v1.0.0**: 初始版本，包含基本的备忘录功能
- **v1.1.0**: 添加排序功能 (SortOrder字段)
- **v1.2.0**: 添加完成时间跟踪 (CompletedAt字段)

---

如有任何疑问或需要进一步的数据库信息，请联系开发者。