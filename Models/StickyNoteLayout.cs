namespace DesktopMemo.Models
{
    /// <summary>
    /// 侧边便签相对主窗口的布局信息。
    /// </summary>
    public class StickyNoteLayout
    {
        public double WidthRatio { get; set; } = 0.42;

        public double HeightRatio { get; set; } = 0.52;

        // Default to an edge-aligned note whose right edge touches the main window.
        public double OffsetXRatio { get; set; } = -0.42;

        public double OffsetYRatio { get; set; } = 0.02;
    }
}
