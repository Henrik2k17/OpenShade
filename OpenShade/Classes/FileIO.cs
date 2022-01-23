﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace OpenShade.Classes
{
    class FileIO
    {
        MainWindow mainWindowHandle;

        public const string settingsFile = "config.txt";

        public const string cloudFile = "Cloud.fx";
        public const string generalFile = "General.fx";
        public const string terrainFile = "Terrain.fx";
        public const string funclibFile = "FuncLibrary.fxh";
        public const string terrainFXHFile = "Terrain.fxh";
        public const string shadowFile = "Shadow.fxh";
        public const string HDRFile = "HDR.hlsl";
        public const string PBRFile = "PBRBase.fx";
        public const string compositeFile = "DeferredComposite.fx";

        public FileIO(MainWindow handle)
        {
            mainWindowHandle = handle;
        }

        public bool LoadShaderFiles(string dir)
        {
            try
            {
                MainWindow.cloudText = File.ReadAllText(dir + cloudFile);
                MainWindow.generalText = File.ReadAllText(dir + generalFile);
                MainWindow.funclibText = File.ReadAllText(dir + funclibFile);
                MainWindow.terrainFXHText = File.ReadAllText(dir + terrainFXHFile);
                MainWindow.terrainText = File.ReadAllText(dir + terrainFile);
                MainWindow.shadowText = File.ReadAllText(dir + shadowFile);
                MainWindow.HDRText = File.ReadAllText(dir + HDRFile);
                MainWindow.PBRText = File.ReadAllText(dir + PBRFile);
                MainWindow.CompositeText = File.ReadAllText(dir + compositeFile);

                return true;

            }
            catch (Exception ex)
            {
                mainWindowHandle.Log(ErrorType.Error, ex.Message);
                return false;
            }
        }

        public bool CheckShaderBackup(string dir) {
            if (File.Exists(dir + cloudFile) == false) { return false; }
            if (File.Exists(dir + generalFile) == false) { return false; }
            if (File.Exists(dir + funclibFile) == false) { return false; }
            if (File.Exists(dir + terrainFXHFile) == false) { return false; }
            if (File.Exists(dir + terrainFile) == false) { return false; }
            if (File.Exists(dir + shadowFile) == false) { return false; }
            if (File.Exists(dir + HDRFile) == false) { return false; }
            if (File.Exists(dir + PBRFile) == false) { return false; }
            if (File.Exists(dir + compositeFile) == false) { return false; }

            return true;
        }

        public bool CopyShaderFiles(string origin, string destination)
        {
            try
            {
                File.Copy(origin + cloudFile, destination + cloudFile, true);
                File.Copy(origin + generalFile, destination + generalFile, true);
                File.Copy(origin + funclibFile, destination + funclibFile, true);
                File.Copy(origin + terrainFXHFile, destination + terrainFXHFile, true);
                File.Copy(origin + terrainFile, destination + terrainFile, true);
                File.Copy(origin + shadowFile, destination + shadowFile, true);
                File.Copy(origin + PBRFile, destination + PBRFile, true);
                File.Copy(origin + compositeFile, destination + compositeFile, true);

                if (origin.Contains("ShadersHLSL"))
                {
                    origin += "PostProcess\\";
                }
                else
                {
                    destination += "PostProcess\\";
                }
                File.Copy(origin + HDRFile, destination + HDRFile, true);

                return true;
            }
            catch (Exception ex)
            {
                mainWindowHandle.Log(ErrorType.Error, ex.Message);
                return false;
            }
        }

        public bool ClearDirectory(string dir)
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dir);

                foreach (FileInfo file in dirInfo.GetFiles())
                {
                    file.Delete();
                }
                return true;
            }
            catch (Exception ex) {
                mainWindowHandle.Log(ErrorType.Error, ex.Message);
                return false;
            }
        }


        public void LoadTweaks(List<Tweak> tweaks, IniFile pref, bool monitorChanges)
        {
            foreach (var tweak in tweaks)
            {
                bool wasEnabled = tweak.isEnabled;

                // BEGIN CUSTOM -----
                if (tweak.key == "HDR & POST-PROCESSING_POSTPROCESS") // Special case for HDR on/off switch, since the implementation is a bit different from PTA preset files
                {
                    if (!pref.KeyExists("NoHDR", tweak.key))
                    {
                        mainWindowHandle.Log(ErrorType.Warning, "Missing entry 'NoHDR' for tweak [" + tweak.key + "]");
                        break;
                    }
                    else
                    {
                        tweak.isEnabled = pref.Read("NoHDR", tweak.key) == "1" ? true : false;
                    }
                }
                // END CUSTOM ----

                else if (!pref.KeyExists("IsActive", tweak.key))
                {
                    mainWindowHandle.Log(ErrorType.Warning, "Missing entry 'IsActive' for tweak [" + tweak.key + "]");
                    break;
                }
                else
                {
                    tweak.isEnabled = pref.Read("IsActive", tweak.key) == "1" ? true : false;
                }     

                if (!monitorChanges)
                {
                    tweak.wasEnabled = tweak.isEnabled;
                }
                                           
                foreach (var param in tweak.parameters)
                {
                    param.oldValue = param.value;

                    if (param.control == UIType.RGB)
                    {
                        string dataR = param.dataName.Split(',')[0];
                        string dataG = param.dataName.Split(',')[1];
                        string dataB = param.dataName.Split(',')[2];

                        if (!pref.KeyExists(dataR, tweak.key)) { mainWindowHandle.Log(ErrorType.Warning, "Missing entry '" + dataR + "' for tweak [" + tweak.key + "]"); break; }
                        if (!pref.KeyExists(dataG, tweak.key)) { mainWindowHandle.Log(ErrorType.Warning, "Missing entry '" + dataG + "' for tweak [" + tweak.key + "]"); break; }
                        if (!pref.KeyExists(dataB, tweak.key)) { mainWindowHandle.Log(ErrorType.Warning, "Missing entry '" + dataB + "' for tweak [" + tweak.key + "]"); break; }

                        param.value = pref.Read(dataR, tweak.key) + "," + pref.Read(dataG, tweak.key) + "," + pref.Read(dataB, tweak.key);                                             
                    }
                    else
                    {
                        if (!pref.KeyExists(param.dataName, tweak.key))
                        {
                            mainWindowHandle.Log(ErrorType.Warning, "Missing entry '" + param.dataName + "' for tweak [" + tweak.key + "]");
                            break;
                        }
                        param.value = pref.Read(param.dataName, tweak.key);
                    }

                    if (!monitorChanges) {
                        param.oldValue = param.value;
                    }
                }                
            }           
        }

        public string LoadComments(IniFile pref) {
            if (pref.KeyExists("Comment", "PRESET COMMENTS")) {
                string rawComment = pref.Read("Comment", "PRESET COMMENTS");
                string result = rawComment.Replace("~^#", "\r\n");
                return result;
            }
            return "";            
        }

        public void SavePreset(List<Tweak> tweaks, string comment, IniFile preset)
        {

            // Standard tweaks    
            foreach (var tweak in tweaks) {
                preset.Write("IsActive", tweak.isEnabled ? "1" : "0", tweak.key);

                if (tweak.key == "HDR & POST-PROCESSING_POSTPROCESS") // Special case for HDR on/off switch, since the implementation is a bit different from PTA preset files
                {
                    preset.Write("NoHDR", tweak.isEnabled ? "1" : "0", tweak.key);
                }

                foreach (var param in tweak.parameters)
                {
                    if (param.control == UIType.RGB)
                    {
                        string dataR = param.dataName.Split(',')[0];
                        string dataG = param.dataName.Split(',')[1];
                        string dataB = param.dataName.Split(',')[2];

                        string valueR = param.value.Split(',')[0];
                        string valueG = param.value.Split(',')[1];
                        string valueB = param.value.Split(',')[2];

                        preset.Write(dataR, valueR, tweak.key);
                        preset.Write(dataG, valueG, tweak.key);
                        preset.Write(dataB, valueB, tweak.key);
                    }
                    else
                    {
                        preset.Write(param.dataName, param.value, tweak.key);
                    }
                }                
            }       
            // Comment
            preset.Write("Comment", comment.Replace("\r\n", "~^#"), "PRESET COMMENTS");
        }

        public void LoadSettings(string filepath)
        {
            // TODO: Ignore comments and blank lines
            // TODO: Error checking

            var lines = File.ReadAllLines(filepath);

            for (int i = 0; i < lines.Count(); i++)
            {
                List<string> parts = lines[i].Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (parts.Count() == 1)
                {
                    mainWindowHandle.Log(ErrorType.Warning, $"Missing data in config.txt. Check line {(i + 1).ToString()} contains {{key}}, {{value}}");
                }
                else if (parts.Count() == 2)
                {
                    switch (parts[0])
                    {
                        case "Active_Preset":
                            mainWindowHandle.activePresetPath = parts[1].Trim(); 
                            break;

                        case "Loaded_Preset":
                            mainWindowHandle.loadedPresetPath = parts[1].Trim();
                            break;

                        case "P3D_Version":
                            mainWindowHandle.P3DVersion = parts[1].Trim();
                            break;

                        case "Theme":
                            Themes current;
                            if (Enum.TryParse(parts[1], out current))
                            {
                                ((App)Application.Current).ChangeTheme(current);
                            }
                            break;

                        case "Backup_Directory":
                            mainWindowHandle.backupDirectory = parts[1].Trim();
                            break;

                        case "Main_Width":
                            mainWindowHandle.Width = double.Parse(parts[1].Trim());
                            break;

                        case "Main_Height":
                            mainWindowHandle.Height = double.Parse(parts[1].Trim());
                            break;

                        case "Col1_Width":
                            mainWindowHandle.Tweaks_Grid.ColumnDefinitions[0].Width = new GridLength(double.Parse(parts[1].Trim()));
                            break;
                    }
                }
                else
                {
                    mainWindowHandle.Log(ErrorType.Warning, $"Too much data in config.txt. Check line {(i + 1).ToString()} contains only {{key}}, {{value}}");
                }
            }
        }

        public void SaveSettings(string filepath)
        {
            List<string> lines = new List<string>();

            if (mainWindowHandle.activePresetPath != null && File.Exists(mainWindowHandle.activePresetPath)) { lines.Add("Active_Preset, " + mainWindowHandle.activePresetPath); }
            if (mainWindowHandle.loadedPresetPath != null && File.Exists(mainWindowHandle.loadedPresetPath)) { lines.Add("Loaded_Preset, " + mainWindowHandle.loadedPresetPath); }

            lines.Add("P3D_Version, " + mainWindowHandle.P3DVersion);
            lines.Add("Theme, " + ((App)Application.Current).CurrentTheme.ToString());
            lines.Add("Backup_Directory, " + mainWindowHandle.backupDirectory);
            lines.Add("Main_Width, " + mainWindowHandle.Width.ToString());
            lines.Add("Main_Height, " + mainWindowHandle.Height.ToString());
            lines.Add("Col1_Width, " + mainWindowHandle.Tweaks_Grid.ColumnDefinitions[0].Width.ToString());       

            try
            {
                File.WriteAllLines(filepath, lines);
                mainWindowHandle.Log(ErrorType.None, "Data saved in settings.txt");
            }
            catch
            {
                mainWindowHandle.Log(ErrorType.Error, "Could not save data in settings.txt");
            }

        }
    }
}
