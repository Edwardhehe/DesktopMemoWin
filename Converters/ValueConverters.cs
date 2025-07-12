using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace DesktopMemo.Converters
{
    /// <summary>
    /// 布尔值到可见性转换器
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 转换方法
        /// </summary>
        /// <param name="value">源值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        /// <summary>
        /// 反向转换方法
        /// </summary>
        /// <param name="value">目标值</param>
        /// <param name="targetType">源类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// 反向布尔值到可见性转换器
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 转换方法
        /// </summary>
        /// <param name="value">源值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        /// <summary>
        /// 反向转换方法
        /// </summary>
        /// <param name="value">目标值</param>
        /// <param name="targetType">源类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed;
            }
            return true;
        }
    }

    /// <summary>
    /// 布尔值到透明度转换器
    /// </summary>
    public class BoolToOpacityConverter : IValueConverter
    {
        /// <summary>
        /// 转换方法
        /// </summary>
        /// <param name="value">源值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 1.0 : 0.3;
            }
            return 0.3;
        }

        /// <summary>
        /// 反向转换方法
        /// </summary>
        /// <param name="value">目标值</param>
        /// <param name="targetType">源类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return doubleValue > 0.5;
            }
            return false;
        }
    }

    /// <summary>
    /// 布尔值到前景色转换器
    /// </summary>
    public class BoolToForegroundConverter : IValueConverter
    {
        /// <summary>
        /// 转换方法
        /// </summary>
        /// <param name="value">源值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Brushes.Black : Brushes.Gray;
            }
            return Brushes.Gray;
        }

        /// <summary>
        /// 反向转换方法
        /// </summary>
        /// <param name="value">目标值</param>
        /// <param name="targetType">源类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到备忘录样式转换器
    /// </summary>
    public class BoolToMemoStyleConverter : IValueConverter
    {
        // 静态样式缓存，避免重复创建
        private static Style? _completedMemoStyle;
        private static Style? _normalMemoStyle;

        /// <summary>
        /// 转换方法
        /// </summary>
        /// <param name="value">源值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // 查找应用程序资源中的样式
                var app = Application.Current;
                if (app?.Resources != null)
                {
                    string styleKey = boolValue ? "CompletedMemoStyle" : "MemoItemStyle";
                    if (app.Resources.Contains(styleKey))
                    {
                        return app.Resources[styleKey];
                    }
                }

                // 如果找不到样式，返回缓存的默认样式
                return boolValue ? GetCompletedMemoStyle() : GetNormalMemoStyle();
            }
            return GetNormalMemoStyle();
        }

        /// <summary>
        /// 获取已完成的备忘录样式
        /// </summary>
        /// <returns>样式对象</returns>
        private static Style GetCompletedMemoStyle()
        {
            if (_completedMemoStyle == null)
            {
                _completedMemoStyle = CreateDefaultStyle(true);
            }
            return _completedMemoStyle;
        }

        /// <summary>
        /// 获取普通备忘录样式
        /// </summary>
        /// <returns>样式对象</returns>
        private static Style GetNormalMemoStyle()
        {
            if (_normalMemoStyle == null)
            {
                _normalMemoStyle = CreateDefaultStyle(false);
            }
            return _normalMemoStyle;
        }

        /// <summary>
        /// 创建默认样式
        /// </summary>
        /// <param name="isCompleted">是否已完成</param>
        /// <returns>样式对象</returns>
        private static Style CreateDefaultStyle(bool isCompleted)
        {
            var style = new Style(typeof(Border));

            if (isCompleted)
            {
                style.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(232, 245, 232))));
                style.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(144, 238, 144))));
            }
            else
            {
                style.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(248, 249, 250))));
                style.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(209, 213, 219))));
            }

            style.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Border.MarginProperty, new Thickness(2)));
            style.Setters.Add(new Setter(Border.PaddingProperty, new Thickness(6)));
            style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(4)));

            return style;
        }

        /// <summary>
        /// 反向转换方法
        /// </summary>
        /// <param name="value">目标值</param>
        /// <param name="targetType">源类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到文本装饰转换器
    /// </summary>
    public class BoolToTextDecorationConverter : IValueConverter
    {
        /// <summary>
        /// 转换方法
        /// </summary>
        /// <param name="value">源值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TextDecorations.Strikethrough : null;
            }
            return null;
        }

        /// <summary>
        /// 反向转换方法
        /// </summary>
        /// <param name="value">目标值</param>
        /// <param name="targetType">源类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 减去转换器
    /// </summary>
    public class SubtractConverter : IValueConverter
    {
        /// <summary>
        /// 转换方法
        /// </summary>
        /// <param name="value">源值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue && parameter is string paramString)
            {
                if (double.TryParse(paramString, out double subtractValue))
                {
                    return doubleValue - subtractValue;
                }
            }
            return value;
        }

        /// <summary>
        /// 反向转换方法
        /// </summary>
        /// <param name="value">目标值</param>
        /// <param name="targetType">源类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 文本截断转换器
    /// </summary>
    public class TextTruncateConverter : IValueConverter
    {
        /// <summary>
        /// 转换方法
        /// </summary>
        /// <param name="value">源值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数（最大字符数，默认20）</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                // 获取最大字符数参数，默认为20
                int maxLength = 20;
                if (parameter is string paramString && int.TryParse(paramString, out int paramLength))
                {
                    maxLength = paramLength;
                }

                // 如果文本长度超过最大长度，则截断并添加省略号
                if (text.Length > maxLength)
                {
                    return text.Substring(0, maxLength) + "...";
                }

                return text;
            }
            return value ?? string.Empty;
        }

        /// <summary>
        /// 反向转换方法
        /// </summary>
        /// <param name="value">目标值</param>
        /// <param name="targetType">源类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 当天日期格子样式转换器
    /// </summary>
    public class TodayCalendarDayStyleConverter : IValueConverter
    {
        // 静态样式缓存，避免重复创建
        private static Style? _todayStyle;
        private static Style? _normalStyle;

        /// <summary>
        /// 转换方法
        /// </summary>
        /// <param name="value">源值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // 查找应用程序资源中的样式
                var app = Application.Current;
                if (app?.Resources != null)
                {
                    string styleKey = boolValue ? "TodayCalendarDayStyle" : "CalendarDayStyle";
                    if (app.Resources.Contains(styleKey))
                    {
                        return app.Resources[styleKey];
                    }
                }

                // 如果找不到样式，返回缓存的默认样式
                return boolValue ? GetTodayStyle() : GetNormalStyle();
            }
            return GetNormalStyle();
        }

        /// <summary>
        /// 获取当天日期格子样式
        /// </summary>
        /// <returns>样式对象</returns>
        private static Style GetTodayStyle()
        {
            if (_todayStyle == null)
            {
                _todayStyle = CreateTodayStyle();
            }
            return _todayStyle;
        }

        /// <summary>
        /// 获取普通日期格子样式
        /// </summary>
        /// <returns>样式对象</returns>
        private static Style GetNormalStyle()
        {
            if (_normalStyle == null)
            {
                _normalStyle = CreateNormalStyle();
            }
            return _normalStyle;
        }

        /// <summary>
        /// 创建当天日期格子样式
        /// </summary>
        /// <returns>样式对象</returns>
        private static Style CreateTodayStyle()
        {
            var style = new Style(typeof(Border));
            style.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(179, 227, 242, 253))));
            style.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(74, 144, 226))));
            style.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2)));
            style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(8)));
            style.Setters.Add(new Setter(Border.MarginProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Border.MinHeightProperty, 95.0));
            style.Setters.Add(new Setter(Border.PaddingProperty, new Thickness(4)));
            return style;
        }

        /// <summary>
        /// 创建普通日期格子样式
        /// </summary>
        /// <returns>样式对象</returns>
        private static Style CreateNormalStyle()
        {
            var style = new Style(typeof(Border));
            style.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(102, 255, 255, 255))));
            style.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Colors.Transparent)));
            style.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(0)));
            style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(8)));
            style.Setters.Add(new Setter(Border.MarginProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Border.MinHeightProperty, 95.0));
            style.Setters.Add(new Setter(Border.PaddingProperty, new Thickness(4)));
            return style;
        }

        /// <summary>
        /// 反向转换方法
        /// </summary>
        /// <param name="value">目标值</param>
        /// <param name="targetType">源类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 当天前景色转换器
    /// </summary>
    public class TodayForegroundConverter : IValueConverter
    {
        /// <summary>
        /// 转换方法
        /// </summary>
        /// <param name="value">源值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? new SolidColorBrush(Color.FromRgb(74, 144, 226)) : Brushes.Black;
            }
            return Brushes.Black;
        }

        /// <summary>
        /// 反向转换方法
        /// </summary>
        /// <param name="value">目标值</param>
        /// <param name="targetType">源类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到字体粗细转换器
    /// </summary>
    public class BoolToFontWeightConverter : IValueConverter
    {
        /// <summary>
        /// 转换方法
        /// </summary>
        /// <param name="value">源值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? FontWeights.Bold : FontWeights.Normal;
            }
            return FontWeights.Normal;
        }

        /// <summary>
        /// 反向转换方法
        /// </summary>
        /// <param name="value">目标值</param>
        /// <param name="targetType">源类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换结果</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}