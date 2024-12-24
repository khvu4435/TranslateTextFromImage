using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanTextImage.Model
{
    public class MenuModel
    {
        public string headerMenu { get; set; } = string.Empty;
        public ShortcutModel ShortcutModel { get; set; } = new();
        public string shortCutMenuDisplay { get; set; } = string.Empty;
        public List<string> eventNames { get; set; } = new List<string>();
        public List<MenuModel> childMenuModels { get; set; } = new List<MenuModel>();
    }
}
