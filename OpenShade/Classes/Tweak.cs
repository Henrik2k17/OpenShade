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
        PBR,
        HDR
    };

    public enum UIType {
        Text,
        Checkbox,
        RGB,
        Combobox
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
            newTweak.parameters.Add(new Parameter("Enable", "Sky Ozone Effect Day", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Sky Ozone Effect Twilight", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Sky Ozone Effect Night", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Sky Brightness Day", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Sky Brightness Twilight", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Sky Brightness Night", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Sky Saturation Day", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Sky Saturation twilight", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Sky Saturation Night", 1, 0, 0.1, 5, UIType.Text));
            tweaks.Add(newTweak);

            newTweak = new Tweak("ENHANCED_ATMOSPHERICS_Clouds", Category.EnhancedAtmospherics, "Enhanced Atmospherics Clouds", "");
            newTweak.parameters.Add(new Parameter("Enable", "Cloud Ozone Effect Day", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Cloud Ozone Effect Twilight", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Cloud Ozone Effect Night", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Cloud Brightness Day", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Cloud Brightness Twilight", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Cloud Brightness Night", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Cloud Saturation Day", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Cloud Saturation Twilight", 1, 0, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("Enable", "Cloud Saturation Night", 1, 0, 0.1, 51, UIType.Text));
            tweaks.Add(newTweak);

            // -----------------------
            //  PBR
            // -----------------------

            newTweak = new Tweak("PBR_BRIGHTNESS", Category.PBR, "PBR brightness", "");
            newTweak.parameters.Add(new Parameter("BrightnessNight", "PBR brightness day", 1, 0.1, 0.1, 5, UIType.Text));
            newTweak.parameters.Add(new Parameter("BrightnessDay", "PBR brightness night", 1, 1, 0.1, 5, UIType.Text));
            tweaks.Add(newTweak);

            newTweak = new Tweak("PBR_SATURATION_EXTERNAL", Category.PBR, "Cockpit PBR saturation", "");
            newTweak.parameters.Add(new Parameter("SaturateRatio", "Saturation", 1, 1, 0, 2, UIType.Text));
            tweaks.Add(newTweak);

            newTweak = new Tweak("PBR_SATURATION_VC", Category.PBR, "Aircraft PBR saturation", "");
            newTweak.parameters.Add(new Parameter("SaturateRatio", "Saturation", 1, 1, 0, 2, UIType.Text));
            tweaks.Add(newTweak);

            newTweak = new Tweak("PBR_IBL_AIRCRAFT", Category.PBR, "Aircraft PBR IBL tuning", "");
            newTweak.parameters.Add(new Parameter("IblSpecularSaturateRatio", "IBL Specular Saturation", 1, 1, 0, 2, UIType.Text));
            newTweak.parameters.Add(new Parameter("IblDiffuseSaturateRatio", "IBL Diffuse Saturation", 1, 1, 0, 2, UIType.Text));
            tweaks.Add(newTweak);

            // -----------------------
            //  HDR Section
            // -----------------------

            newTweak = new Tweak("HDR & POST-PROCESSING_HDRTONEMAP", Category.HDR, "Alternate tonemap adjustment", "This tweak replaces the default P3D tonemapper with a new tonemapper called 'Reinhard', recommended when using EA.");
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
