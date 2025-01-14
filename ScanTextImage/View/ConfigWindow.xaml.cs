using ScanTextImage.Interface;
using ScanTextImage.Model;
using Serilog;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScanTextImage.View
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        #region Service
        private ISaveDataService _saveDataService;
        #endregion

        #region window
        private MainWindow _mainWindow;
        #endregion

        private ObservableCollection<ShortcutModel> shortcutModels;

        private TextBox currFocusTextBox = null;


        public ConfigWindow(ISaveDataService saveDataService, MainWindow mainWindow)
        {
            InitializeComponent();

            _saveDataService = saveDataService;
            _mainWindow = mainWindow;

            this.Owner = mainWindow;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var dataShortcuts = _saveDataService.GetShortcutConfig();
            shortcutModels = new ObservableCollection<ShortcutModel>(dataShortcuts);
            lvConfigShortcut.ItemsSource = shortcutModels;

            LoadCheckBoxAll(dataShortcuts);
        }

        #region btn event
        private void btnCancelConfigShortcut_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSaveConfigShortcut_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("start btnSaveConfigShortcut_Click");
            try
            {
                var data = lvConfigShortcut.ItemsSource as ObservableCollection<ShortcutModel>;

                if (data == null)
                {
                    throw new Exception("Data shortcut config is empty");
                }

                var listUpdate = data.ToList();
                _saveDataService.SaveShortcut(listUpdate);

                var msgboxResult = MessageBox.Show("Save config shortcut sucessfull!", "Save shortcut sucess", MessageBoxButton.OK, MessageBoxImage.Information);
                if (msgboxResult == MessageBoxResult.OK)
                {
                    _mainWindow.LoadCommandBinding();
                    _mainWindow.LoadMenuItems();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error Config Shortcut", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Log.Information("end btnSaveConfigShortcut_Click");

        }

        #endregion

        #region check box unchecked & checked

        private void cbCtrlAll_Checked(object sender, RoutedEventArgs e)
        {
            Log.Information("start cbCtrlAll_Checked");
            try
            {
                UpdateModifierKyOfShortcut(nameof(ShortcutModel.IsControlKey), true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when click check box all - ctrl");
                MessageBox.Show("Error when config shortcut: " + ex.Message, "Error Config Shortcut", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Log.Information("start cbCtrlAll_Checked");
        }
        private void cbCtrlAll_Unchecked(object sender, RoutedEventArgs e)
        {
            Log.Information("start cbCtrlAll_Unchecked");
            try
            {
                UpdateModifierKyOfShortcut(nameof(ShortcutModel.IsControlKey), false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when click check box all - ctrl");
                MessageBox.Show("Error when config shortcut: " + ex.Message, "Error Config Shortcut", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Log.Information("end cbCtrlAll_Unchecked");
        }

        private void cbShiftAll_Checked(object sender, RoutedEventArgs e)
        {
            Log.Information("start cbShiftAll_Checked");
            try
            {
                UpdateModifierKyOfShortcut(nameof(ShortcutModel.IsShiftKey), true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when click check box all - shift");
                MessageBox.Show("Error when config shortcut: " + ex.Message, "Error Config Shortcut", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Log.Information("end cbShiftAll_Checked");
        }

        private void cbShiftAll_Unchecked(object sender, RoutedEventArgs e)
        {
            Log.Information("start cbShiftAll_Unchecked");
            try
            {
                UpdateModifierKyOfShortcut(nameof(ShortcutModel.IsShiftKey), false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when click check box all - shift");
                MessageBox.Show("Error when config shortcut: " + ex.Message, "Error Config Shortcut", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Log.Information("end cbShiftAll_Unchecked");
        }

        private void cbAltAll_Checked(object sender, RoutedEventArgs e)
        {
            Log.Information("start cbAltAll_Checked");
            try
            {
                UpdateModifierKyOfShortcut(nameof(ShortcutModel.IsAltKey), true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when click check box all - alt");
                MessageBox.Show("Error when config shortcut: " + ex.Message, "Error Config Shortcut", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Log.Information("end cbAltAll_Checked");
        }

        private void cbAltAll_Unchecked(object sender, RoutedEventArgs e)
        {
            Log.Information("start cbAltAll_Unchecked");

            try
            {
                UpdateModifierKyOfShortcut(nameof(ShortcutModel.IsAltKey), false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when click check box all - alt");
                MessageBox.Show("Error when config shortcut: " + ex.Message, "Error Config Shortcut", MessageBoxButton.OK, MessageBoxImage.Error);
                return;

            }
            Log.Information("end cbAltAll_Unchecked");
        }

        #endregion check box

        #region Check Box click
        private void cbAll_Click(object sender, RoutedEventArgs e)
        {
            var cb = e.Source as CheckBox;

            if (cb != null && !cb.IsChecked.HasValue)
            {
                cb.IsChecked = false;
            }
        }

        private void cbKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // get list shortcut data to update check box all
                var sorce = lvConfigShortcut.ItemsSource as ObservableCollection<ShortcutModel>;

                if (sorce == null || sorce.Count <= 0)
                {
                    throw new InvalidOperationException("the list data shortcut is empty when trying to update");
                }

                var data = sorce.ToList();
                LoadCheckBoxAll(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show("error when update checkbox: " + ex.Message, "Error Config Shortcut", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        #endregion Check Box click
        private void UpdateModifierKyOfShortcut(string nameField, bool isChecked)
        {
            try
            {
                var dataShortcuts = lvConfigShortcut.ItemsSource as ObservableCollection<ShortcutModel>;

                if (dataShortcuts == null || dataShortcuts.Count <= 0)
                {
                    Log.Warning("the list data shortcut is empty when trying to update");
                    throw new InvalidOperationException("the list data shortcut is empty when trying to update");
                }

                // set all shortcut use ctrl
                foreach (var data in dataShortcuts)
                {
                    var typeData = data.GetType();
                    if (typeData != typeof(ShortcutModel) || typeData == null)
                    {
                        Log.Warning("invalid data");
                        throw new InvalidOperationException("the data is invalid");
                    }

                    var property = typeData.GetProperty(nameField, System.Reflection.BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    if (property == null || property.PropertyType != typeof(bool))
                    {
                        Log.Warning("the data field is invalid");
                        throw new InvalidOperationException("the data field is invalid");
                    }

                    // update field
                    property.SetValue(data, isChecked);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when update modifier key of shortcut");
                MessageBox.Show("Error when config shortcut: " + ex.Message, "Error Config Shortcut", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool? DetermineStateAllCheckBox(IEnumerable<bool> datas, int count)
        {
            // check if all element is true => return true
            if (datas.Count(data => data) == count)
            {
                return true;
            }

            // check if all element is false => return false
            if (datas.Count(data => !data) == count)
            {
                return false;
            }

            return null;
        }
        private void LoadCheckBoxAll(List<ShortcutModel> dataShortcuts)
        {
            bool? isAllAlt = DetermineStateAllCheckBox(dataShortcuts.Select(x => x.IsAltKey), dataShortcuts.Count);
            bool? isAllCtrl = DetermineStateAllCheckBox(dataShortcuts.Select(x => x.IsControlKey), dataShortcuts.Count);
            bool? isAllShift = DetermineStateAllCheckBox(dataShortcuts.Select(x => x.IsShiftKey), dataShortcuts.Count);

            cbAltAll.IsChecked = isAllAlt;
            cbCtrlAll.IsChecked = isAllCtrl;
            cbShiftAll.IsChecked = isAllShift;
        }

        #region Text box event
        private void tbxKey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Log.Information("start tbxKey_PreviewKeyDown");

            try
            {
                // Get text box that trigger event
                TextBox tbx = sender as TextBox;

                if (tbx == null)
                {
                    MessageBox.Show("Error the text box trigger the event is null", "Error Config", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Key pressedKey = e.Key;

                ValidKeyPress(pressedKey);

                string displayKey = pressedKey.ToString();

                tbx.Text = ConstData.Const.MapModifierKey.ContainsKey(displayKey) ? ConstData.Const.MapModifierKey[displayKey] : displayKey;

                // set lost focus for text box
                tbx.Focusable = false;
                tbx.Focusable = true;

                e.Handled = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in preview key down");
                MessageBox.Show("Error when config shortcut " + ex.Message, "Error Config", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Log.Information("end tbxKey_PreviewKeyDown");
        }

        private void tbxKey_GotFocus(object sender, RoutedEventArgs e)
        {
            // Get text box that trigger event
            TextBox tbx = sender as TextBox;

            if (tbx == null)
            {
                MessageBox.Show("Error the text box trigger the event is null", "Error Config", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (currFocusTextBox != null && currFocusTextBox != tbx)
            {
                // to prevent change focus to another if other text box is focused
                tbx.IsReadOnly = true;
                return;
            }

            currFocusTextBox = tbx;

            tbx.BorderThickness = new Thickness(2);
            tbx.FontWeight = FontWeights.Bold;
            tbx.CaretIndex = tbx.Text.Length;

            e.Handled = true;
        }

        private void tbxKey_LostFocus(object sender, RoutedEventArgs e)
        {
            // Get text box that trigger event
            TextBox tbx = sender as TextBox;

            if (tbx == null)
            {
                MessageBox.Show("Error the text box trigger the event is null", "Error Config", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // check if key is invalid or not
            string keyPressedStr = tbx.Text;
            if (!Enum.TryParse(typeof(Key), keyPressedStr, out _) || string.IsNullOrWhiteSpace(keyPressedStr))
            {
                Log.Warning("Warning invalid key press, " + keyPressedStr);
                MessageBox.Show("Warning invalid key press: " + keyPressedStr, "Warning Config", MessageBoxButton.OK, MessageBoxImage.Warning);
                tbx.Text = string.Empty;
                //currFocusTextBox.Focus();
                return;
            }

            if (currFocusTextBox != null && currFocusTextBox == tbx)
            {
                currFocusTextBox = null;
            }

            tbx.BorderThickness = new Thickness(0);
            tbx.FontWeight = FontWeights.Normal;

            e.Handled = true;
        }

        private void ValidKeyPress(Key pressedKey)
        {
            // check if pressed key is none
            if (pressedKey == Key.None)
            {
                Log.Warning("Warning key should not be empty ");
                throw new Exception("Key should not be empty");
            }

            // Skip modifier keys themselves
            if (pressedKey == Key.LeftCtrl || pressedKey == Key.RightCtrl ||
                pressedKey == Key.LeftAlt || pressedKey == Key.RightAlt ||
                pressedKey == Key.LeftShift || pressedKey == Key.RightShift ||
                pressedKey == Key.LWin || pressedKey == Key.RWin ||
                pressedKey == Key.System)
            {
                Log.Warning("Warning key should not be modifier key " + pressedKey);
                throw new Exception("key should not be modifier key");
            }
        }

        #endregion Text box event
    }
}