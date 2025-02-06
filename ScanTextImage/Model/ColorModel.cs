using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.Model
{
    public class ColorModel
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public override string ToString() => $"RGB({R}, {G}, {B})";
        public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

        public double GetBrightness()
        {
            return (0.299 * R + 0.587 * G + 0.114 * B);
        }
    }
}
