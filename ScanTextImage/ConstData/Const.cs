using ScanTextImage.Model;
using ScanTextImage.View.Command;
using ScanTextImage.View.Helper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ScanTextImage.ConstData
{
    public static class Const
    {
        public static readonly Dictionary<string, string> SpecialMapLangCodeName = new Dictionary<string, string>
        {
            { "chi_sim", "Chinese (Simplified)" },
            { "chi_tra", "Chinese (Traditional)" },
        };

        //public static readonly Dictionary<string, string> MapAbbreviateCountry = new Dictionary<string, string>
        //{
        //    { "Chinese (Simplified)", "zh_CN" },
        //    { "Chinese (Traditional)", "zh_TW" },
        //};

        public static readonly Dictionary<string, string> MapModifierKey = new Dictionary<string, string>
        {
            { ModifierKeys.None.ToString(), "" },
            { ModifierKeys.Control.ToString(), "Ctrl" },
            { ModifierKeys.Alt.ToString(), "Alt" },
            { ModifierKeys.Shift.ToString(), "Shift" },
            { Key.D1.ToString(), "1" },
            { Key.D2.ToString(), "2" },
            { Key.D3.ToString(), "3" },
            { Key.D4.ToString(), "4" },
            { Key.D5.ToString(), "5" },
            { Key.D6.ToString(), "6" },
            { Key.D7.ToString(), "7" },
            { Key.D8.ToString(), "8" },
            { Key.D9.ToString(), "9" },
            { Key.D0.ToString(), "0" },
        };

        public static readonly Dictionary<string, string> MapKeyNumber = new Dictionary<string, string>
        {
            { "1", Key.D1.ToString() },
            { "2", Key.D2.ToString() },
            { "3", Key.D3.ToString() },
            { "4", Key.D4.ToString() },
            { "5", Key.D5.ToString() },
            { "6", Key.D6.ToString() },
            { "7", Key.D7.ToString() },
            { "8", Key.D8.ToString() },
            { "9", Key.D9.ToString() },
            { "0", Key.D0.ToString() }
        };

        public static readonly Dictionary<string, string> MapCommandEventName = new Dictionary<string, string>
        {
            { "Clear", "CommandBindingClear_Executed" },
            { "Save", "CommandBindingCreateDataSave_Executed" },
            { "View image", "CommandBindingViewImage_Executed" },
            { "Translate image", "CommandBindingtranslateImage_Executed" },
            { "Translate text", "CommandBindingtranslateText_Executed" },
            { "Free selection", "CommandBindingFreeSelection_Executed" },
            { "loadSave", "CommandBindingLoadData_Executed" },
        };


        public static readonly string clearShortcutName = "Clear";
        public static readonly string saveShortcutName = "Save";
        public static readonly string viewImageShortcutName = "View_image";
        public static readonly string translateImageShortcutName = "Translate_image";
        public static readonly string translateTextShortcutName = "Translate_text";
        public static readonly string freeSelectionShortcutName = "Free_selection";
        public static readonly string loadSaveShortcutName = "loadSave";

        public static readonly string regexNameLoadSaveCommand = "^loadSave(?'numberLoad'[1-9])$";

        public static readonly List<MenuModel> MenuModels = new List<MenuModel>()
        {
            new MenuModel()
            {
                headerMenu = "File",
                shortCutMenuDisplay = string.Empty,
                childMenuModels = new List<MenuModel>()
                {
                    new MenuModel()
                    {
                        headerMenu = "Save",
                        eventNames = new List<string>
                        {
                            "saveDataBtn_Click"
                        },
                        //ShortcutModel = ShortcutHelper.GetShortcutModel(nameof(CustomCommand.createDataSave), "_Save"),
                        //shortCutMenuDisplay = ShortcutHelper.GetShortcutText(nameof(CustomCommand.createDataSave)),
                    },
                    new MenuModel()
                    {
                        headerMenu = "Exit",
                        shortCutMenuDisplay = "Alt + F4",
                        eventNames = new List<string>
                        {
                            "menuExit_Click"
                        }
                    },
                }
            },
            new MenuModel()
            {
                headerMenu = "Tools",
                shortCutMenuDisplay = string.Empty,
                childMenuModels = new List<MenuModel>()
                {
                    new MenuModel()
                    {
                        headerMenu = "Mini mode",
                        shortCutMenuDisplay = "",
                        eventNames = new List<string>
                        {
                            "miniBtn_Click"
                        }
                    },
                    new MenuModel()
                    {
                        headerMenu = "Free selection",
                        eventNames = new List<string>
                        {
                            "freeSelection_Click"
                        },
                        //ShortcutModel = ShortcutHelper.GetShortcutModel(nameof(CustomCommand.freeSelection), "_Free selection"),
                        //shortCutMenuDisplay = ShortcutHelper.GetShortcutText(nameof(CustomCommand.freeSelection)),
                    },
                    new MenuModel()
                    {
                        headerMenu = "Translate text",
                        eventNames = new List<string>
                        {
                            "translate_Click"
                        },
                        //ShortcutModel = ShortcutHelper.GetShortcutModel(nameof(CustomCommand.translateText), "_Translate text"),
                        //shortCutMenuDisplay = ShortcutHelper.GetShortcutText(nameof(CustomCommand.translateText)),
                    },
                    new MenuModel()
                    {
                        headerMenu = "Translate image",
                        eventNames = new List<string>
                        {
                            "translateImage_Click"
                        },
                        //ShortcutModel = ShortcutHelper.GetShortcutModel(nameof(CustomCommand.translateImage), "_Translate image"),
                        //shortCutMenuDisplay = ShortcutHelper.GetShortcutText(nameof(CustomCommand.translateImage)),
                    },
                    new MenuModel()
                    {
                        headerMenu = "View image",
                        eventNames = new List<string>
                        {
                            "viewImageBtn_Click"
                        },
                        //ShortcutModel = ShortcutHelper.GetShortcutModel(nameof(CustomCommand.viewImage), "_View image"),
                        //shortCutMenuDisplay = ShortcutHelper.GetShortcutText(nameof(CustomCommand.viewImage)),
                    },
                    new MenuModel()
                    {
                        headerMenu = "Clear",
                        eventNames = new List<string>
                        {
                            "clearBtn_Click"
                        },
                        //ShortcutModel = ShortcutHelper.GetShortcutModel(nameof(CustomCommand.clear), "_Clear"),
                        //shortCutMenuDisplay = ShortcutHelper.GetShortcutText(nameof(CustomCommand.clear)),
                    },
                }
            },
            new MenuModel()
            {
                headerMenu = "System",
                shortCutMenuDisplay = string.Empty,
                childMenuModels = new List<MenuModel>()
                {
                    new MenuModel()
                    {
                        headerMenu = "Config Shortcut",
                        eventNames = new List<string>
                        {
                            "configBtn_Click"
                        },
                        shortCutMenuDisplay = ""
                    },
                }
            },
        };

        public static readonly string pathSaveData = @"./Data";
        public static readonly string pathConfigData = @"./Config/config_shortcut.json";

        public static readonly string tessdataPath = @"./tessdata";

        public static readonly string regexSaveFileName = @"(?:.*)data_(?'idSave'[1-9]).json";

        public static readonly Double miniWidth = 658;
        public static readonly Double miniHeight = 200;
        public static readonly Double miniCollapseWidth = 25;
        public static readonly Double miniCollapseHeight = 75;

        public static readonly string placeholderTextFrom = "From...";
        public static readonly string placeholderTextTo = "To...";
    }
}
