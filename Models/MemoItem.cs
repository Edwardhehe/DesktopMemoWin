using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DesktopMemo.Models
{
    /// <summary>
    /// 备忘录项目数据模型
    /// </summary>
    public class MemoItem : INotifyPropertyChanged
    {
        private int _id;
        private string _content = string.Empty;
        private DateTime _date;
        private bool _isCompleted;
        private DateTime _createdAt;
        private DateTime? _completedAt;
        private int _sortOrder;

        /// <summary>
        /// 备忘录ID
        /// </summary>
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 备忘录内容
        /// </summary>
        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 备忘录日期
        /// </summary>
        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                _createdAt = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompletedAt
        {
            get => _completedAt;
            set
            {
                _completedAt = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder
        {
            get => _sortOrder;
            set
            {
                _sortOrder = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 属性变化事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变化事件
        /// </summary>
        /// <param name="propertyName">属性名</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}