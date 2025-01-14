using ScanTextImage.ConstData;
using System.Windows;
using System.Windows.Input;

namespace ScanTextImage.View.Command
{
    public static class CustomCommand
    {
        public static RoutedUICommand freeSelectionCommand = new RoutedUICommand(
            Const.freeSelectionShortcutName,
            Const.freeSelectionShortcutName,
            typeof(Window),
            new InputGestureCollection
            {
                        new KeyGesture(Key.F1, ModifierKeys.Control)
            });

        public static RoutedUICommand translateTextCommand = new RoutedUICommand(
            Const.translateTextShortcutName,
            Const.translateTextShortcutName,
            typeof(Window),
            new InputGestureCollection
            {
                        new KeyGesture(Key.F2, ModifierKeys.Control)
            });

        public static RoutedUICommand translateImageCommand = new RoutedUICommand(
            Const.translateImageShortcutName,
            Const.translateImageShortcutName,
            typeof(Window),
            new InputGestureCollection
            {
                        new KeyGesture(Key.F3, ModifierKeys.Control)
            });

        public static RoutedUICommand viewImageCommand = new RoutedUICommand(
            Const.viewImageShortcutName,
            Const.viewImageShortcutName,
            typeof(Window),
            new InputGestureCollection
            {
                        new KeyGesture(Key.F4, ModifierKeys.Control)
            });

        public static RoutedUICommand clearCommand = new RoutedUICommand(
            Const.clearShortcutName,
            Const.clearShortcutName,
            typeof(Window),
            new InputGestureCollection
            {
                         new KeyGesture(Key.F5, ModifierKeys.Control)
            });

        public static RoutedUICommand saveDataCommand = new RoutedUICommand(
            Const.saveShortcutName,
            Const.saveShortcutName,
            typeof(Window),
            new InputGestureCollection
            {
                        new KeyGesture(Key.S, ModifierKeys.Control)
            });

        public static RoutedUICommand loadData1Command = new RoutedUICommand(
            Const.loadSaveShortcutName + "1",
            Const.loadSaveShortcutName + "1",
            typeof(Window),
            new InputGestureCollection
            {
                        new KeyGesture(Key.D1, ModifierKeys.Control)
            });

        public static RoutedUICommand loadData2Command = new RoutedUICommand(
            Const.loadSaveShortcutName + "2",
            Const.loadSaveShortcutName + "2",
            typeof(Window),
            new InputGestureCollection
            {
                        new KeyGesture(Key.D2, ModifierKeys.Control)
            });

        public static RoutedUICommand loadData3Command = new RoutedUICommand(
            Const.loadSaveShortcutName + "3",
            Const.loadSaveShortcutName + "3",
            typeof(Window),
            new InputGestureCollection
            {
                        new KeyGesture(Key.D3, ModifierKeys.Control)
            });

        public static RoutedUICommand loadData4Command = new RoutedUICommand(
            Const.loadSaveShortcutName + "4",
            Const.loadSaveShortcutName + "4",
            typeof(Window),
            new InputGestureCollection
            {
                        new KeyGesture(Key.D4, ModifierKeys.Control)
            });

        public static RoutedUICommand loadData5Command = new RoutedUICommand(
            Const.loadSaveShortcutName + "5",
            Const.loadSaveShortcutName + "5",
            typeof(Window),
            new InputGestureCollection
            {
                        new KeyGesture(Key.D5, ModifierKeys.Control)
            });



        public static RoutedUICommand loadData6Command = new RoutedUICommand(
            Const.loadSaveShortcutName + "6",
            Const.loadSaveShortcutName + "6",
            typeof(Window),
            new InputGestureCollection
            {
                        new KeyGesture(Key.D6, ModifierKeys.Control)
            });


        public static RoutedUICommand loadData7Command = new RoutedUICommand(
            Const.loadSaveShortcutName + "7",
            Const.loadSaveShortcutName + "7",
            typeof(Window),
            new InputGestureCollection
            {
                        new KeyGesture(Key.D7, ModifierKeys.Control)
            });



        public static RoutedUICommand loadData8Command = new RoutedUICommand(
            Const.loadSaveShortcutName + "8",
            Const.loadSaveShortcutName + "8",
            typeof(Window),
            new InputGestureCollection
            {
                    new KeyGesture(Key.D8, ModifierKeys.Control)
            });


        public static RoutedUICommand loadData9Command = new RoutedUICommand(
            Const.loadSaveShortcutName + "9",
            Const.loadSaveShortcutName + "9",
            typeof(Window),
            new InputGestureCollection
            {
                    new KeyGesture(Key.D9, ModifierKeys.Control)
            });

    }
}

