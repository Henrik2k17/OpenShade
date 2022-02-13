using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace OpenShade.Classes
{
    public enum Category
    {
        EnhancedAtmospherics,
        Lighting,
        Terrain,
        PBR,
        HDR
    };

    public enum UIType {
        Text,
        Checkbox,
        RGB,
        Combobox,
        TextBox
    }


    public class RGB {
        public double R;
        public double G;
        public double B;

        public RGB(double red, double green, double blue) {
            R = red;
            G = green;
            B = blue;
        }

        public string GetString() {
            string result = R.ToString("F2") + "," + G.ToString("F2") + "," + B.ToString("F2");
            return result;
        }
    }

    public class Parameter : INotifyPropertyChanged
    {
        public string id;
        public string dataName; // name of the parameter in the .ini file
        public string name; // name of the parameter in the UI
        public string description;

        private string _oldValue;
        public string oldValue   {
            get { return _oldValue; }
            set { _oldValue = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("oldValue")); }
        }    
        private string _value;
        public string value
        {
            get { return _value; }
            set { _value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("value")); }
        }
        public string defaultValue;

        public decimal min;
        public decimal max;
        public List<string> range;
        public UIType control;
        
        public bool hasChanged {
            get { return oldValue != value; } // TODO: In some cases this evaluates to false because of stuff like "1.0" != "1.00" ... not sure what's the best thing to do
        }

        public Parameter() { }

        public Parameter(string DataName, string Name, string Val, string Default, double Min, double Max, UIType Control, string Descr = null) {
            id = Guid.NewGuid().ToString();
            dataName = DataName;
            name = Name;
            description = Descr;
            value = Val;
            defaultValue = Default;
            if (Min == 0 && Max == 0)
            {
                min = decimal.MinValue;
                max = decimal.MaxValue;
            }
            else
            {
                min = Convert.ToDecimal(Min);
                max = Convert.ToDecimal(Max);
            }
            control = Control;
        }

        public Parameter(string DataName, string Name, double Val, double Default, double Min, double Max, UIType Control, string Descr = null)
        {
            id = Guid.NewGuid().ToString();
            dataName = DataName;
            name = Name;
            description = Descr;
            value = Convert.ToDecimal(Val).ToString();
            defaultValue = Convert.ToDecimal(Default).ToString();
            if (Min == 0 && Max == 0)
            {
                min = decimal.MinValue;
                max = decimal.MaxValue;
            }
            else
            {
                min = Convert.ToDecimal(Min);
                max = Convert.ToDecimal(Max);
            }
            control = Control;
        }

        public Parameter(string DataName, string Name, RGB Val, RGB Default, double Min, double Max, UIType Control, string Descr = null)
        {
            id = Guid.NewGuid().ToString();
            dataName = DataName;
            name = Name;
            description = Descr;
            value = Val.GetString();
            defaultValue = Default.GetString();
            if (Min == 0 && Max == 0)
            {
                min = decimal.MinValue;
                max = decimal.MaxValue;
            }
            else
            {
                min = Convert.ToDecimal(Min);
                max = Convert.ToDecimal(Max);
            }
            control = Control;
        }

        public Parameter(string DataName, string Name, string Val, string Default, List<string> ValueRange, UIType Control, string Descr = null)
        {
            id = Guid.NewGuid().ToString();
            dataName = DataName;
            name = Name;
            description = Descr;
            value = Val;
            defaultValue = Default;
            range = ValueRange;
            control = Control;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class BaseTweak : INotifyPropertyChanged {
        public string key;
        public string name { get; set; }
        public string description { get; set; }

        private bool _wasEnabled;
        public bool wasEnabled
        {
            get { return _wasEnabled; }
            set
            {
                _wasEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("wasEnabled"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("stateChanged"));
            }
        }
        private bool _isEnabled;
        public bool isEnabled {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("isEnabled"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("stateChanged"));
            }
        }        
        
        public bool stateChanged { // to know if the tweak was switched enabled/disabled
            get { return wasEnabled != isEnabled; }            
        }

        public bool containsChanges { get { return parameters.Any(p => p.hasChanged == true); } }

        public BindingList<Parameter> parameters { get; set; }

        public BaseTweak(string Key, string Name, string Descr)
        {
            key = Key;           
            name = Name;
            description = Descr;
            isEnabled = false;
            parameters = new BindingList<Parameter>() { };
            parameters.ListChanged += new ListChangedEventHandler(ContentListChanged);
        }

        public void ContentListChanged(object sender, ListChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("containsChanges"));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class Tweak : BaseTweak
    {   
        public Category category { get; set; }

        //public ChangeType tweakType;
        //public string referenceCode;
        //public string newCode;

        public Tweak(string Key, Category Cat, string Name, string Descr) : base(Key, Name, Descr)
        {
            this.category = Cat;
        }

        public static void GenerateTweaksData(List<Tweak> tweaks)
        {
            var

            // -----------------------
            //  EnhancedAtmospherics
            // -----------------------

            newTweak = new Tweak("ENHANCED_ATMOSPHERICS_ATMOSPHERE", Category.EnhancedAtmospherics, "Enhanced Atmospherics Atmosphere", "");
            newTweak.parameters.Add(new Parameter("SkyOzoneEffectDay", "Sky Ozone Effect Day", 0.125, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("SkyOzoneEffectTwilight", "Sky Ozone Effect Twilight", 0.325, 0, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("SkyOzoneEffectNight", "Sky Ozone Effect Night", 0.125, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("SkyBrightnessDay", "Sky Brightness Day", 1, 1, 0.1, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("SkyBrightnessTwilight", "Sky Brightness Twilight", 3.25, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("SkyBrightnessNight", "Sky Brightness Night", 0.5, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("SkySaturationDay", "Sky Saturation Day", 1, 1, 0.1, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("SkySaturationtwilight", "Sky Saturation twilight", 0.875, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("SkySaturationNight", "Sky Saturation Night", 1, 1, 0.01, 5, UIType.TextBox));
            tweaks.Add(newTweak);

            newTweak = new Tweak("ENHANCED_ATMOSPHERICS_Clouds", Category.EnhancedAtmospherics, "Enhanced Atmospherics Clouds", "");
            newTweak.parameters.Add(new Parameter("CloudOzoneEffectDay", "Cloud Ozone Effect Day", 0, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("CloudOzoneEffectTwilight", "Cloud Ozone Effect Twilight", 0.325, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("CloudOzoneEffectNight", "Cloud Ozone Effect Night", 0.125, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("CloudBrightnessDay", "Cloud Brightness Day", 1, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("CloudBrightnessTwilight", "Cloud Brightness Twilight", 1, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("CloudBrightnessNight", "Cloud Brightness Night", 0.5, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("CloudSaturationDay", "Cloud Saturation Day", 1, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("CloudSaturationTwilight", "Cloud Saturation Twilight", 0.325, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("CloudSaturationNight", "Cloud Saturation Night", 1, 1, 0.01, 51, UIType.TextBox));
            tweaks.Add(newTweak);

            //// -----------------------
            ////  Lighting
            //// -----------------------

            //newTweak = new Tweak("LIGHTING_COCKPIT_LIGHTING", Category.Lighting, "Cockpit Lighting", "");
            ////newTweak.parameters.Add(new Parameter("SkyOzoneEffectDay", "Sky Ozone Effect Day", 0.125, 1, 0.01, 5, UIType.TextBox));
            //tweaks.Add(newTweak);

            // -----------------------
            //  Terrain
            // -----------------------

            newTweak = new Tweak("TERRAIN_REFLECTANCE", Category.Terrain, "Terrain Reflectance", "Adjust how much the terrain reflects, 0.25 is the default p3d pbr terrain reflectance");
            newTweak.parameters.Add(new Parameter("TerrainReflectance", "Terrain Reflectance", 0.02, 1, 0.001, 1, UIType.TextBox));
            tweaks.Add(newTweak);
                                                                                                                                                                                                                                                                                                                                                                                                                                
            newTweak = new Tweak("TERRAIN_LIGHTING", Category.Terrain, "Terrain Lighting", "Terrain Lighting Diffuse - Diffuse Lighting Factor of the terrain\r\nTerrain Lighting Ambient - Ambient Lighting Factor of the terrain\r\nTerrain Lighting Moon - This value defines how much the moonlight affects the terrain at night\r\n\r\nPro Tip: Use Expressions to darken the ambient/diffuse only at a given time\r\n             Something like this  saturate(0.5 + cb_mSun.mDiffuse.g/0.33)");
            newTweak.parameters.Add(new Parameter("TerrainLightingDiffuse", "Terrain Lighting Diffuse", 1, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("TerrainLightingAmbient", "Terrain Lighting Ambient", 1, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("TerrainLightingMoon", "Terrain Lighting Moon", 1, 1, 0.01, 5, UIType.TextBox));
            tweaks.Add(newTweak);

            newTweak = new Tweak("TERRAIN_Saturation", Category.Terrain, "Terrain Saturation", "Saturates the terrain, 1.0 is the default p3d saturation\r\nIncrease or decrease as you like");
            newTweak.parameters.Add(new Parameter("TerrainSaturation", "Terrain Saturation", 1, 1, 0.01, 5, UIType.TextBox));
            tweaks.Add(newTweak);

            // -----------------------
            //  PBR
            // -----------------------

            newTweak = new Tweak("AIRCRAFT_PBR_BRIGHTNESS", Category.PBR, "Aircraft PBR brightness", "Tweak the aircraft PBR brightness independed from day and night.\r\n Higher value means darker.\r\n A value of 2 means the aircraft brightness double as dark as default.");
            newTweak.parameters.Add(new Parameter("AircraftBrightnessDay", "Aircraft PBR brightness day", 1.1, 1, 0.1, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("AircraftBrightnessNight", "Aircraft PBR brightness night", 2.2, 1, 0.1, 5, UIType.TextBox));
            tweaks.Add(newTweak);
            // -----------------------
            //  HDR Section
            // -----------------------

            newTweak = new Tweak("HDR_TONEMAP", Category.HDR, "Alternate tonemap adjustment", "This tweak replaces the default P3D tonemapper with a new tonemapper.");
            newTweak.parameters.Add(new Parameter("toneMapPower", "Tonemap Power", 1.3, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("toneMapExposure", "Tonemap Exposure", 1.2, 1, 0.01, 5, UIType.TextBox));
            tweaks.Add(newTweak);
        }

        // NOTE: DO NOT change GetHashCode function because the lists selection logic is based on that.
        // So if you change it, it screws everything up in the list, unless you removed-add the item. Just don't do it
    
    }

    public class CustomTweak : INotifyPropertyChanged
    {
        public string key;
        private string _name;
        public string name // This is fucking awful boilerplate code, I hate it.. getters and setters everywhere, ugh. Just imagine having this on every property...
        {
            get { return _name; }
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("name"));
            }
        }
        public string shaderFile { get; set; }        
        public int index { get; set; }
        public string oldCode { get; set; }
        public string newCode { get; set; }
        public bool isEnabled { get; set; }

        public CustomTweak(string Key, string Name, string Shader, int idx, string OldCode, string NewCode, bool IsOn)
        {
            key = Key;
            name = Name;
            shaderFile = Shader;
            index = idx;
            oldCode = OldCode;
            newCode = NewCode;
            isEnabled = IsOn;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class PostProcess : BaseTweak
    {
        public int index;

        public PostProcess(string Key, string Name, int Idx, string Descr) : base(Key, Name, Descr)
        {
            index = Idx;
        }
        
    }


}
