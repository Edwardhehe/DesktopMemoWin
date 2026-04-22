using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DesktopMemo.Models
{
    /// <summary>
    /// 备忘录数据模型。
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
        private int _priority;
        private bool _isPinned;
        private bool _isDeleted;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged();
            }
        }

        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged();
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                OnPropertyChanged();
            }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                _createdAt = value;
                OnPropertyChanged();
            }
        }

        public DateTime? CompletedAt
        {
            get => _completedAt;
            set
            {
                _completedAt = value;
                OnPropertyChanged();
            }
        }

        public int SortOrder
        {
            get => _sortOrder;
            set
            {
                _sortOrder = value;
                OnPropertyChanged();
            }
        }

        public int Priority
        {
            get => _priority;
            set
            {
                _priority = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PriorityText));
                OnPropertyChanged(nameof(PriorityBadge));
            }
        }

        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                _isPinned = value;
                OnPropertyChanged();
            }
        }

        public bool IsDeleted
        {
            get => _isDeleted;
            set
            {
                _isDeleted = value;
                OnPropertyChanged();
            }
        }

        public string PriorityText => Priority switch
        {
            2 => "高",
            1 => "中",
            _ => "低"
        };

        public string PriorityBadge => Priority switch
        {
            2 => "高",
            1 => "中",
            _ => string.Empty
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
