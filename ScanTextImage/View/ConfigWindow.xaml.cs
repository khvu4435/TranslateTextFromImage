using ScanTextImage.Interface;
using ScanTextImage.Model;
using ScanTextImage.View.Command;
using ScanTextImage.View.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ScanTextImage.View
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        #region Service
        private IConfigService _configService;
        private ISaveDataService _saveDataService;
        #endregion

        #region window
        private MainWindow _mainWindow;
        #endregion

        private ObservableCollection<ShortcutModel> shortcutModels;

        public ConfigWindow(IConfigService configService, ISaveDataService saveDataService, MainWindow mainWindow)
        {
            InitializeComponent();

            _configService = configService;
            _saveDataService = saveDataService;
            _mainWindow = mainWindow;

            var dataShortcuts = _saveDataService.GetShortcutConfig();

            shortcutModels = new ObservableCollection<ShortcutModel>(dataShortcuts);
            lvConfigShortcut.ItemsSource = shortcutModels;

            LoadCheckBoxAll(dataShortcuts);

        }
        private void btnCancelConfigShortcut_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSaveConfigShortcut_Click(object sender, RoutedEventArgs e)
        {
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

        }

        #region check box unchecked & checked

        private void cbCtrlAll_Checked(object sender, RoutedEventArgs e)
        {
            UpdateModifierKyOfShortcut(nameof(ShortcutModel.IsControlKey), true);
        }
        private void cbCtrlAll_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateModifierKyOfShortcut(nameof(ShortcutModel.IsControlKey), false);
        }

        private void cbShiftAll_Checked(object sender, RoutedEventArgs e)
        {
            UpdateModifierKyOfShortcut(nameof(ShortcutModel.IsShiftKey), true);
        }

        private void cbShiftAll_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateModifierKyOfShortcut(nameof(ShortcutModel.IsShiftKey), false);
        }

        private void cbAltAll_Checked(object sender, RoutedEventArgs e)
        {
            UpdateModifierKyOfShortcut(nameof(ShortcutModel.IsAltKey), true);
        }

        private void cbAltAll_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateModifierKyOfShortcut(nameof(ShortcutModel.IsAltKey), false);
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
                    throw new InvalidOperationException("the list data shortcut is empty when trying to update");
                }

                // set all shortcut use ctrl
                foreach (var data in dataShortcuts)
                {
                    var typeData = data.GetType();
                    if (typeData != typeof(ShortcutModel) || typeData == null)
                    {
                        throw new InvalidOperationException("the data is invalid");
                    }

                    var property = typeData.GetProperty(nameField, System.Reflection.BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    if (property == null || property.PropertyType != typeof(bool))
                    {
                        throw new InvalidOperationException("the data field is invalid");
                    }

                    // update field
                    property.SetValue(data, isChecked);
                }
            }
            catch (Exception ex)
            {
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

        private void tbxKey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Get text box that trigger event
            TextBox tbx = sender as TextBox;

            if(tbx == null)
            {
                MessageBox.Show("Error the text box trigger the event is null", "Error Config", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // prevent from being handle normally when press key
            e.Handled = true;

            Key pressedKey = e.Key;

            // Skip modifier keys themselves
            if (pressedKey == Key.LeftCtrl || pressedKey == Key.RightCtrl ||
                pressedKey == Key.LeftAlt || pressedKey == Key.RightAlt ||
                pressedKey == Key.LeftShift || pressedKey == Key.RightShift ||
                pressedKey == Key.LWin || pressedKey == Key.RWin ||
                pressedKey == Key.System)
            {
                MessageBox.Show("Warning key should not be modifier key", "Warning Config", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string displayKey = pressedKey.ToString();


            tbx.Text = ConstData.Const.MapModifierKey.ContainsKey(displayKey) ? ConstData.Const.MapModifierKey[displayKey] : displayKey;
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

            tbx.BorderThickness = new Thickness(1);
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

            tbx.BorderThickness = new Thickness(0);
        }
    }
}
