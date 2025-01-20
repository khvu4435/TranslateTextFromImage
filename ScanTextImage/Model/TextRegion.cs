

using System.Windows;
using System.Windows.Shapes;

namespace ScanTextImage.Model
{
    public class TextRegion
    {
        public string Text { get; set; }
        public string TranslationText { get; set; } = string.Empty;
        public Rect Bounds { get; set; }
        public Rect OriginBounds { get; set; }
        public Rectangle Highlight { get; set; }
        public bool IsSelected { get; set; }
    }
}
