using Microsoft.Win32;
using OpenShade.Controls;
using OpenShade.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace OpenShade
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public enum ErrorType { None, Warning, Error, Info };


    public partial class MainWindow : Window
    {
        public string Debug = "yes"; //Debug Options: yes/no 
        string tweaksHash;
        string customTweaksHash;
        string postProcessesHash;
        string commentHash; // that's just the original comments

        /*
         * If a list contains the same items for their whole lifetime, but the individual objects within that list change, 
         * then it's enough for just the objects to raise change notifications (typically through INotifyPropertyChanged) and List<T> is sufficient. 
         * But if the list contains different objects from time to time, or if the order changes, then you should use ObservableCollection<T>.
         */
        List<Tweak> tweaks;
        ObservableCollection<CustomTweak> customTweaks;
        List<PostProcess> postProcesses;
        string comment;

        const string P3DRegistryPath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Lockheed Martin\\Prepar3D v5";
        string cacheDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Lockheed Martin\\Prepar3D v5\\Shaders\\";
        string currentDirectory = Directory.GetCurrentDirectory();
        string P3DDirectory;
        public string P3DVersion = "5.3.17.28160";

        FileIO fileData;
        string shaderDirectory;
        public string backupDirectory;

        public string activePresetPath;
        IniFile activePreset;
        public string loadedPresetPath;
        IniFile loadedPreset;

        // TODO: put this in a struct somewhere
        public static string cloudText, generalText, terrainText, funclibText, terrainFXHText, shadowText, HDRText, PBRText, compositeText;

        public MainWindow()
        {
            InitializeComponent();

            Log_RichTextBox.Document.Blocks.Clear();

            // Init
            tweaks = new List<Tweak>() { };
            customTweaks = new ObservableCollection<CustomTweak>() { };
            postProcesses = new List<PostProcess>() { };
            comment = "";

            Tweak.GenerateTweaksData(tweaks);

            ClearChangesInfo(tweaks);
            ClearChangesInfo(postProcesses);

            Tweak_List.ItemsSource = tweaks;

            CollectionView tweaksView = (CollectionView)CollectionViewSource.GetDefaultView(Tweak_List.ItemsSource);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("category");
            tweaksView.GroupDescriptions.Add(groupDescription);

            fileData = new FileIO(this);

            tweaksHash = HelperFunctions.GetDictHashCode(tweaks);
            customTweaksHash = HelperFunctions.GetDictHashCode(customTweaks);
            postProcessesHash = HelperFunctions.GetDictHashCode(postProcesses);
            commentHash = comment;

            // Shaders files
            P3DDirectory = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Lockheed Martin\Prepar3D v5", "AppPath", null);
            if (P3DDirectory == null)
            {
                Log(ErrorType.Error, "Prepar3D v5 path not found");
                ChangeMenuBarState(false);
                return;
            }

            int index = P3DDirectory.IndexOf('\0');
            if (index >= 0) { P3DDirectory = P3DDirectory.Substring(0, index); }

            P3DMain_TextBox.Text = P3DDirectory;

            shaderDirectory = P3DDirectory + "ShadersHLSL\\";
            backupDirectory = currentDirectory + "\\Backup Shaders\\"; // Default directory        

            if (!Directory.Exists(shaderDirectory))
            {
                Log(ErrorType.Error, "P3D shader directory not found!");
                ChangeMenuBarState(false);
                return;
            }
            P3DShaders_TextBox.Text = shaderDirectory;

            if (!Directory.Exists(cacheDirectory))
            {
                Log(ErrorType.Error, "Shader cache directory not found, please launch P3D first!");
                ChangeMenuBarState(false);
                return;
            }
            ShaderCache_TextBox.Text = cacheDirectory;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Load settings first
            if (File.Exists(currentDirectory + "\\" + FileIO.settingsFile))
            {
                fileData.LoadSettings(currentDirectory + "\\" + FileIO.settingsFile);
            }

            // Load preset
            if (loadedPresetPath != null)
            {
                if (File.Exists(loadedPresetPath))
                {
                    try
                    {
                        loadedPreset = new IniFile(loadedPresetPath);
                        LoadedPreset_TextBlock.Text = loadedPreset.filename;
                        LoadPreset(loadedPreset, false);
                        Log(ErrorType.None, "Preset [" + loadedPreset.filename + "] loaded");
                    }
                    catch (Exception ex)
                    {                        
                        Log(ErrorType.Error, "Failed to load preset file [" + loadedPresetPath + "]. " + ex.Message);
                    }
                }
                else
                {                    
                    Log(ErrorType.Error, "Loaded Preset file [" + loadedPresetPath + "] not found");
                }
            }

            if (activePresetPath != null)
            {
                if (File.Exists(activePresetPath))
                {                   
                    activePreset = new IniFile(activePresetPath);
                    ActivePreset_TextBlock.Text = activePreset.filename;                    
                }
                else
                {
                    Log(ErrorType.Error, "Active Preset file [" + activePresetPath + "] not found");
                }
            }

            // Load Theme
            Theme_ComboBox.ItemsSource = Enum.GetValues(typeof(Themes)).Cast<Themes>();
            Theme_ComboBox.SelectedItem = ((App)Application.Current).CurrentTheme;

            // Load Backup files
            ShaderBackup_TextBox.Text = backupDirectory;

            // Show P3D Version Info and some Debug stuff
            string currentP3DEXEVersion = FileVersionInfo.GetVersionInfo(P3DDirectory + "Prepar3D.exe").FileVersion;
            Log(ErrorType.Info, "You currently running P3D Version: " + currentP3DEXEVersion);
            CurrentP3DVersionText.Text = currentP3DEXEVersion;
            if (Debug == "yes") {
            Log(ErrorType.Info, "Application Hardcoded Version: " + P3DVersion);
            Log(ErrorType.Info, "Current P3D EXE Version: " + currentP3DEXEVersion);
            Log(ErrorType.Info, "Current P3D Path: " + P3DDirectory);
            Log(ErrorType.Info, "Current Backup Directory: " + backupDirectory);
            }

            //Handling Current P3D Version
            if (Directory.Exists(backupDirectory))
            {
                string currentP3DVersion = FileVersionInfo.GetVersionInfo(P3DDirectory + "Prepar3D.exe").FileVersion;
                if (P3DVersion != currentP3DVersion)
                {
                    MessageBoxResult result = MessageBox.Show("OpenShade has detected a new version of Prepar3D (" + currentP3DVersion + ").\r\n\r\nIt is STRONGLY recommended that you backup the default shader files again otherwise they will be overwritten by old shader files when applying a preset.", "New version detected", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.OK); // TODO: Localization
                    if (result == MessageBoxResult.OK)
                    {
                        if (fileData.CopyShaderFiles(shaderDirectory, backupDirectory))
                        {
                            P3DVersion = currentP3DVersion;
                            Log(ErrorType.None, "Shaders backed up");
                        }
                        else
                        {
                            Log(ErrorType.Warning, "Shaders could not be backed up. OpenShade can not run.");
                            ChangeMenuBarState(false);
                        }
                    }                    
                }

                if (fileData.CheckShaderBackup(backupDirectory))
                {
                    fileData.LoadShaderFiles(backupDirectory);
                }
                else
                {
                    Log(ErrorType.Error, "Missing shader files in " + backupDirectory + ". OpenShade can not run");
                    ChangeMenuBarState(false);
                }
            }
            else {

                if (Directory.Exists(shaderDirectory)) // This better be true
                {
                    MessageBoxResult result = MessageBox.Show("OpenShade will backup your Prepar3D shaders now.\r\nMake sure the files are the original ones or click 'Cancel' and manually select your backup folder in the application settings.", "Backup", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.OK); // TODO: Localization
                    if (result == MessageBoxResult.OK)
                    {
                        Directory.CreateDirectory("Backup Shaders");
                        if (fileData.CopyShaderFiles(shaderDirectory, backupDirectory))
                        {
                            Log(ErrorType.None, "Shaders backed up");                            
                        }
                        else
                        {
                            Log(ErrorType.Warning, "Shaders could not be backed up. OpenShade can not run.");
                            ChangeMenuBarState(false);
                        }
                    }
                    else
                    {
                        Log(ErrorType.Warning, "Shaders were not backed up. OpenShade can not run.");
                        ChangeMenuBarState(false);
                    }
                }
            }        
        }

        private void Window_Closed(object sender, EventArgs e) // important to use Closed() and not Closing() because this has to happen after any LostFocus() event to have all up-to-date parameters
        {
            if (HelperFunctions.GetDictHashCode(tweaks) != tweaksHash ||
                HelperFunctions.GetDictHashCode(customTweaks) != customTweaksHash || 
                HelperFunctions.GetDictHashCode(postProcesses) != postProcessesHash || 
                comment != commentHash ||
                !File.Exists(loadedPresetPath))
            {
                if (loadedPreset != null)
                {
                    MessageBoxResult result = MessageBox.Show("Some changes for the preset [" + loadedPreset.filename + "] were not saved.\r\nWould you like to save them now?", "Save", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                    if (result == MessageBoxResult.Yes)
                    {
                        SavePreset_Click(null, null);
                    }
                }
                else {
                    loadedPresetPath = currentDirectory + "\\custom_preset.ini";
                    int i = 1;
                    while (File.Exists(loadedPresetPath))
                    {
                        i++;
                        loadedPresetPath = currentDirectory + "\\custom_preset_" + i.ToString() + ".ini";
                    }

                    loadedPreset = new IniFile(loadedPresetPath);

                    MessageBoxResult result = MessageBox.Show("Some changes were not saved.\r\nWould you like to save them now as a new preset ["+ loadedPreset.filename + "] ?", "Save", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                    if (result == MessageBoxResult.Yes)
                    {                        
                        SavePreset_Click(null, null);
                    }
                }
            }

            fileData.SaveSettings(currentDirectory + "\\" + FileIO.settingsFile);
        }


        #region MainTweaks
        private void TweakList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List_SelectionChanged(Tweak_List, TweakStack, TweakClearStack, TweakTitleTextblock, TweakDescriptionTextblock);
        }
        #endregion

        #region ParametersUpdates
        private void List_SelectionChanged(ListView itemListview, StackPanel StackGrid, StackPanel clearStack, Label titleBlock, TextBlock descriptionBlock)
        {
            if (itemListview.SelectedItem != null)
            {
                StackGrid.Children.Clear();
                clearStack.Children.Clear();

                BaseTweak selectedEffect = (BaseTweak)itemListview.SelectedItem;                

                titleBlock.Content = selectedEffect.name;
                descriptionBlock.Text = selectedEffect.description;

                if (selectedEffect.parameters.Count > 0)
                {
                    Button resetButton = new Button();
                    resetButton.Content = "Reset default";
                    resetButton.ToolTip = "Reset parameters to their default value";
                    resetButton.Width = 100;
                    resetButton.Height = 25;
                    resetButton.VerticalAlignment = VerticalAlignment.Top;
                    resetButton.HorizontalAlignment = HorizontalAlignment.Right;
                    resetButton.Margin = new Thickness(0, 0, 0, 10);
                    resetButton.Click += new RoutedEventHandler(ResetParameters_Click);

                    clearStack.Children.Add(resetButton);

                    Button clearButton = new Button();
                    clearButton.Content = "Reset previous";
                    clearButton.ToolTip = "Reset parameters to their previous value";
                    clearButton.Width = 100;
                    clearButton.Height = 25;
                    clearButton.VerticalAlignment = VerticalAlignment.Top;
                    clearButton.HorizontalAlignment = HorizontalAlignment.Right;
                    clearButton.Click += new RoutedEventHandler(ResetParametersPreset_Click);

                    clearStack.Children.Add(clearButton);

                    foreach (Parameter param in selectedEffect.parameters)
                    {
                        StackPanel rowStack = new StackPanel();
                        rowStack.Orientation = Orientation.Horizontal;

                        TextBlock txtBlock = new TextBlock();
                        txtBlock.Text = param.name;
                        txtBlock.TextWrapping = TextWrapping.Wrap;
                        txtBlock.Width = 170;
                        txtBlock.Height = 30;
                        txtBlock.Margin = new Thickness(0, 0, 10, 0);                        

                        rowStack.Children.Add(txtBlock);

                        if (param.control == UIType.Checkbox)
                        {
                            CheckBox checkbox = new CheckBox();
                            checkbox.IsChecked = ((param.value == "1") ? true : false);
                            checkbox.Uid = param.id;
                            checkbox.VerticalAlignment = VerticalAlignment.Center;
                            checkbox.Click += new RoutedEventHandler(Checkbox_Click);
                            rowStack.Children.Add(checkbox);
                        }
                        else if (param.control == UIType.RGB)
                        {
                            var group = new GroupBox();
                            group.Header = "RGB";

                            var container = new StackPanel();
                            container.Orientation = Orientation.Horizontal;

                            var Rtext = new NumericSpinner();
                            Rtext.Uid = param.id + "_R";
                            Rtext.Height = 25;
                            Rtext.Width = 70;
                            Rtext.Value = decimal.Parse(param.value.Split(',')[0]);
                            Rtext.MinValue = param.min;
                            Rtext.MaxValue = param.max;
                            Rtext.PropertyChanged += new EventHandler(RGB_ValueChanged);

                            var Gtext = new NumericSpinner();
                            Gtext.Uid = param.id + "_G";
                            Gtext.Height = 25;
                            Gtext.Width = 70;
                            Gtext.Value = decimal.Parse(param.value.Split(',')[1]);
                            Gtext.MinValue = param.min;
                            Gtext.MaxValue = param.max;
                            Gtext.PropertyChanged += new EventHandler(RGB_ValueChanged);

                            var Btext = new NumericSpinner();
                            Btext.Uid = param.id + "_B";
                            Btext.Height = 25;
                            Btext.Width = 70;
                            Btext.Value = decimal.Parse(param.value.Split(',')[2]);
                            Btext.MinValue = param.min;
                            Btext.MaxValue = param.max;
                            Btext.PropertyChanged += new EventHandler(RGB_ValueChanged);

                            container.Children.Add(Rtext);
                            container.Children.Add(Gtext);
                            container.Children.Add(Btext);

                            group.Content = container;

                            rowStack.Children.Add(group);
                        }

                        else if (param.control == UIType.Text)
                        {
                            var spinner = new NumericSpinner();
                            spinner.Uid = param.id;
                            spinner.Width = 170;
                            spinner.Height = 25;
                            spinner.Decimals = 10;
                            spinner.MinValue = param.min;
                            spinner.MaxValue = param.max;
                            spinner.Step = 0.1m;
                            spinner.ValueChanged += new EventHandler(ParameterSpinner_ValueChanged);

                            var item = new MenuItem();
                            item.Header = "Make Custom";
                            item.SetResourceReference(Control.ForegroundProperty, "TextColor");
                            item.Tag = spinner;
                            item.Click += ParameterSwitch_Click;
                            spinner.ContextMenu = new ContextMenu();
                            spinner.ContextMenu.Items.Add(item);


                            var txtbox = new TextBox();
                            txtbox.Uid = param.id;
                            txtbox.Width = 170;
                            txtbox.Height = 25;
                            txtbox.VerticalContentAlignment = VerticalAlignment.Top;
                            //spinner.TextWrapping = TextWrapping.Wrap;
                            txtbox.Text = param.value;
                            txtbox.KeyUp += new KeyEventHandler(ParameterText_KeyUp);

                            item = new MenuItem();
                            item.Header = "Make Default";
                            item.SetResourceReference(Control.ForegroundProperty, "TextColor");
                            item.Tag = txtbox;
                            item.Click += ParameterSwitch_Click;
                            txtbox.ContextMenu = new ContextMenu();
                            txtbox.ContextMenu.Items.Add(item);

                            decimal val;
                            if (decimal.TryParse(param.value, out val))
                            {
                                spinner.Value = val;
                                txtbox.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                spinner.Value = decimal.Parse(param.defaultValue);
                                spinner.Visibility = Visibility.Collapsed;
                            }

                            rowStack.Children.Add(spinner);
                            rowStack.Children.Add(txtbox);
                        }

                        else if (param.control == UIType.TextBox)
                        {

                            var txtbox = new TextBox();
                            txtbox.Uid = param.id;
                            txtbox.Width = 190;
                            txtbox.Height = 23;
                            txtbox.VerticalAlignment = VerticalAlignment.Center;
                            txtbox.VerticalContentAlignment = VerticalAlignment.Center;
                            //spinner.TextWrapping = TextWrapping.Wrap;
                            txtbox.Text = param.value;
                            txtbox.KeyUp += new KeyEventHandler(ParameterText_KeyUp);

                            rowStack.Children.Add(txtbox);


                        }

                        else if (param.control == UIType.Combobox)
                        {
                            var combo = new ComboBox();
                            combo.Uid = param.id;
                            combo.Width = 170;
                            combo.Height = 25;
                            combo.SetResourceReference(Control.ForegroundProperty, "TextColor");
                            foreach (var item in param.range)
                            {
                                combo.Items.Add(item);
                            }
                            combo.SelectedIndex = int.Parse(param.value);
                            combo.SelectionChanged += new SelectionChangedEventHandler(Combobox_SelectionChanged);

                            rowStack.Children.Add(combo);
                        }
                        
                        TextBox changeTxtbox = new TextBox();                        
                        changeTxtbox.IsReadOnly = true;
                        changeTxtbox.Background = Brushes.Transparent;
                        changeTxtbox.Foreground = Brushes.Orange;
                        changeTxtbox.BorderThickness = new Thickness(0);
                        changeTxtbox.Width = 170;
                        changeTxtbox.Height = 30;
                        changeTxtbox.Margin = new Thickness(10, 0, 10, 0);
                        changeTxtbox.Visibility = param.hasChanged ? Visibility.Visible : Visibility.Collapsed;
                        changeTxtbox.Uid = param.id + "-changeTxtbox";

                        if (param.control == UIType.Checkbox)
                        {
                            changeTxtbox.Text = (param.oldValue == "1") ? "Enabled" : "Disabled";
                        }
                        else {
                            changeTxtbox.Text = param.oldValue;
                        }

                        rowStack.Children.Add(changeTxtbox);                        

                        StackGrid.Children.Add(rowStack);
                    }
                }
                else
                {
                    Label label = new Label();
                    label.Content = "No additional parameters";
                    StackGrid.Children.Add(label);
                }                
            }
        }

        private void Checkbox_Click(object sender, EventArgs e)
        {
            CheckBox checkbox = (CheckBox)sender;
            TabItem currentTab = HelperFunctions.FindAncestorOrSelf<TabItem>(checkbox);
            Parameter param = null;

            if (currentTab.Name == "Tweak_Tab") { param = ((Tweak)Tweak_List.SelectedItem).parameters.First(p => p.id == checkbox.Uid); }
                       
            if (checkbox.IsChecked == true)
            {
                param.value = "1";
            }
            else
            {
                param.value = "0";
            }

            // TODO: This is terrible code, but it simplifies things for now...
            TextBox tb = null;
            if (currentTab.Name == "Tweak_Tab") { tb = (TextBox)HelperFunctions.FindUid(TweakStack, param.id + "-changeTxtbox"); }
            tb.Visibility = param.hasChanged ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ParameterText_KeyUp(object sender, EventArgs e)
        {
            TextBox txtBox = (TextBox)sender;
            TabItem currentTab = HelperFunctions.FindAncestorOrSelf<TabItem>(txtBox);            
            Parameter param = null;

            if (currentTab.Name == "Tweak_Tab") { param = ((Tweak)Tweak_List.SelectedItem).parameters.First(p => p.id == txtBox.Uid); }

            if (currentTab.Name != "Custom_Tab") {                
                param.value = txtBox.Text;

                TextBox tb = null;
                if (currentTab.Name == "Tweak_Tab") { tb = (TextBox)HelperFunctions.FindUid(TweakStack, param.id + "-changeTxtbox"); }
                tb.Visibility = param.hasChanged ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ParameterSpinner_ValueChanged(object sender, EventArgs e)
        {
            NumericSpinner spinner = (NumericSpinner)sender;
            TabItem currentTab = HelperFunctions.FindAncestorOrSelf<TabItem>(spinner);
            Parameter param = null;

            if (currentTab.Name == "Tweak_Tab") { param = ((Tweak)Tweak_List.SelectedItem).parameters.First(p => p.id == spinner.Uid); }    

            if (currentTab.Name != "Custom_Tab") {                
                param.value = spinner.Value.ToString();

                TextBox tb = null;
                if (currentTab.Name == "Tweak_Tab") { tb = (TextBox)HelperFunctions.FindUid(TweakStack, param.id + "-changeTxtbox"); }
                tb.Visibility = param.hasChanged ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ParameterSwitch_Click(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;

            TabItem currentTab = HelperFunctions.FindAncestorOrSelf<TabItem>((DependencyObject)item.Tag);
            StackPanel stackRow = HelperFunctions.FindAncestorOrSelf<StackPanel>((DependencyObject)item.Tag);
    
            if (item.Tag.GetType() == typeof(NumericSpinner))
            {
                NumericSpinner control = (NumericSpinner)item.Tag;
                int index = stackRow.Children.IndexOf(control);

                control.Visibility = Visibility.Collapsed;
                stackRow.Children[index + 1].Visibility = Visibility.Visible;

                ParameterText_KeyUp(stackRow.Children[index + 1], new RoutedEventArgs());
            }

            else if (item.Tag.GetType() == typeof(TextBox)) // TODO: possible bug here still
            {
                TextBox control = (TextBox)item.Tag;
                int index = stackRow.Children.IndexOf(control);

                control.Visibility = Visibility.Collapsed;
                stackRow.Children[index - 1].Visibility = Visibility.Visible;

                ParameterSpinner_ValueChanged(stackRow.Children[index - 1], new RoutedEventArgs());
            }
        }

        private void RGB_ValueChanged(object sender, EventArgs e)
        {
            NumericSpinner spinner = (NumericSpinner)sender;

            string uid = spinner.Uid.Split('_')[0];
            string channel = spinner.Uid.Split('_')[1];

            TabItem currentTab = HelperFunctions.FindAncestorOrSelf<TabItem>(spinner);
            Parameter param = null;

            if (currentTab.Name == "Tweak_Tab") { param = ((Tweak)Tweak_List.SelectedItem).parameters.First(p => p.id == uid); }
            
            string oldR = param.value.Split(',')[0];
            string oldG = param.value.Split(',')[1];
            string oldB = param.value.Split(',')[2];
                        
            switch (channel)
            {
                case "R":
                    param.value = spinner.Value.ToString() + "," + oldG + "," + oldB;
                    break;
                case "G":
                    param.value = oldR + "," + spinner.Value.ToString() + "," + oldB;
                    break;
                case "B":
                    param.value = oldR + "," + oldG + "," + spinner.Value.ToString();
                    break;
            }

            TextBox tb = null;
            if (currentTab.Name == "Tweak_Tab") { tb = (TextBox)HelperFunctions.FindUid(TweakStack, param.id + "-changeTxtbox"); }
            tb.Visibility = param.hasChanged ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;
            TabItem currentTab = HelperFunctions.FindAncestorOrSelf<TabItem>(combo);
            Parameter param = null;

            if (currentTab.Name == "Tweak_Tab") { param = ((Tweak)Tweak_List.SelectedItem).parameters.First(p => p.id == combo.Uid); }

            if (currentTab.Name != "Custom_Tab") {                
                param.value = combo.SelectedIndex.ToString();

                TextBox tb = null;
                if (currentTab.Name == "Tweak_Tab") { tb = (TextBox)HelperFunctions.FindUid(TweakStack, param.id + "-changeTxtbox"); }
                tb.Visibility = param.hasChanged ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void RichTextBox_LostFocus(object sender, EventArgs e)
        {
            RichTextBox rich = (RichTextBox)sender;

            switch (rich.Name)
            {
                case "CustomTweakOldCode_RichTextBox":
                    break;

                case "CustomTweakNewCode_RichTextBox":
                    break;
            }
        }

        private void ResetParameters_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            TabItem currentTab = HelperFunctions.FindAncestorOrSelf<TabItem>(btn);

            ListView currentList = null;

            if (currentTab.Name == "Tweak_Tab") { currentList = Tweak_List; }

            BaseTweak selectedEffect = (BaseTweak)currentList.SelectedItem;

            foreach (var param in selectedEffect.parameters)
            {
                param.value = param.defaultValue;
            }

            if (currentTab.Name == "Tweak_Tab") { List_SelectionChanged(Tweak_List, TweakStack, TweakClearStack, TweakTitleTextblock, TweakDescriptionTextblock); }
        }

        private void ResetParametersPreset_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            TabItem currentTab = HelperFunctions.FindAncestorOrSelf<TabItem>(btn);

            ListView currentList = null;

            if (currentTab.Name == "Tweak_Tab") { currentList = Tweak_List; }           

            BaseTweak selectedEffect = (BaseTweak)currentList.SelectedItem;

            foreach (var param in selectedEffect.parameters)
            {
                param.value = param.oldValue;
            }

            if (currentTab.Name == "Tweak_Tab") { List_SelectionChanged(Tweak_List, TweakStack, TweakClearStack, TweakTitleTextblock, TweakDescriptionTextblock); }            
        }
        #endregion

        #region Settings
        private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ((App)Application.Current).ChangeTheme((Themes)Theme_ComboBox.SelectedItem);
            }
        }

        private void ShaderBackup_Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog dlg = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();

            dlg.IsFolderPicker = true;
            dlg.Multiselect = false;
            dlg.InitialDirectory = Directory.GetCurrentDirectory();
            dlg.Title = "Browse Backup Shader directory";
                       
            var result = dlg.ShowDialog();

            if (result == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                backupDirectory = dlg.FileName + "\\";
                if (fileData.CheckShaderBackup(backupDirectory))
                {
                    ChangeMenuBarState(true);
                    ShaderBackup_TextBox.Text = backupDirectory;
                    Log(ErrorType.None, "All shader files found");
                    Log(ErrorType.None, "Backup directory set to " + backupDirectory);
                }
                else {
                    ChangeMenuBarState(false);
                    ShaderBackup_TextBox.Text = backupDirectory;
                    Log(ErrorType.Error, "Missing shader files in " + backupDirectory + ". OpenShade can not run");
                }
            }
        }
        #endregion


        private void NewPreset_Click(object sender, RoutedEventArgs e)
        {
            // When creating a new preset, we choose to KEEP all the parameters that were on the previous preset instead of clearing everything.
            // This seems to make more sense, if the user wants to reset everything, there's a button for that.           

            loadedPresetPath = currentDirectory + "\\custom_preset.ini";
            int i = 1;
            while (File.Exists(loadedPresetPath)) {
                i++;
                loadedPresetPath = currentDirectory + "\\custom_preset_" + i.ToString() + ".ini";
            }

            loadedPreset = new IniFile(loadedPresetPath);
            LoadedPreset_TextBlock.Text = loadedPreset.filename;

            Log(ErrorType.None, "New Preset [" + loadedPreset.filename + "] created");
        }

        private void OpenPreset_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            OpenFileDialog dlg = new OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".ini";
            dlg.Filter = "ini files|*.ini";
            dlg.Multiselect = false;
            dlg.InitialDirectory = Directory.GetCurrentDirectory();
            dlg.Title = "Browse ini file";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            if (result.HasValue && result.Value)
            {
                // Load preset
                string backupPresetPath = loadedPresetPath; // used as backup in case the following load fails

                try
                {
                    loadedPresetPath = dlg.FileName;
                    loadedPreset = new IniFile(loadedPresetPath);
                    LoadedPreset_TextBlock.Text = loadedPreset.filename;
                    LoadPreset(loadedPreset, true);
                    Log(ErrorType.None, "Preset [" + loadedPreset.filename + "] loaded");
                }
                catch (Exception ex)
                {
                    Log(ErrorType.Error, "Failed to load preset file [" + loadedPresetPath + "]. " + ex.Message);

                    // Revert to previous preset
                    loadedPresetPath = backupPresetPath;
                    loadedPreset = new IniFile(backupPresetPath);
                    LoadedPreset_TextBlock.Text = loadedPreset.filename;
                    LoadPreset(loadedPreset, true);                    
                }    
            }
        }

        private void LoadPreset(IniFile preset, bool monitorChanges)
        {             
            fileData.LoadTweaks(tweaks, preset, monitorChanges);
            PresetComments_TextBox.Text = fileData.LoadComments(preset);

            tweaksHash = HelperFunctions.GetDictHashCode(tweaks);
            customTweaksHash = HelperFunctions.GetDictHashCode(customTweaks);
            postProcessesHash = HelperFunctions.GetDictHashCode(postProcesses);
            commentHash = comment;

            List_SelectionChanged(Tweak_List, TweakStack, TweakClearStack, TweakTitleTextblock, TweakDescriptionTextblock);

            Tweak_List.Items.Refresh();                 
        }

        private void SavePreset_Click(object sender, RoutedEventArgs e)
        {
            try // TODO: Do we want to reset changes when we save?
            {
                if (loadedPreset == null) {
                    loadedPresetPath = currentDirectory + "\\custom_preset.ini";
                    
                    int i = 1;
                    while (File.Exists(loadedPresetPath))
                    {
                        i++;
                        loadedPresetPath = currentDirectory + "\\custom_preset_" + i.ToString() + ".ini";
                    }

                    loadedPreset = new IniFile(loadedPresetPath);
                    LoadedPreset_TextBlock.Text = loadedPreset.filename;
                }

                comment = PresetComments_TextBox.Text;
                fileData.SavePreset(tweaks, comment, loadedPreset);

                // Update hashes
                tweaksHash = HelperFunctions.GetDictHashCode(tweaks);
                customTweaksHash = HelperFunctions.GetDictHashCode(customTweaks);
                postProcessesHash = HelperFunctions.GetDictHashCode(postProcesses);
                commentHash = comment;

                Log(ErrorType.None, "Preset [" + loadedPreset.filename + "] saved in " + loadedPreset.filepath);
            }
            catch (Exception ex)
            {
                Log(ErrorType.Error, "Failed to save preset file [" + loadedPreset.filename + "]. " + ex.Message);
            }
        }

        private void SavePresetAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "ini files|*.ini";
            dlg.Title = "Save Preset";
            dlg.FileName = "custom_preset.ini";

            Nullable<bool> result = dlg.ShowDialog();

            if (result.HasValue && result.Value && dlg.FileName != "")
            {
                string newPresetPath = dlg.FileName;
                IniFile newPreset = new IniFile(newPresetPath);
                try
                {
                    comment = PresetComments_TextBox.Text;
                    fileData.SavePreset(tweaks, comment, newPreset);

                    tweaksHash = HelperFunctions.GetDictHashCode(tweaks);
                    commentHash = comment;

                    loadedPresetPath = newPresetPath;
                    loadedPreset = newPreset;
                    LoadedPreset_TextBlock.Text = loadedPreset.filename;
                    Log(ErrorType.None, "Preset [" + loadedPreset.filename + "] saved in " + loadedPreset.filepath);
                }
                catch (Exception ex)
                {
                    Log(ErrorType.Error, "Failed to save preset file [" + loadedPreset.filename + "]. " + ex.Message);
                }
            }
        }


        private void ApplyPreset(object sender, RoutedEventArgs e)
        {

            fileData.LoadShaderFiles(backupDirectory); // Always load the unmodified files;

            // NOTE: Not sure what is the best way to implement this... for now just handle each tweak on a case by case basis, which is a lot of code but fine for now
            // NOTE: This code is getting more awful by the minute


            //                            currentFile = FileIO.compositeFile;
            //                            compositeText = compositeText.AddAfter(ref success, "const float ToReplace;", "\r\n New Code1" +
            //"                                                                                          \r\n New Code2"+
            //"                                                                                          \r\n Last Line");

            int tweakCount = 0;

            foreach (var tweak in tweaks)
            {
                if (tweak.isEnabled)
                {
                    bool supported = true;
                    bool success = false;
                    string currentFile = "";

                    switch (tweak.name)
                    {

                        #region EnhancedAtmospherics

                        case "Enhanced Atmospherics Atmosphere":
                            currentFile = FileIO.funclibFile;

                            // Add required constants and phase functions
                            // Const
                            funclibText = funclibText.AddBefore(ref success, "float4 EnhancedAtmosphericsBlend(float4 color, float3 screenPos, float3 worldPos, uint InstanceID)", "\r\n\r\nstatic const float3 normalized_ozone_coefficient = float3(0.61344749, 0.0, 1.0);\r\n\r\n\r\n\r\n");
                            
                            //map
                            funclibText = funclibText.AddBefore(ref success, "float4 EnhancedAtmosphericsBlend(float4 color, float3 screenPos, float3 worldPos, uint InstanceID)", "\r\nfloat map(float input_value, float input_start, float input_end, float output_start, float output_end)" +
                            "\r\n{" +
                            "\r\n    float slope = (output_end - output_start) / (input_end - input_start);\r\n" +
                            "\r\n     return clamp(output_start + (slope * (input_value - input_start)), min(output_start, output_end), max(output_start, output_end));" +
                            "\r\n}\r\n");

                            //interpolate_sun_angle
                            funclibText = funclibText.AddBefore(ref success, "float4 EnhancedAtmosphericsBlend(float4 color, float3 screenPos, float3 worldPos, uint InstanceID)", "\r\nfloat interpolate_sun_angle(float sun_angle, float day_value, float twilight_value, float night_value)" +
                            "\r\n{" +
                            "\r\n    float output_value;\r\n" +
                            "\r\n    if (sun_angle > 0.0) output_value = map(sqrt(sun_angle), sqrt(0.175), 0.0, day_value, twilight_value);" +
                            "\r\n    else output_value = map(sun_angle, -0.125, -0.25, twilight_value, night_value); \r\n" +
                            "\r\n    return output_value;" +
                            "\r\n}\r\n");

                            // get_luminance
                            funclibText = funclibText.AddBefore(ref success, "float4 EnhancedAtmosphericsBlend(float4 color, float3 screenPos, float3 worldPos, uint InstanceID)", "\r\nfloat3 get_luminance(float3 input_color)" +
                            "\r\n{" +
                            "\r\n    float output_luminance = dot(input_color, float3(0.2126, 0.7152, 0.0722));" +
                            "\r\n    return float3(output_luminance, output_luminance, output_luminance);" +
                            "\r\n}\r\n");

                            //Add main Tweaks in func lib
                            funclibText = funclibText.AddBefore(ref success, "#if !defined(SHD_NO_FOG)", $"       insc = interpolate_sun_angle(cb_mSun.mDirection.y, {tweak.parameters[3].value}, {tweak.parameters[4].value}, {tweak.parameters[5].value}) * lerp(insc, insc * normalized_ozone_coefficient, interpolate_sun_angle(cb_mSun.mDirection.y, {tweak.parameters[0].value}, {tweak.parameters[1].value}, {tweak.parameters[2].value}));" +
                            $"\r\n        insc = lerp(get_luminance(insc), insc, interpolate_sun_angle(cb_mSun.mDirection.y, {tweak.parameters[6].value}, {tweak.parameters[7].value}, {tweak.parameters[8].value}));\r\n");


                            //Add main Tweaks in composite
                            currentFile = FileIO.compositeFile;
                            compositeText = compositeText.AddAfter(ref success, "        loss = txBindless(cb_mLoss2DTextureIndex).SampleLevel(samClamp, lossUV, 0);", $"\r\n\r\n        insc.rgb = interpolate_sun_angle(cb_mSun.mDirection.y, {tweak.parameters[3].value}, {tweak.parameters[4].value}, {tweak.parameters[5].value}) * lerp(insc.rgb, insc.rgb * normalized_ozone_coefficient, interpolate_sun_angle(cb_mSun.mDirection.y, {tweak.parameters[0].value}, {tweak.parameters[1].value}, {tweak.parameters[2].value}));" +
                            $"\r\n        insc.rgb = lerp(get_luminance(insc.rgb), insc.rgb, interpolate_sun_angle(cb_mSun.mDirection.y, {tweak.parameters[6].value}, {tweak.parameters[7].value}, {tweak.parameters[8].value}));");
                            break;

                        case "Enhanced Atmospherics Clouds":

                            //Add Cloud Tweak in composite
                            currentFile = FileIO.compositeFile;
                            compositeText = compositeText.AddAfter(ref success, "            clouds = lerp(clouds, nearClouds, cloudFade);", $"\r\n\r\n            clouds.rgb = interpolate_sun_angle(cb_mSun.mDirection.y, {tweak.parameters[3].value}, {tweak.parameters[4].value}, {tweak.parameters[5].value}) * lerp(clouds.rgb, clouds.rgb * normalized_ozone_coefficient, interpolate_sun_angle(cb_mSun.mDirection.y, {tweak.parameters[0].value}, {tweak.parameters[1].value}, {tweak.parameters[2].value}));" +
                            $"\r\n            clouds.rgb = lerp(get_luminance(clouds.rgb), clouds.rgb, interpolate_sun_angle(cb_mSun.mDirection.y, {tweak.parameters[6].value}, {tweak.parameters[7].value}, {tweak.parameters[8].value}));\r\n");

                            currentFile = FileIO.cloudFile;
                            cloudText = cloudText.AddAfter(ref success, "    fColor += scatter * cb_mCombinedDiffuse.rgb;", "\r\n\r\n    #if defined(SHD_ENHANCED_ATMOSPHERICS_BLEND)" +
                            $"\r\n        fColor *= interpolate_sun_angle(cb_mSun.mDirection.y, {tweak.parameters[3].value}, {tweak.parameters[4].value}, {tweak.parameters[5].value});" +
                            $"\r\n        fColor = lerp(get_luminance(fColor), fColor, interpolate_sun_angle(cb_mSun.mDirection.y, {tweak.parameters[6].value}, {tweak.parameters[7].value}, {tweak.parameters[8].value}));\r\n" +
                            "    #endif\r\n");
                            break;
                        #endregion




                        #region PBR
                        case "Aircraft PBR brightness":
                            currentFile = FileIO.PBRFile;
                            PBRText = PBRText.AddBefore(ref success, "        color.rgb += ambient;", "\r\n        if (cb_mObjectType == 19) " +
                            $"\r\n			color.rgb += ambient*(2.2-(lerp( {tweak.parameters[0].value}, {tweak.parameters[1].value}, cb_mDayNightInterpolant)));" +
                            "\r\n		else");





                            break;
                        #endregion




                        #region HDR
                        case "Alternate tonemap adjustment":
                            currentFile = FileIO.HDRFile;
                            HDRText = HDRText.AddBefore(ref success, "shared cbuffer cbHDRData : REGISTER(b, POST_PROCESS_CB_REGISTER)", "\r\nstatic const float toneMapFactor2 = 0.001;");
                            HDRText = HDRText.AddBefore(ref success, "shared cbuffer cbHDRData : REGISTER(b, POST_PROCESS_CB_REGISTER)", "\r\nstatic const float exposureKey2 = 0.8;\r\n");

                            HDRText = HDRText.AddAfter(ref success, "shared StructuredBuffer<float> exposureBuffer: register(t2);", "\r\n\r\nfloat GetAvgLuminance(Texture2D lumTex, float2 texCoord){" +
                            "\r\n	return exp(lumTex.Sample(samClamp, texCoord).x);" +
                            "\r\n}");

                            HDRText = HDRText.AddAfter(ref success, "shared StructuredBuffer<float> exposureBuffer: register(t2);", "\r\n\r\nfloat LinearExposure(float GetAvgLuminance){" +
                            "\r\n    return (exposureKey2 + toneMapFactor2) / (GetAvgLuminance + toneMapFactor2);" +
                            "\r\n}");

                            HDRText = HDRText.AddAfter(ref success, "shared StructuredBuffer<float> exposureBuffer: register(t2);", "\r\n\r\nfloat3 ToneMapExposure(float3 E) {" +
                            $"\r\n	return pow(1 - exp(-E* {tweak.parameters[0].value}), {tweak.parameters[1].value});" +
                            "\r\n}");

                            HDRText = HDRText.ReplaceAll(ref success, "//ACES based ToneMapping curve, takes and outputs linear values.\r\nfloat3 ToneMap(float3 color)\r\n{  \r\n    const float A = 2.51;\r\n    const float B = 0.03;\r\n    const float C = 2.43;\r\n    const float D = 0.59;\r\n    const float E = 0.14;\r\n	return saturate((color * (A * color + B)) / (color * (C * color + D) + E));\r\n}\r\n", "float3 ToneMap(float3 color, float GetAvgLuminance){" +                          
                            "\r\n	float3 linearColor = color * LinearExposure(GetAvgLuminance);" +
                            "\r\n    float3 tonmappedColor = ToneMapExposure(linearColor);\r\n" +
                            "\r\n    return tonmappedColor;" +
                            "\r\n}\r\n\r\n\r\n\r\n");

                            HDRText = HDRText.ReplaceAll(ref success, "	color.rgb = ToneMap(color.rgb);", "	color.rgb = ToneMap(color.rgb, 1.0);");

                            break;
                         #endregion

                    }


                    if (!success && supported)
                    {
                        Log(ErrorType.Error, "Failed to apply tweak [" + tweak.name + "] in " + currentFile + " file.");
                    }
                    else if (!success && !supported)
                    {
                        Log(ErrorType.Warning, "Did not apply tweak [" + tweak.name + "] in " + currentFile + " file. Tweak is not supported.");
                    }
                    else
                    {
                        tweakCount++;
                        Log(ErrorType.None, "Tweak [" + tweak.name + "] applied.");
                    }
                }
            }


            try
            {
                File.WriteAllText(shaderDirectory + FileIO.cloudFile, cloudText);
                File.WriteAllText(shaderDirectory + FileIO.generalFile, generalText);
                File.WriteAllText(shaderDirectory + FileIO.shadowFile, shadowText);
                File.WriteAllText(shaderDirectory + FileIO.funclibFile, funclibText);
                File.WriteAllText(shaderDirectory + FileIO.terrainFile, terrainText);
                File.WriteAllText(shaderDirectory + FileIO.terrainFXHFile, terrainFXHText);
                File.WriteAllText(shaderDirectory + FileIO.shadowFile, shadowText);
                File.WriteAllText(shaderDirectory + FileIO.PBRFile, PBRText);
                File.WriteAllText(shaderDirectory + FileIO.compositeFile, compositeText);
                File.WriteAllText(shaderDirectory + "PostProcess\\" + FileIO.HDRFile, HDRText);
            }
            catch
            {
                Log(ErrorType.Error, "Could not write tweaks to shader files.");
                return;
            }

            if (loadedPreset != null)
            {
                activePresetPath = loadedPresetPath;
                activePreset = loadedPreset;
                ActivePreset_TextBlock.Text = activePreset.filename;

                Log(ErrorType.None, "Preset [" + activePreset.filename + "] applied. "
                    + tweakCount + "/" + tweaks.Count(p => p.isEnabled == true) + " tweaks applied. ");
            }
            else {
                activePresetPath = loadedPresetPath; // becomes null
                activePreset = loadedPreset; // becomes null
                ActivePreset_TextBlock.Text = "";

                Log(ErrorType.None, "Tweaks applied. "
                    + tweakCount + "/" + tweaks.Count(p => p.isEnabled == true) + " tweaks applied. ");
            }

            try
            {
                fileData.ClearDirectory(cacheDirectory);
                Log(ErrorType.None, "Shader cache cleared");
            }
            catch
            {
                Log(ErrorType.Error, "Could not clear shader cache.");                
            }

            ClearChangesInfo(tweaks);
            ClearChangesInfo(postProcesses);
        }

        private void ResetShaderFiles(object sender, RoutedEventArgs e)
        {
            if (fileData.CopyShaderFiles(backupDirectory, shaderDirectory))
            {
                activePresetPath = null;
                activePreset = null;
                ActivePreset_TextBlock.Text = "";

                Log(ErrorType.None, "Shader files restored");

                if (fileData.ClearDirectory(cacheDirectory))
                {
                    Log(ErrorType.None, "Shader cache cleared");
                }
                else
                {
                    Log(ErrorType.Error, "Could not clear shader cache.");
                }
            }
        }

        private void ClearShaders_Click(object sender, RoutedEventArgs e)
        {
            if (fileData.ClearDirectory(cacheDirectory))
            {
                Log(ErrorType.None, "Shader cache cleared");
            }
            else
            {
                Log(ErrorType.Error, "Could not clear shader cache.");
            }
        }


        private void ResetToPreset(object sender, RoutedEventArgs e)
        {
            loadedPresetPath = activePresetPath;
            loadedPreset = activePreset;
            LoadedPreset_TextBlock.Text = loadedPreset.filename;
            LoadPreset(activePreset, false);

            List_SelectionChanged(Tweak_List, TweakStack, TweakClearStack, TweakTitleTextblock, TweakDescriptionTextblock);

            Log(ErrorType.None, "Active preset parameters restored.");
        }

        private void ResetToDefaults(object sender, RoutedEventArgs e)
        {
            foreach (var tweak in tweaks)
            {                
                foreach (var param in tweak.parameters)
                {
                    param.value = param.defaultValue;
                }
                tweak.isEnabled = false;
            }

            foreach (var post in postProcesses)
            {               
                foreach (var param in post.parameters)
                {
                    param.value = param.defaultValue;
                }
                post.isEnabled = false;
            }

            customTweaks.Clear();

            List_SelectionChanged(Tweak_List, TweakStack, TweakClearStack, TweakTitleTextblock, TweakDescriptionTextblock);

            Tweak_List.Items.Refresh();

             
            Log(ErrorType.None, "Parameters reset to default");
        }

        private void ClearChangesInfo<T>(List<T> effectsList) {
            foreach (T entry in effectsList)
            {
                BaseTweak effect = entry as BaseTweak;
                effect.wasEnabled = effect.isEnabled;
                foreach (var param in effect.parameters) {
                    param.oldValue = param.value;
                }                
            }    
            
        }

        public void ChangeMenuBarState(bool enable)
        {
            //NewPreset_btn.IsEnabled = enable;
            //OpenPreset_btn.IsEnabled = enable;
            //SavePreset_btn.IsEnabled = enable;
            //SavePresetAs_btn.IsEnabled = enable;
            //ApplyPreset_btn.IsEnabled = enable;
            //ResetShaderFiles_btn.IsEnabled = enable;
            //ClearShaders_btn.IsEnabled = enable;
            //ResetToDefaults_btn.IsEnabled = enable;
            //ResetToPreset_btn.IsEnabled = enable;
        }

        public void Log(ErrorType type, string message)
        {

            string typeString = "";
            SolidColorBrush color = Brushes.Black;

            switch (type)
            {
                case ErrorType.None:
                    typeString = "Success";
                    color = Brushes.Green;
                    break;
                case ErrorType.Warning:
                    typeString = "Warning";
                    color = Brushes.Orange;
                    break;
                case ErrorType.Error:
                    typeString = "Error";
                    color = Brushes.Red;
                    break;
                case ErrorType.Info:
                    typeString = "Information";
                    color = Brushes.Black;
                    break;
            }


            Paragraph para = new Paragraph();
            para.Inlines.Add(new Bold(new Run(DateTime.Now.ToLongTimeString() + " - ")));
            para.Inlines.Add(new Bold(new Run(typeString)) { Foreground = color });
            para.Inlines.Add(new Run(": " + message));

            Log_RichTextBox.Document.Blocks.Add(para);
            Log_RichTextBox.ScrollToEnd();
        }
    }


}
