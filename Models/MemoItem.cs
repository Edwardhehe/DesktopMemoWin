using System;

namespace DesktopMemo.Models
{
    /// <summary>
    /// 备忘录项目数据模型
    /// </summary>
    public class MemoItem
    {
        /// <summary>
        /// 备忘录ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// 备忘录内容
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// 备忘录日期
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        
        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder { get; set; }
    }
} 