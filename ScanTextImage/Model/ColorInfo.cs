using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.Model
{
    public class ColorInfo
    {
        public ColorModel Color { get; set; }
        public double Percentage { get; set; }
        public bool IsDark { get; set; }
    }
}
