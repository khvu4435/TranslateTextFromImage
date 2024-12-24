using ScanTextImage.ConstData;
using ScanTextImage.Model;
using ScanTextImage.View.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ScanTextImage.View.Helper
{
    public class ShortcutHelper
    {
        //public static string GetShortcutText(string name)
        //{
        //    string key;
        //    List<string> modifirers = new();
        //    GetCmdShortcutByName(name, out key, out modifirers);

        //    string modifirerKey = string.Empty;
        //    foreach (var modifier in modifirers)
        //    {
        //        if (Const.MapModifierKey.TryGetValue(modifier, out string? value))
        //        {
        //            modifirerKey = modifirerKey + (string.IsNullOrWhiteSpace(value) ? "" : value + " + ");
        //        }

        //    }

        //    var shortcut = modifirerKey + key;

        //    return shortcut;
        //}

        //public static ShortcutModel GetShortcutModel(string name, string displayName)
        //{
        //    string key;
        //    List<string> modifirers = new();
        //    GetCmdShortcutByName(name, out key, out modifirers);

        //    ShortcutModel model = new ShortcutModel
        //    {
        //        DisplayName = displayName,
        //        IsControlKey = modifirers.Contains(ModifierKeys.Control.ToString(), StringComparer.OrdinalIgnoreCase),
        //        IsShiftKey = modifirers.Contains(ModifierKeys.Shift.ToString(), StringComparer.OrdinalIgnoreCase),
        //        IsAltKey = modifirers.Contains(ModifierKeys.Alt.ToString(), StringComparer.OrdinalIgnoreCase),
        //        Key = key
        //    };

        //    return model;
        //}

        //private static void GetCmdShortcutByName(string name, out string key, out List<string> modifirers)
        //{
        //    var typeCustomCmd = typeof(CustomCommand);
        //    FieldInfo field = typeCustomCmd.GetField(name, BindingFlags.Static | BindingFlags.Public);

        //    if (field == null)
        //    {
        //        throw new ArgumentException($"Static field '{name}' not found in {typeCustomCmd.Name}");
        //    }

        //    var cmd = field.GetValue(null) as RoutedUICommand;

        //    if (cmd == null)
        //    {
        //        throw new ArgumentException($"Static field '{name}' not have any value in {typeCustomCmd.Name}");
        //    }

        //    if (cmd.InputGestures.Count <= 0)
        //    {
        //        throw new ArgumentException($"Static field '{name}' not set any input gesture in {typeCustomCmd.Name}");
        //    }

        //    if (cmd.InputGestures[0] == null)
        //    {
        //        throw new ArgumentException($"Static field '{name}' has null input gesture in {typeCustomCmd.Name}");
        //    }

        //    // get the first getsture
        //    var keyGetsture = (KeyGesture)cmd.InputGestures[0];

        //    key = keyGetsture.Key.ToString();
        //    modifirers = keyGetsture.Modifiers.ToString().Split(", ", StringSplitOptions.RemoveEmptyEntries).ToList();
        //}

        //public static List<ShortcutModel> GetListCmdShortcut()
        //{
        //    var typeCustomCmd = typeof(CustomCommand);
        //    var fields = typeCustomCmd.GetFields(BindingFlags.Static | BindingFlags.Public);

        //    if (fields == null)
        //    {
        //        throw new ArgumentException($"Static fields not found in {typeCustomCmd.Name}");
        //    }

        //    var listShortcut = new List<ShortcutModel>();
        //    foreach(var field in fields)
        //    {
        //        var data = field.GetValue(null) as RoutedUICommand;
        //        if (data != null && data.Name != "cancelScreenShot")
        //        {
        //            var keyGetsture = (KeyGesture)data.InputGestures[0];
        //            var key = Const.MapModifierKey.TryGetValue(keyGetsture.Key.ToString(), out string? value) ? value : keyGetsture.Key.ToString();
        //            var modifirers = keyGetsture.Modifiers.ToString().Split(", ", StringSplitOptions.RemoveEmptyEntries).ToList();

        //            listShortcut.Add(new ShortcutModel
        //            {
        //                DisplayName = data.Name,
        //                IsControlKey = modifirers.Contains(ModifierKeys.Control.ToString(), StringComparer.OrdinalIgnoreCase),
        //                IsShiftKey = modifirers.Contains(ModifierKeys.Shift.ToString(), StringComparer.OrdinalIgnoreCase),
        //                IsAltKey = modifirers.Contains(ModifierKeys.Alt.ToString(), StringComparer.OrdinalIgnoreCase),
        //                Key = key
        //            });
        //        }

        //    }

        //    return listShortcut;
        //}

    }
}