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
using System.Security.Policy;

namespace OpenShade
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public enum ErrorType { None, Warning, Error, Info };

    public partial class MainWindow : Window
    {
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
        public string GeneralShaderMD5HashHardCode = "73F32C32CFDC62E60F3ADCDBBE0FF8A2";
        public string FuncLibShaderMD5HashHardCode = "FF7BF76D7D73DDC65383F6928A79B113";
        public string TerrainShaderMD5HashHardCode = "9E1CB526E2E166CC628DCBECE4118698";
        public string HDRShaderMD5HashHardCode = "9EDF0627E6ABB3180EBA83A3FDF210BE";


        FileIO fileData;
        string shaderDirectory;
        public string backupDirectory;

        public string activePresetPath;
        IniFile activePreset;
        public string loadedPresetPath;
        IniFile loadedPreset;

        // TODO: put this in a struct somewhere
        public static string cloudText, generalText, terrainText, funclibText, terrainFXHText, shadowText, HDRText, PBRText, compositeText, PrecipParticleText;

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


            string currentP3DEXEVersion = FileVersionInfo.GetVersionInfo(P3DDirectory + "Prepar3D.exe").FileVersion;
            string hashGeneral = fileData.MD5IntegrityCheck(shaderDirectory + FileIO.generalFile);
            string hashFuncLib = fileData.MD5IntegrityCheck(shaderDirectory + FileIO.funclibFile);
            string hashTerrain = fileData.MD5IntegrityCheck(shaderDirectory + FileIO.terrainFile);
            string hashHDR = fileData.MD5IntegrityCheck(shaderDirectory + "PostProcess\\" + FileIO.HDRFile);
            Log(ErrorType.Info, "You currently running P3D Version: " + currentP3DEXEVersion);




            if (!Directory.Exists(backupDirectory))
            {
                //Check installed shaders and p3d version
                if (hashGeneral != GeneralShaderMD5HashHardCode && hashFuncLib != FuncLibShaderMD5HashHardCode && hashTerrain != TerrainShaderMD5HashHardCode && hashHDR != HDRShaderMD5HashHardCode)
                {
                    MessageBoxResult result = MessageBox.Show("Non default P3D Shaders detected, please restore the original shaders before trying again!", "Integrity Check", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.OK);
                    Log(ErrorType.Error, "Non default Shaders detected, Openshade can not run!");
                    if (result == MessageBoxResult.OK)
                    {
                        System.Environment.Exit(0);
                    }
                }

                if (currentP3DEXEVersion != P3DVersion)
                {
                    MessageBoxResult result = MessageBox.Show("You have an old P3D Version installed, please update to the latest version!", "Unsupported Version", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.OK);
                    Log(ErrorType.Warning, "Unsupported P3D Version OpenShade can not run.");
                    if (result == MessageBoxResult.OK)
                    {
                        System.Environment.Exit(0);
                    }
                }
            }




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
                    //ActivePreset_TextBlock.Text = activePreset.filename;                    
                }
                else
                {
                    Log(ErrorType.Error, "Active Preset file [" + activePresetPath + "] not found");
                }
            }

            // Load Backup files
            ShaderBackup_TextBox.Text = backupDirectory;

            // Show P3D Version Info and some Debug stuff
            //string currentP3DEXEVersion = FileVersionInfo.GetVersionInfo(P3DDirectory + "Prepar3D.exe").FileVersion;
            Log(ErrorType.Info, "You currently running P3D Version: " + currentP3DEXEVersion);
            Log(ErrorType.Info, "Application Version: " + P3DVersion);
            Log(ErrorType.Info, "Current P3D Path: " + P3DDirectory);
            Log(ErrorType.Info, "Current Backup Directory: " + backupDirectory);
            CurrentP3DVersionText.Text = currentP3DEXEVersion;

            //Handling Current P3D Version
            if (Directory.Exists(backupDirectory))
            {
                string currentP3DVersion = FileVersionInfo.GetVersionInfo(P3DDirectory + "Prepar3D.exe").FileVersion;
                if (currentP3DVersion != P3DVersion)
                {
                    MessageBoxResult result = MessageBox.Show("OpenShade has detected a new version of Prepar3D (" + currentP3DVersion + ").\r\n\r\nIt is STRONGLY recommended that you backup the default shader files again otherwise they will be overwritten by old shader files when applying a preset.", "New version detected", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.OK);
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
            else
            {

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
            string currentP3DEXEVersion = FileVersionInfo.GetVersionInfo(P3DDirectory + "Prepar3D.exe").FileVersion;
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
                    if (currentP3DEXEVersion == P3DVersion){
                        MessageBoxResult result = MessageBox.Show("Some changes were not saved.\r\nWould you like to save them now as a new preset [" + loadedPreset.filename + "] ?", "Save", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                        if (result == MessageBoxResult.Yes)
                        {
                            SavePreset_Click(null, null);
                        }
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
                    Button TweakDescriptionButton = new Button();
                    TweakDescriptionButton.Content = "Images";
                    TweakDescriptionButton.ToolTip = "Show Images and detailed tweak description";
                    TweakDescriptionButton.Width = 100;
                    TweakDescriptionButton.Height = 25;
                    TweakDescriptionButton.VerticalAlignment = VerticalAlignment.Top;
                    TweakDescriptionButton.HorizontalAlignment = HorizontalAlignment.Right;
                    TweakDescriptionButton.Margin = new Thickness(0, 0, 0, 10);
                    TweakDescriptionButton.Click += new RoutedEventHandler(ShowDescriptionImage);
                    clearStack.Children.Add(TweakDescriptionButton);

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
                            txtbox.Width = 240;
                            txtbox.Height = 20;
                            txtbox.VerticalAlignment = VerticalAlignment.Top;
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

            //if (currentTab.Name == "Tweak_Tab") { param = ((Tweak)Tweak_List.SelectedItem).parameters.First(p => p.id == checkbox.Uid); }
            param = ((Tweak)Tweak_List.SelectedItem).parameters.First(p => p.id == checkbox.Uid);
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
            //if (currentTab.Name == "Tweak_Tab") { tb = (TextBox)HelperFunctions.FindUid(TweakStack, param.id + "-changeTxtbox"); }
            tb = (TextBox)HelperFunctions.FindUid(TweakStack, param.id + "-changeTxtbox");
            tb.Visibility = param.hasChanged ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ParameterText_KeyUp(object sender, EventArgs e)
        {
            TextBox txtBox = (TextBox)sender;
            TabItem currentTab = HelperFunctions.FindAncestorOrSelf<TabItem>(txtBox);            
            Parameter param = null;

            //if (currentTab.Name == "Tweak_Tab") { param = ((Tweak)Tweak_List.SelectedItem).parameters.First(p => p.id == txtBox.Uid); }

            param = ((Tweak)Tweak_List.SelectedItem).parameters.First(p => p.id == txtBox.Uid);
            //if (currentTab.Name != "Custom_Tab") {                
            param.value = txtBox.Text;

                TextBox tb = null;
            //if (currentTab.Name == "Tweak_Tab") { tb = (TextBox)HelperFunctions.FindUid(TweakStack, param.id + "-changeTxtbox"); }
            tb = (TextBox)HelperFunctions.FindUid(TweakStack, param.id + "-changeTxtbox");
            tb.Visibility = param.hasChanged ? Visibility.Visible : Visibility.Collapsed;
            //}
        }

        private void ParameterSpinner_ValueChanged(object sender, EventArgs e)
        {
            NumericSpinner spinner = (NumericSpinner)sender;
            TabItem currentTab = HelperFunctions.FindAncestorOrSelf<TabItem>(spinner);
            Parameter param = null;

            //if (currentTab.Name == "Tweak_Tab") { param = ((Tweak)Tweak_List.SelectedItem).parameters.First(p => p.id == spinner.Uid); }
            param = ((Tweak)Tweak_List.SelectedItem).parameters.First(p => p.id == spinner.Uid);

            //if (currentTab.Name != "Custom_Tab") {                
            param.value = spinner.Value.ToString();

                TextBox tb = null;
            //if (currentTab.Name == "Tweak_Tab") { tb = (TextBox)HelperFunctions.FindUid(TweakStack, param.id + "-changeTxtbox"); }
            tb = (TextBox)HelperFunctions.FindUid(TweakStack, param.id + "-changeTxtbox");
            tb.Visibility = param.hasChanged ? Visibility.Visible : Visibility.Collapsed;
            //}
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

            //if (currentTab.Name == "Tweak_Tab") { param = ((Tweak)Tweak_List.SelectedItem).parameters.First(p => p.id == uid); }
            param = ((Tweak)Tweak_List.SelectedItem).parameters.First(p => p.id == uid);
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
            //if (currentTab.Name == "Tweak_Tab") { tb = (TextBox)HelperFunctions.FindUid(TweakStack, param.id + "-changeTxtbox"); }
            tb = (TextBox)HelperFunctions.FindUid(TweakStack, param.id + "-changeTxtbox");
            tb.Visibility = param.hasChanged ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;
            TabItem currentTab = HelperFunctions.FindAncestorOrSelf<TabItem>(combo);
            Parameter param = null;

            //if (currentTab.Name == "Tweak_Tab") { param = ((Tweak)Tweak_List.SelectedItem).parameters.First(p => p.id == combo.Uid); }
            param = ((Tweak)Tweak_List.SelectedItem).parameters.First(p => p.id == combo.Uid);

            //if (currentTab.Name != "Custom_Tab") {                
            param.value = combo.SelectedIndex.ToString();

                TextBox tb = null;
            //if (currentTab.Name == "Tweak_Tab") { tb = (TextBox)HelperFunctions.FindUid(TweakStack, param.id + "-changeTxtbox"); }
            tb = (TextBox)HelperFunctions.FindUid(TweakStack, param.id + "-changeTxtbox");
            tb.Visibility = param.hasChanged ? Visibility.Visible : Visibility.Collapsed;
            //}
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

            //if (currentTab.Name == "Tweak_Tab") { currentList = Tweak_List; }
            currentList = Tweak_List;

            BaseTweak selectedEffect = (BaseTweak)currentList.SelectedItem;

            foreach (var param in selectedEffect.parameters)
            {
                param.value = param.defaultValue;
            }

            //if (currentTab.Name == "Tweak_Tab") { List_SelectionChanged(Tweak_List, TweakStack, TweakClearStack, TweakTitleTextblock, TweakDescriptionTextblock); }
            List_SelectionChanged(Tweak_List, TweakStack, TweakClearStack, TweakTitleTextblock, TweakDescriptionTextblock);
        }

        private void ResetParametersPreset_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            TabItem currentTab = HelperFunctions.FindAncestorOrSelf<TabItem>(btn);

            ListView currentList = null;

            currentList = Tweak_List;

            BaseTweak selectedEffect = (BaseTweak)currentList.SelectedItem;

            foreach (var param in selectedEffect.parameters)
            {
                param.value = param.oldValue;
            }
            List_SelectionChanged(Tweak_List, TweakStack, TweakClearStack, TweakTitleTextblock, TweakDescriptionTextblock);
            //if (currentTab.Name == "Tweak_Tab") { List_SelectionChanged(Tweak_List, TweakStack, TweakClearStack, TweakTitleTextblock, TweakDescriptionTextblock); }            
        }

        private void ShowDescriptionImage(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            TabItem currentTab = HelperFunctions.FindAncestorOrSelf<TabItem>(btn);

            ListView currentList = null;

            currentList = Tweak_List;
            BaseTweak selectedEffect = (BaseTweak)currentList.SelectedItem;




            switch(selectedEffect.name)
            {
                case "Terrain Lighting":
                    OpenShade.Pages.TerrainLightingCompare TerrainLighting = new OpenShade.Pages.TerrainLightingCompare();
                    TerrainLighting.Show();
                    break;

                case "Terrain Saturation":
                    OpenShade.Pages.TerrainSaturationCompare TerrainSaturation = new OpenShade.Pages.TerrainSaturationCompare();
                    TerrainSaturation.Show();
                    break;

                case "Terrain Reflectance":
                    OpenShade.Pages.TerrainReflectanceCompare TerrainReflectance = new OpenShade.Pages.TerrainReflectanceCompare();
                    TerrainReflectance.Show();
                    break;

                case "Advanced PBR":
                    OpenShade.Pages.AdvancedPBRCompare AdvancedPBR = new OpenShade.Pages.AdvancedPBRCompare();
                    AdvancedPBR.Show();
                    break;
            }
        }



        #endregion

        #region Settings
        //private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (IsLoaded)
        //    {
        //        ((App)Application.Current).ChangeTheme((Themes)Theme_ComboBox.SelectedItem);
        //    }
        //}

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
                else
                {
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

                            ////interpolate_sun_angle
                            //funclibText = funclibText.AddBefore(ref success, "float4 EnhancedAtmosphericsBlend(float4 color, float3 screenPos, float3 worldPos, uint InstanceID)", "\r\nfloat interpolate_sun_angle(float sun_angle, float day_value, float twilight_value, float night_value)" +
                            //"\r\n{" +
                            //"\r\n    float output_value;\r\n" +
                            //"\r\n    if (sun_angle > 0.0) output_value = map(sqrt(sun_angle), sqrt(0.175), 0.0, day_value, twilight_value);" +
                            //"\r\n    else output_value = map(sun_angle, -0.125, -0.25, twilight_value, night_value); \r\n" +
                            //"\r\n    return output_value;" +
                            //"\r\n}\r\n");

                            //square
                            funclibText = funclibText.AddBefore(ref success, "float4 EnhancedAtmosphericsBlend(float4 color, float3 screenPos, float3 worldPos, uint InstanceID)", "\r\nfloat square(float value)" +
                            "\r\n{" +
                            "\r\n    return value * value;" +
                            "\r\n}\r\n");

                            //interpolate_two_values
                            funclibText = funclibText.AddBefore(ref success, "float4 EnhancedAtmosphericsBlend(float4 color, float3 screenPos, float3 worldPos, uint InstanceID)", "\r\nfloat interpolate_two_values(float value_1, float value_2, float sun_angle)" +
                            "\r\n{" +
                            "\r\n    float output_value;" +
                            "\r\n    if (sun_angle > 0.0) output_value = map(square(sun_angle), square(0.175), 0.0, value_1, value_2);" +
                            "\r\n    else output_value = value_2;" +
                            "\r\n    return output_value;" +
                            "\r\n}\r\n");

                            //interpolate_three_values
                            funclibText = funclibText.AddBefore(ref success, "float4 EnhancedAtmosphericsBlend(float4 color, float3 screenPos, float3 worldPos, uint InstanceID)", "\r\nfloat interpolate_three_values(float value_1, float value_2, float value_3, float sun_angle)" +
                            "\r\n{" +
                            "\r\n    float output_value;" +
                            "\r\n    if (sun_angle > 0.0) output_value = map(sqrt(sun_angle), sqrt(0.175), 0.0, value_1, value_2);" +
                            "\r\n    else output_value = map(sun_angle, -0.125, -0.25, value_2, value_3);" +
                            "\r\n    return output_value;" +
                            "\r\n}\r\n");


                            //get_luminance
                            funclibText = funclibText.AddBefore(ref success, "float4 EnhancedAtmosphericsBlend(float4 color, float3 screenPos, float3 worldPos, uint InstanceID)", "\r\nfloat3 get_luminance(float3 input_color)" +
                            "\r\n{" +
                            "\r\n    float output_luminance = dot(input_color, float3(0.2126, 0.7152, 0.0722));" +
                            "\r\n    return float3(output_luminance, output_luminance, output_luminance);" +
                            "\r\n}\r\n");


                            //Add main Tweaks in func lib
                            funclibText = funclibText.AddBefore(ref success, "#if !defined(SHD_NO_FOG)", $"        insc = lerp(insc, insc * normalized_ozone_coefficient, interpolate_three_values({ tweak.parameters[0].value}, { tweak.parameters[1].value}, { tweak.parameters[2].value}, cb_mSun.mDirection.y));" +
                            $"\r\n        insc = interpolate_three_values({ tweak.parameters[3].value}, { tweak.parameters[4].value}, { tweak.parameters[5].value}, cb_mSun.mDirection.y) *lerp(get_luminance(insc), insc, interpolate_three_values({ tweak.parameters[6].value}, { tweak.parameters[7].value}, { tweak.parameters[8].value}, cb_mSun.mDirection.y));\r\n");


                            //Add main Tweaks in composite
                            currentFile = FileIO.compositeFile;
                            compositeText = compositeText.AddAfter(ref success, "        loss = txBindless(cb_mLoss2DTextureIndex).SampleLevel(samClamp, lossUV, 0);", $"\r\n\r\n        insc.rgb = lerp(insc.rgb, insc.rgb * normalized_ozone_coefficient, interpolate_three_values({ tweak.parameters[0].value}, { tweak.parameters[1].value}, { tweak.parameters[2].value}, cb_mSun.mDirection.y));" +
                            $"\r\n        insc.rgb = interpolate_three_values({ tweak.parameters[3].value}, { tweak.parameters[4].value}, { tweak.parameters[5].value}, cb_mSun.mDirection.y) * lerp(get_luminance(insc.rgb), insc.rgb, interpolate_three_values({ tweak.parameters[6].value}, { tweak.parameters[7].value}, { tweak.parameters[8].value}, cb_mSun.mDirection.y));");
                            break;

                        case "Enhanced Atmospherics Clouds":

                            //Add Cloud Tweak in composite
                            currentFile = FileIO.compositeFile;
                            compositeText = compositeText.AddAfter(ref success, "            clouds = lerp(clouds, nearClouds, cloudFade);", $"\r\n\r\n            clouds.rgb = lerp(clouds.rgb, clouds.rgb * normalized_ozone_coefficient, interpolate_three_values({ tweak.parameters[0].value}, { tweak.parameters[1].value}, { tweak.parameters[2].value}, cb_mSun.mDirection.y));" +
                            $"\r\n            clouds.rgb = interpolate_three_values({ tweak.parameters[3].value}, { tweak.parameters[4].value}, { tweak.parameters[5].value}, cb_mSun.mDirection.y) * lerp(get_luminance(clouds.rgb), clouds.rgb, interpolate_three_values({ tweak.parameters[6].value}, { tweak.parameters[7].value}, { tweak.parameters[8].value}, cb_mSun.mDirection.y));\r\n");

                            //Add Cloud Tweak in cloudfx
                            cloudText = cloudText.AddAfter(ref success, "    #if defined(SHD_ENHANCED_ATMOSPHERICS_BLEND) ", "\r\nstatic const float3 normalized_ozone_coefficient = float3(0.61344749, 0.0, 1.0);\r\n");

                            currentFile = FileIO.cloudFile;
                            cloudText = cloudText.AddBefore(ref success, "        cColor = EnhancedAtmosphericsBlend(cColor, In.position.xyw, viewInstanceWS, InstanceID);", $"\r\n\r\n            cColor.rgb = lerp(cColor.rgb, cColor.rgb * normalized_ozone_coefficient, interpolate_three_values({ tweak.parameters[0].value}, { tweak.parameters[1].value}, { tweak.parameters[2].value}, cb_mSun.mDirection.y));" +
                            $"\r\n            cColor.rgb = interpolate_three_values({ tweak.parameters[3].value}, { tweak.parameters[4].value}, { tweak.parameters[5].value}, cb_mSun.mDirection.y) * lerp(get_luminance(cColor.rgb), cColor.rgb, interpolate_three_values({ tweak.parameters[6].value}, { tweak.parameters[7].value}, { tweak.parameters[8].value}, cb_mSun.mDirection.y));");
                            break;
                        #endregion

                        #region Clouds       
                        case "'No popcorn' clouds":
                            currentFile = FileIO.cloudFile;
                            cloudText = cloudText.AddAfter(ref success, "void GetPointDiffuse( out float4 diffuse, in float3 corner, in float3 groupCenter", ", in float cloudDistance");
                            cloudText = cloudText.AddAfter(ref success, "float  fIntensity = -1.0f * max(dot(lightDirection, cloudGroupNormal), dot(lightDirection, facingDirection));", $@"
                            const float fExp = saturate(exp(-cloudDistance * cloudDistance * {tweak.parameters[0].value} ));
                            fIntensity = lerp(0.36f, fIntensity, fExp);");
                            cloudText = cloudText.AddAfter(ref success, "diffuse = saturate(float4(colorIntensity.rgb + (0.33f * saturate(colorIntensity.rgb - 1)), colorIntensity.a));", "\r\nif (diffuse.a > " + tweak.parameters[1].value.ToString() + ") { diffuse.a = lerp(" + tweak.parameters[1].value.ToString() + ", diffuse.a, fExp); }");
                            cloudText = cloudText.AddBefore(ref success, "rotationMatrix[2] = normalize(cameraFacingVector);", " float cloudDistance = length(positionVector);");
                            cloudText = cloudText.ReplaceAll(ref success, "GetPointDiffuse(Out.diffuse[i], position, spriteCenter.xyz, rotationMatrix[2], cloudMaterial);", "GetPointDiffuse(Out.diffuse[i], position, spriteCenter.xyz, cloudDistance, rotationMatrix[2], cloudMaterial);");
                            cloudText = cloudText.ReplaceAll(ref success, "GetPointDiffuse( Out.diffuse[i], position, spriteCenter.xyz, rotationMatrix[2], cloudMaterial);", "GetPointDiffuse( Out.diffuse[i], position, spriteCenter.xyz, cloudDistance, rotationMatrix[2], cloudMaterial);");
                        break;

                        case "Alternate lighting for cloud groups":
                            currentFile = FileIO.cloudFile;
                            cloudText = cloudText.ReplaceAll(ref success, "GetPointDiffuse(Out.diffuse[i], position, spriteCenter.xyz", "GetPointDiffuse(Out.diffuse[i], position, groupCenter.xyz");
                            cloudText = cloudText.ReplaceAll(ref success, "GetPointDiffuse( Out.diffuse[i], position, spriteCenter.xyz", "GetPointDiffuse(Out.diffuse[i], position, groupCenter.xyz");
                        break;

                        case "Cirrus lighting":
                            currentFile = FileIO.generalFile;
                            generalText = generalText.AddBefore(ref success, "// Apply IR if active", "if (cb_mObjectType == (uint)3)\r\n    {\r\n        cColor.rgb = " + tweak.parameters[0].value.ToString() + " * saturate(lerp(dot(cColor.rgb, float3(0.299f, 0.587f, 0.114f)), cColor.rgb, " + tweak.parameters[1].value.ToString() + "));\r\n   }\r\n\r\n");
                        break;
                            
                        case "Cloud light scattering":
                            currentFile = FileIO.cloudFile;
                            cloudText = cloudText.CommentOut(ref success, "if (fIntensity < -cb_mMedianLine)", "    fIntensity = clamp(fIntensity, 0, 1);", false);
                            cloudText = cloudText.AddBefore(ref success, "/*if (fIntensity < -cb_mMedianLine)", $@"const float fScatter = {tweak.parameters[0].value} + cb_mDayNightInterpolant + (cb_mView.fPrecipitationLevel*0.1);
                            fIntensity =  saturate(fScatter * fIntensity + {tweak.parameters[1].value});
                            ");

                            if (tweak.parameters[2].value == "1")
                            {
                                cloudText = cloudText.CommentOut(ref success, "float height = corner.y;", "float4 color = lerp(baseColor, topColor, s);", true);
                                cloudText = cloudText.ReplaceAll(ref success, "float4 colorIntensity = float4(fColor.r, fColor.g, fColor.b, saturate(alpha)) * color;", "float4 colorIntensity = float4(fColor.r, fColor.g, fColor.b, saturate(alpha));");
                            }
                        break;

                        case "Cloud lighting tuning":
                            currentFile = FileIO.cloudFile;
                            cloudText = cloudText.ReplaceAll(ref success, "diffuse = saturate(float4(colorIntensity.rgb + (0.33f * saturate(colorIntensity.rgb - 1)), colorIntensity.a));", "diffuse = saturate( float4( " + tweak.parameters[0].value.ToString() + " * colorIntensity.rgb + ( " + tweak.parameters[1].value.ToString() + " * saturate(colorIntensity.rgb - 1)), colorIntensity.a));");
                        break;

                        case "Cloud saturation":
                            currentFile = FileIO.cloudFile;
                            cloudText = cloudText.AddAfter(ref success, "* saturate(colorIntensity.rgb - 1)), colorIntensity.a));", "\r\ndiffuse.rgb = saturate(lerp(dot(diffuse.rgb, float3(0.299f, 0.587f, 0.114f)), diffuse.rgb, " + tweak.parameters[0].value.ToString() + "));");
                        break;

                        case "Cloud shadow depth":
                            currentFile = FileIO.cloudFile;
                            cloudText = cloudText.ReplaceAll(ref success, "    return float2(1.0f-cColor.a,In.z);", $"    return float2(1.0f-cColor.a * {tweak.parameters[0].value},In.z);");
                        break;

                        case "Cloud shadow extended size":
                            currentFile = FileIO.cloudFile;
                            cloudText = cloudText.ReplaceFirst(ref success, "Out.position[i] = mul(float4(position, 1.0), matWorld);", "Out.position[i] = mul(float4(position, 0.8), matWorld);");
                        break;

                        case "Reduce cloud brightness at dawn/dusk/night":
                            currentFile = FileIO.cloudFile;
                            cloudText = cloudText.AddAfter(ref success, "float3 fColor = fIntensity * cb_mCombinedDiffuse.rgb + cb_mCombinedAmbient.rgb;", "\r\n    float fCumulusDusk = 1 + saturate(fColor.g/(cb_View.mFogColor.g + 0.00001) - 2);\r\n    fColor /= fCumulusDusk;\r\n if (cb_mDayNightInterpolant > 0.9){ \r\n        fColor = fColor*0.1;\r\n    }\r\n ");
                            generalText = generalText.AddAfter(ref success, "#endif //SHD_ALPHA_TEST", "\r\nif (cb_mObjectType == (uint)3)\r\n {\r\n      float fCirrusDusk = 1 + saturate(cColor.g / (cb_View.mFogColor.g + 0.00001) - 2);\r\n     cColor.rgb /= fCirrusDusk;\r\n     if (cb_mDayNightInterpolant > 0.9){ \r\n         cColor.rgb = cColor.rgb*0.1;\r\n     }\r\n  }\r\n");
                        break;

                        case "Cloud puffs width and height scaling":
                            currentFile = FileIO.cloudFile;
                            cloudText = cloudText.ReplaceAll(ref success, "GetScreenQuadPositions(quad, width*0.5, height*0.5);", "GetScreenQuadPositions(quad, width*" + tweak.parameters[0].value.ToString() + ", height*" + tweak.parameters[1].value.ToString() + ");");
                        break;


                        #endregion

                        #region Atmosphere

                        case "Atmospheres Fog Fix":
                            currentFile = FileIO.funclibFile;

                            if (tweak.parameters[0].value == "1")
                            {
                                funclibText = funclibText.ReplaceAll(ref success, "#if !defined(SHD_ENHANCED_ATMOSPHERICS_BLEND) && !defined(SHD_TO_FAR_CLIP) && !defined(SHD_SKY_STARS)", "#if !defined(SHD_ENHANCED_ATMOSPHERICS_BLEND) && !defined(SHD_TO_FAR_CLIP)");
                                PBRText = PBRText.AddAfter(ref success, "    #elif !defined(SHD_NO_FOG) && !defined(INPUT_SCREENSPACE_POSITION)", "\r\n        const float pixelDistance = distance(float3(0, 0, 0), Input.vPositionWS);");
                            }
                        break;

                        case "Atmospheres Haze Effect":
                            currentFile = FileIO.funclibFile;

                            Tweak FogFix = tweaks.First(p => p.name == "Atmospheres Fog Fix");
                            if (FogFix.isEnabled == false) {
                                Log(ErrorType.Error, "Please activate the 'Atmospheres Fog Fix' tweak first!");
                                break;
                            }

                            //HazeEffect
                                funclibText = funclibText.AddBefore(ref success, "float4 EnhancedAtmosphericsBlend(float4 color, float3 screenPos, float3 worldPos, uint InstanceID)", "\r\nfloat4 CalculateHazeEffectFog(const float fPositonY, const float4 fColor, const float fDistance){" +
                                "\r\n    float4 FinalColor = fColor;" +
                                "\r\n	#if !defined(SHD_ADDITIVE) && !defined(SHD_MULTIPLICATIVE)" +
                                "\r\n        if ((cb_mObjectType != (uint)1) && (cb_mObjectType != (uint)3) && (cb_mObjectType != (uint)21) && (cb_mObjectType != (uint)19))" +
                                "\r\n        {" +
                                $"\r\n            FinalColor.rgb = lerp(pow(saturate(cb_View.mFogColor.rgb * float3({tweak.parameters[2].value}, {tweak.parameters[3].value}, {tweak.parameters[4].value})), (1 + saturate(cb_mSun.mDiffuse.g - 0.35f)) * {tweak.parameters[0].value})," +
                                $"\r\n            FinalColor.rgb, saturate(exp(-fDistance * fDistance * {tweak.parameters[1].value})));" +
                                "\r\n        }" +
                                "\r\n    #endif" +
                                "\r\n    return FinalColor;" +
                                "\r\n}\r\n");

                                if (tweak.parameters[5].value == "1")
                                {
                                    funclibText = funclibText.ReplaceAll(ref success, $"            FinalColor.rgb, saturate(exp(-fDistance * fDistance * {tweak.parameters[1].value})));", $"            FinalColor.rgb, saturate(exp(-fDistance * fDistance *  {tweak.parameters[1].value} *  saturate(1.0f - cb_mView.mAltitude/{tweak.parameters[6].value}))));");
                                }


                                generalText = generalText.AddBefore(ref success, "        cColor = CalculateFog(cColor, ViewInstancedWS, InstanceID);", "\r\n        cColor = CalculateHazeEffectFog(Input.vPositionWS.y - cb_View.mViewInstanceOffset.y, cColor, pixelDistance);");
                                terrainText = terrainText.AddBefore(ref success, "            color = CalculateFog(color, ViewInstancedWS, InstanceID);", "\r\n            color = CalculateHazeEffectFog(Input.vPosWS.y, color, eyeDist);");
                                PBRText = PBRText.AddBefore(ref success, "        color = CalculateFog(color, ViewInstancedWS, InstanceID);", "\r\n        color = CalculateHazeEffectFog(Input.vPositionWS.y - cb_View.mViewInstanceOffset.y, color, pixelDistance);");
                                //PBRText = PBRText.AddAfter(ref success, "    #elif !defined(SHD_NO_FOG) && !defined(INPUT_SCREENSPACE_POSITION)", "\r\n        const float pixelDistance = distance(float3(0, 0, 0), Input.vPositionWS);");
                                cloudText = cloudText.AddBefore(ref success, "        cColor = CalculateFog(cColor, viewInstanceWS, InstanceID);", "\r\n       cColor = CalculateHazeEffectFog(positionWS.y - cb_View.mViewInstanceOffset.y, cColor, In.fFogDistance / 2.0);");     
                            break;


                     case "Atmosphere Rayleight Scattering":
                            currentFile = FileIO.funclibFile;


                            Tweak HazeEffect = tweaks.First(p => p.name == "Atmospheres Haze Effect");
                            if (HazeEffect.isEnabled == false)
                            {
                                Log(ErrorType.Error, "Please activate the 'Atmospheres Haze Effect' tweak first!");
                                break;
                            }

                            //Rayleigh
                            funclibText = funclibText.AddBefore(ref success, "float4 EnhancedAtmosphericsBlend(float4 color, float3 screenPos, float3 worldPos, uint InstanceID)", "\r\nfloat4 CalculateRayleightScatteringFog(const float fPositonY, const float4 fColor, const float fDistance){" +
                            "\r\n    float4 FinalColor = fColor;" +
                            "\r\n    #if !defined(SHD_ADDITIVE) && !defined(SHD_MULTIPLICATIVE)" +
                            "\r\n        if ((cb_mObjectType != (uint)1) && (cb_mObjectType != (uint)3) && (cb_mObjectType != (uint)21) && (cb_mObjectType != (uint)19))" +
                            "\r\n        {" +
                            $"\r\n            const float DensFactor = {tweak.parameters[1].value};" +
                            "\r\n            const float DistK = 2 * (1 - saturate(exp(-fDistance * fDistance * DensFactor))) * saturate(cb_mSun.mDiffuse.g - 0.15);" +
                            $"\r\n            FinalColor.rgb = FinalColor.rgb * (1 - float3(0.00, {tweak.parameters[2].value}, {tweak.parameters[3].value}) * DistK) + float3(0.00, {tweak.parameters[2].value}, {tweak.parameters[3].value}) * DistK;" +
                            "\r\n        }" +
                            "\r\n    #endif" +
                            "\r\n    return FinalColor;" +
                            "\r\n}\r\n");

                            if (tweak.parameters[4].value == "1")
                            {
                                funclibText = funclibText.ReplaceAll(ref success, $"            const float DensFactor = {tweak.parameters[1].value};", $"            const float DensFactor = 0.0000000002 * saturate(1.0f - cb_mView.mAltitude/{tweak.parameters[5].value});");
                            }


                            generalText = generalText.AddAfter(ref success, "    #elif !defined(SHD_NO_FOG) && !defined(INPUT_SCREENSPACE_POSITION)", "\r\n        cColor = CalculateRayleightScatteringFog(Input.vPositionWS.y - cb_View.mViewInstanceOffset.y, cColor, pixelDistance);");
                            terrainText = terrainText.AddAfter(ref success, "        #if !defined(SHD_NO_FOG)", "\r\n            color = CalculateRayleightScatteringFog(Input.vPosWS.y, color, eyeDist); //Draw Rayleigh");
                            PBRText = PBRText.AddAfter(ref success, "    #elif !defined(SHD_NO_FOG) && !defined(INPUT_SCREENSPACE_POSITION)", "\r\n        color = CalculateRayleightScatteringFog(Input.vPositionWS.y - cb_View.mViewInstanceOffset.y, color, pixelDistance);");
                            //PBRText = PBRText.AddAfter(ref success, "    #elif !defined(SHD_NO_FOG) && !defined(INPUT_SCREENSPACE_POSITION)", "\r\n        const float pixelDistance = distance(float3(0, 0, 0), Input.vPositionWS);");
                     break;


                        case "Sky Saturation":
                            currentFile = FileIO.generalFile;
                            generalText = generalText.AddBefore(ref success, "// Apply IR if active", "\r\n\r\n    if(cb_mObjectType ==(uint)1){ \r\n" +
                            $"\r\n        cColor.rgb = saturate(lerp(dot(cColor.rgb, float3(0.299f, 0.587f, 0.114f)), cColor.rgb, {tweak.parameters[0].value}));" +
                            "\r\n\r\n    }");
                        break;

                        case "Precipitation Opacity":
                            currentFile = FileIO.PrecipParticleFile;
                            PrecipParticleText = PrecipParticleText.ReplaceAll(ref success, "    finalColor.a = texAlpha.a * input.intensity * nearClipFade;", $"\r\n\r\n    finalColor.a = texAlpha.a * input.intensity * nearClipFade*{tweak.parameters[1].value};\r\n" +
                            "\r\n    if(cb_uPrecipType == 1){" +
                            $"\r\n    finalColor.a = texAlpha.a * input.intensity * nearClipFade*{tweak.parameters[0].value};" +
                            "\r\n\r\n    }");
                        break;
                        #endregion

                        #region Lighting
                        case "Cockpit Lighting":
                            currentFile = FileIO.generalFile;
                            //add the secondary DirectionalLighting in the fragment shader
                            generalText = generalText.AddBefore(ref success, "VS_OUTPUT VS( VS_INPUT Input )", "\r\nfloat3 DirectionalLighting2(const float3 vNormalWS, const float shadowContrib, out float3 DiffuseAndAmbient)" +
                            "\r\n{" +
                            $"\r\n#if defined(SHD_VERTICAL_NORMAL)" +
                            $"\r\n    const float fDotSun = max(cb_mSun.mDirection.y, 0);" +
                            $"\r\n    const float fDotMoon = max(cb_mMoon.mDirection.y, 0);" +
                            $"\r\n#else" +
                            $"\r\n    const float fDotSun = saturate(dot(vNormalWS, normalize(cb_mSun.mDirection)));" +
                            $"\r\n    const float fDotMoon = saturate(dot(vNormalWS, normalize(cb_mMoon.mDirection)));" +
                            $"\r\n#endif\r\n" +
                            $"\r\n    #if defined(PS_NEEDS_TANSPACE)" +
                            $"\r\n        if (cb_mObjectType ==19)" +
                            $"\r\n            DiffuseAndAmbient = (shadowContrib * (cb_mSun.mDiffuse.xyz * fDotSun)) + (shadowContrib * (cb_mMoon.mDiffuse.xyz * fDotMoon)) + cb_mCombinedAmbient.rgb;" +
                            $"\r\n        else" +
                            $"\r\n    #endif\r\n" +
                            $"\r\n    #if !defined(PS_NEEDS_TANSPACE)" +
                            $"\r\n        if (cb_mObjectType ==19)" +
                            $"\r\n            DiffuseAndAmbient = (shadowContrib * (cb_mSun.mDiffuse.xyz * {tweak.parameters[0].value} * fDotSun)) + (shadowContrib * (cb_mMoon.mDiffuse.xyz * fDotMoon)) + cb_mCombinedAmbient.rgb * {tweak.parameters[1].value};" +
                            $"\r\n        else" +
                            $"\r\n   #endif\r\n" +
                            $"\r\n        if (cb_mObjectType !=19)" +
                            $"\r\n            DiffuseAndAmbient = (shadowContrib * (cb_mSun.mDiffuse.xyz * fDotSun)) + (shadowContrib * (cb_mMoon.mDiffuse.xyz * fDotMoon)) + cb_mCombinedAmbient.rgb;" +
                            $"\r\n        else" +
                            $"\r\n            DiffuseAndAmbient = (shadowContrib * (cb_mSun.mDiffuse.xyz * fDotSun)) + (shadowContrib * (cb_mMoon.mDiffuse.xyz * fDotMoon)) + cb_mCombinedAmbient.rgb;" +
                            $"\r\n    return DiffuseAndAmbient;" +
                            "\r\n}");

                            //replace the old directional lighting with our secondary one
                            generalText = generalText.ReplaceAll(ref success, "directionalDiffuse = DirectionalLighting(vNormalWS, shadowContrib);", "directionalDiffuse = DirectionalLighting2(vNormalWS, shadowContrib, directionalDiffuse);");

                            //add VC saturation
                            generalText = generalText.AddBefore(ref success, "// Apply IR if active", "\r\n    #if !defined(PS_NEEDS_TANSPACE)" +
                            "\r\n        if (cb_mObjectType == 19){" +
                            $"\r\n            cColor.rgb = saturate(lerp(dot(cColor.rgb, float3(0.299f, 0.587f, 0.114f)), cColor.rgb, {tweak.parameters[2].value}));" +
                            "\r\n        }" +
                            "\r\n    #endif");

                            break;
                        #endregion

                        #region Terrain
                        case "Terrain Reflectance":
                            currentFile = FileIO.terrainFile;

                            //reflectance
                            terrainText = terrainText.ReplaceAll(ref success, "    float reflectance = 0.25;", $"    float reflectance = {tweak.parameters[0].value} ;");
                        break;

                        case "Terrain Lighting":
                            currentFile = FileIO.terrainFile;

                            //Diffuse 
                            terrainText = terrainText.ReplaceAll(ref success, "    float3 diffuseColor = CalculateDiffuseColor(baseColor.rgb, 0);", $"    float3 diffuseColor = CalculateDiffuseColor(baseColor.rgb, 0)* {tweak.parameters[0].value};");

                            //Ambient 
                            terrainText = terrainText.ReplaceAll(ref success, "    color.rgb += ambient;", $"    color.rgb += ambient * {tweak.parameters[1].value};");
                            
                            //Moon 
                            terrainText = terrainText.ReplaceAll(ref success, "	color.rgb = (sunContrib + moonContrib)* shadowContrib;", $"	color.rgb = (sunContrib + moonContrib * {tweak.parameters[2].value})* shadowContrib;");

                        break;


                        case "Terrain Saturation":
                            currentFile = FileIO.terrainFile;
                            //saturate 
                            terrainText = terrainText.AddBefore(ref success, "    //Apply emissive.", $"    color.rgb = saturate(lerp(dot(color.rgb, float3(0.299f, 0.587f, 0.114f)), color.rgb, {tweak.parameters[0].value}));\r\n");
                        break;
                        #endregion


                        #region PBR
                        //case "Aircraft PBR brightness":
                        //    currentFile = FileIO.PBRFile;
                        //    PBRText = PBRText.AddBefore(ref success, "        color.rgb += ambient;", "\r\n        if (cb_mObjectType == 19) " +
                        //    $"\r\n			color.rgb += ambient*(2.2-(lerp( {tweak.parameters[0].value}, {tweak.parameters[1].value}, cb_mDayNightInterpolant)));" +
                        //    "\r\n		else");
                        //    break;

                        //case "Enhanced Atmospherics Sun":
                        //    currentFile = FileIO.PBRFile;


                        //    PBRText = PBRText.AddAfter(ref success, "pbrValues.clearCoatNDotV = saturate(dot(clearCoatNormal, pbrValues.viewDir)) + 1e-5f;", "\r\nstatic const float3 normalized_ozone_coefficient = float3(0.61344749, 0.0, 1.0);\r\n");

                        //    PBRText = PBRText.AddAfter(ref success, "        pbrValues.clearCoatNDotV = saturate(dot(clearCoatNormal, pbrValues.viewDir)) + 1e-5f;", "\r\n\r\n        DirectionalLight modified_sun = cb_mSun;" +
                        //    $"\r\n        modified_sun.mDiffuse.rgb = lerp(modified_sun.mDiffuse.rgb, modified_sun.mDiffuse.rgb * normalized_ozone_coefficient, interpolate_two_values({tweak.parameters[0].value}, {tweak.parameters[1].value}, modified_sun.mDirection.y));" +
                        //    $"\r\n        modified_sun.mDiffuse.rgb = lerp(get_luminance(modified_sun.mDiffuse.rgb), modified_sun.mDiffuse.rgb, interpolate_two_values({tweak.parameters[4].value}, {tweak.parameters[5].value}, modified_sun.mDirection.y));" +
                        //    $"\r\n        modified_sun.mIntensity *= interpolate_two_values({tweak.parameters[2].value}, {tweak.parameters[3].value}, modified_sun.mDirection.y);");

                        //    PBRText = PBRText.ReplaceAll(ref success, "        float3 sunContrib = DirectionalLightingPBR(cb_mSun, pbrValues);", $"        float3 sunContrib = DirectionalLightingPBR(modified_sun, pbrValues);");
                        //    break;

                        case "Advanced PBR":
                            currentFile = FileIO.PBRFile;
                            //Dont write helperfunctions that are already present in the funclib with specific shader tweaks active
                            if (funclibText.IndexOf("float map") > 0)
                            {
                                

                                PBRText = PBRText.AddBefore(ref success, "struct PS_OUTPUT", "	float4 SampleEnv(float3 txCoord, uint uTextureIndex)\r\n" +
                                "\r\n	{" +
                                "\r\n		return txBindlessCube(uTextureIndex).SampleLevel(samClamp, txCoord, 9);" +
                                "\r\n	}" +
                                "\r\n	float3 CalculateEnv( const float3 vNormal, uint uTextureIndex)" +
                                "\r\n	{" +
                                "\r\n		return SampleEnv(float3(vNormal.x, vNormal.y, vNormal.z), uTextureIndex).xyz;" +
                                "\r\n    }");

                            }
                            else
                            {
                                PBRText = PBRText.AddBefore(ref success, "struct PS_OUTPUT", "	float4 SampleEnv(float3 txCoord, uint uTextureIndex)\r\n" +
                                "\r\n	{" +
                                "\r\n		return txBindlessCube(uTextureIndex).SampleLevel(samClamp, txCoord, 9);" +
                                "\r\n	}" +
                                "\r\n	float3 CalculateEnv( const float3 vNormal, uint uTextureIndex)" +
                                "\r\n	{" +
                                "\r\n		return SampleEnv(float3(vNormal.x, vNormal.y, vNormal.z), uTextureIndex).xyz;" +
                                "\r\n	}" +
                                "\r\n    float map(float input_value, float input_start, float input_end, float output_start, float output_end)" +
                                "\r\n    {" +
                                "\r\n    float slope = (output_end - output_start) / (input_end - input_start);" +
                                "\r\n    return clamp(output_start + (slope * (input_value - input_start)), min(output_start, output_end), max(output_start, output_end));" +
                                "\r\n    }" +
                                "\r\n    float square(float value)" +
                                "\r\n    {" +
                                "\r\n        return value * value;" +
                                "\r\n    }" +
                                "\r\n    float interpolate_two_values(float value_1, float value_2, float sun_angle)" +
                                "\r\n    {" +
                                "\r\n    float output_value;" +
                                "\r\n    if (sun_angle > 0.0) output_value = map(square(sun_angle), square(0.375), 0.0, value_1, value_2);" +
                                "\r\n    else output_value = value_2;" +
                                "\r\n    return output_value;" +
                                "\r\n    }");
                            }

                            //Add Reflect Tweak
                            PBRText = PBRText.AddAfter(ref success, "    float reflectance = 0.5;", "\r\n    if(cb_mObjectType == 19){\r\n" +
                            $"         reflectance = {tweak.parameters[7].value};"+
                            "\r\n    }");

                            //Add Diffuse Twek
                            PBRText = PBRText.ReplaceAll(ref success, "        pbrValues.albedo = LambertDiffuse(diffuseColor);", $"        pbrValues.albedo = LambertDiffuse(diffuseColor) * {tweak.parameters[0].value};");

                            //Add Main Functions
                            PBRText = PBRText.ReplaceAll(ref success, "        color.rgb = (sunContrib + moonContrib) * shadowContrib;", "        #if defined(SHD_VERTICAL_NORMAL)" +
                            "\r\n            const float fDotSun = max(cb_mSun.mDirection.y, 0);" +
                            "\r\n            const float fDotMoon = max(cb_mMoon.mDirection.y, 0);" +
                            "\r\n        #else" +
                            "\r\n            const float fDotSun = saturate(dot(Input.vNormalWS, normalize(cb_mSun.mDirection)));" +
                            "\r\n            const float fDotMoon = saturate(dot(Input.vNormalWS, normalize(cb_mMoon.mDirection)));" +
                            "\r\n        #endif" +
                            "\r\n        float3 colorAmbientSun = cb_mCombinedAmbient.xyz;float3 colorAmbientMoon = cb_mCombinedAmbient.xyz;float3 colorDiffuseSun = cb_mSun.mDiffuse.xyz;float3 colorDiffuseMoon = cb_mMoon.mDiffuse.xyz;" +
                            "\r\n        float3 AmbientLightingCalculation;{" +
                            "\r\n            float3 DiffuseIrradianceApprox = CalculateEnv(Input.vNormalWS,pbrMaterial.uTextureIDs1[1]);" +
                            $"\r\n            DiffuseIrradianceApprox.rgb = saturate(lerp(dot(DiffuseIrradianceApprox.rgb, float3(0.299f, 0.587f, 0.114f)), DiffuseIrradianceApprox.rgb, {tweak.parameters[1].value}));" +
                            "\r\n            float3 kS = SpecularReflection(pbrValues.specColor, pbrValues.specColorF90,pbrValues.nDotV);" +
                            "\r\n            float3 kD = 1.0 - kS;" +
                            "\r\n            float3 diffuseIBL = diffuseColor * DiffuseIrradianceApprox;" +
                            "\r\n            float3 specularIBL = GetIBLSpecular(specColor, specularColorF90, perceptualRoughness, pbrValues.viewDir, vNormal, pbrValues.nDotV, pbrMaterial.uTextureIDs1[1], pbrMaterial.uTextureIDs1[3]);" +
                            $"\r\n            AmbientLightingCalculation = (kD * diffuseIBL + specularIBL * {tweak.parameters[2].value}) * occlusion;" +
                            "\r\n        }" +
                            "\r\n            float3 colorDiffuseTexture = CalculateEnv(Input.vNormalWS,pbrMaterial.uTextureIDs1[1]);" +
                            "\r\n            colorDiffuseSun = colorDiffuseSun * (1.15 * colorDiffuseTexture + float3(1 - 0.15, 1 - 0.15, 1 - 0.15));" +
                            "\r\n        color.rgb = (sunContrib + moonContrib) * shadowContrib * lerp(colorDiffuseSun.xyz, colorDiffuseSun.xyz * 1.6 , interpolate_two_values(1.6, 6.5, cb_mSun.mDirection.y)) * fDotSun*0.8;");

                            //Case 1 IBL for PBR VC's
                            if (tweak.parameters[8].value == "1")
                                PBRText = PBRText.ReplaceAll(ref success, "        color.rgb += ambient;", "\r\n        if(cb_mObjectType == 19)" +
                                $"\r\n            color.rgb += AmbientLightingCalculation * {tweak.parameters[3].value};" +
                                "\r\n        else" +
                                $"\r\n        color.rgb += ambient * {tweak.parameters[4].value} * saturate({tweak.parameters[5].value} + cb_mSun.mDiffuse.g/0.33);" +
                                "\r\n        if(cb_mObjectType == 19){" +
                                "\r\n            #if defined(SHD_NO_NEAR_CLIP)" +
                                $"\r\n                color.rgb += AmbientLightingCalculation * {tweak.parameters[6].value};" +
                                "\r\n            #endif" +
                                "\r\n        }");
                            else
                            //Case 2 no IBL for PBR VC's
                            PBRText = PBRText.ReplaceAll(ref success, "        color.rgb += ambient;", "\r\n        if(cb_mObjectType == 19)" +
                            $"\r\n            color.rgb += AmbientLightingCalculation * {tweak.parameters[3].value};" +
                            "\r\n        else" +
                            $"\r\n        color.rgb += ambient * {tweak.parameters[4].value} * saturate({tweak.parameters[5].value} + cb_mSun.mDiffuse.g/0.33);" +
                            "\r\n        if(cb_mObjectType == 19){" +
                            "\r\n            #if defined(SHD_NO_NEAR_CLIP)" +
                            $"\r\n                color.rgb += ambient * {tweak.parameters[6].value};" +
                            "\r\n            #endif" +
                            "\r\n        }");

                            break;
                        #endregion


                        #region HDR
                        case "Alternate tonemap adjustment":
                            currentFile = FileIO.HDRFile;
                            HDRText = HDRText.AddAfter(ref success, "// Copyright (c) 2021, Lockheed Martin Corporation", "\r\n// Tonemapper adapted from tomatoshade");

                            HDRText = HDRText.ReplaceAll(ref success, "//ACES based ToneMapping curve, takes and outputs linear values.", "\r\nfloat3 tonemap_uncharted2(in float3 x)" +
                            "\r\n{" +
                            "\r\n    float A = 0.15;" +
                            "\r\n    float B = 0.50;" +
                            "\r\n    float C = 0.10;" +
                            "\r\n    float D = 0.20;" +
                            "\r\n    float E = 0.02;" +
                            "\r\n    float F = 0.30;" +
                            "\r\n    return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;" +
                            "\r\n}");

                            HDRText = HDRText.ReplaceAll(ref success, "    const float A = 2.51;", "    color *= 1;");
                            HDRText = HDRText.ReplaceAll(ref success, "    const float B = 0.03;", "    float exposure_bias = 2.0f;");
                            HDRText = HDRText.ReplaceAll(ref success, "    const float C = 2.43;", "    float3 curr = tonemap_uncharted2(exposure_bias*color);");
                            HDRText = HDRText.ReplaceAll(ref success, "    const float D = 0.59;", "    float W = 11.2;");
                            HDRText = HDRText.ReplaceAll(ref success, "    const float E = 0.14;", "    float3 white_scale = 1.0f/tonemap_uncharted2(W);");
                            HDRText = HDRText.ReplaceAll(ref success, "	return saturate((color * (A * color + B)) / (color * (C * color + D) + E));", "    float3 ccolor = curr*white_scale;" +
                            "\r\n    color = pow(abs(ccolor), 1.0 * 0.454545);" +
                            "\r\n    return saturate(pow(color, 2.5f) * 1.2f);");

                            HDRText = HDRText.ReplaceAll(ref success, "    color.rgb *= exposure;", $"    color.rgb *= exposure*{tweak.parameters[0].value};");
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
                File.WriteAllText(shaderDirectory + FileIO.PrecipParticleFile, PrecipParticleText);
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

        private void BackupShaders_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("OpenShade will backup your Prepar3D shaders now.\r\nMake sure the files are the original ones or click 'Cancel' and manually select your backup folder in the application settings.", "Backup", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation, MessageBoxResult.OK); // TODO: Localization
            if (result == MessageBoxResult.OK)
            {
                Log(ErrorType.None, "Shaders backed up");
            }
            else
            {
                Log(ErrorType.Warning, "Shaders could not be backed up. OpenShade can not run.");
                ChangeMenuBarState(false);
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
