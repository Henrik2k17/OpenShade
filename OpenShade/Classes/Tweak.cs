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
        Clouds,
        Atmosphere,
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
            newTweak.parameters.Add(new Parameter("SkyOzoneEffectDay", "Sky Ozone Effect Day", 0.125, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[0].value}
            newTweak.parameters.Add(new Parameter("SkyOzoneEffectTwilight", "Sky Ozone Effect Twilight", 0.325, 0, 0.01, 5, UIType.TextBox)); //{tweak.parameters[1].value}
            newTweak.parameters.Add(new Parameter("SkyOzoneEffectNight", "Sky Ozone Effect Night", 0.125, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[2].value}
            newTweak.parameters.Add(new Parameter("SkyBrightnessDay", "Sky Brightness Day", 1, 1, 0.1, 5, UIType.TextBox)); //{tweak.parameters[3].value}
            newTweak.parameters.Add(new Parameter("SkyBrightnessTwilight", "Sky Brightness Twilight", 3.25, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[4].value}
            newTweak.parameters.Add(new Parameter("SkyBrightnessNight", "Sky Brightness Night", 0.5, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[5].value}
            newTweak.parameters.Add(new Parameter("SkySaturationDay", "Sky Saturation Day", 1, 1, 0.1, 5, UIType.TextBox)); //{tweak.parameters[6].value}
            newTweak.parameters.Add(new Parameter("SkySaturationtwilight", "Sky Saturation twilight", 0.875, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[7].value}
            newTweak.parameters.Add(new Parameter("SkySaturationNight", "Sky Saturation Night", 1, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[8].value}
            tweaks.Add(newTweak);

            newTweak = new Tweak("ENHANCED_ATMOSPHERICS_Clouds", Category.EnhancedAtmospherics, "Enhanced Atmospherics Clouds", "");
            newTweak.parameters.Add(new Parameter("CloudOzoneEffectDay", "Cloud Ozone Effect Day", 0, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[0].value}
            newTweak.parameters.Add(new Parameter("CloudOzoneEffectTwilight", "Cloud Ozone Effect Twilight", 0.325, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[1].value}
            newTweak.parameters.Add(new Parameter("CloudOzoneEffectNight", "Cloud Ozone Effect Night", 0.125, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[2].value}
            newTweak.parameters.Add(new Parameter("CloudBrightnessDay", "Cloud Brightness Day", 1, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[3].value}
            newTweak.parameters.Add(new Parameter("CloudBrightnessTwilight", "Cloud Brightness Twilight", 1, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[4].value}
            newTweak.parameters.Add(new Parameter("CloudBrightnessNight", "Cloud Brightness Night", 0.5, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[5].value}
            newTweak.parameters.Add(new Parameter("CloudSaturationDay", "Cloud Saturation Day", 1, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[6].value}
            newTweak.parameters.Add(new Parameter("CloudSaturationTwilight", "Cloud Saturation Twilight", 0.325, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[7].value}
            newTweak.parameters.Add(new Parameter("CloudSaturationNight", "Cloud Saturation Night", 1, 1, 0.01, 51, UIType.TextBox)); //{tweak.parameters[8].value}
            tweaks.Add(newTweak);

            //newTweak = new Tweak("ENHANCED_ATMOSPHERICS_Sun", Category.EnhancedAtmospherics, "Enhanced Atmospherics Sun", "");
            //newTweak.parameters.Add(new Parameter("SunOzoneEffectDay", "Sun Ozone Effect Day", 0, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[0].value}
            //newTweak.parameters.Add(new Parameter("SunOzoneEffectTwilight", "Sun Ozone Effect Twilight", 0.325, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[1].value}
            //newTweak.parameters.Add(new Parameter("SunBrightnessDay", "Sun Brightness Day", 2.75, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[2].value}
            //newTweak.parameters.Add(new Parameter("SunBrightnessTwilight", "Sun Brightness Twilight", 3.75, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[3].value}
            //newTweak.parameters.Add(new Parameter("SunSaturationDay", "Sun Saturation Day", 1, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[4].value}
            //newTweak.parameters.Add(new Parameter("SunSaturationTwilight", "Sun Saturation Twilight", 1, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[5].value}
            //tweaks.Add(newTweak);

            // -----------------------
            //  Clouds
            // -----------------------

            newTweak = new Tweak("CLOUDS_POPCORN_MODIFICATOR", Category.Clouds, "'No popcorn' clouds", "");
            newTweak.parameters.Add(new Parameter("CloudDistanceFactor", "Distance factor", 0.0000000005, 0.0000000005, 0.0000000001, 0.0000000010, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("CloudOpacity", "Opacity at far range", 1, 1, 0.1, 1, UIType.TextBox));
            tweaks.Add(newTweak);

            newTweak = new Tweak("CLOUDS_CLOUD_ALTERNATE_LIGHTING", Category.Clouds, "Alternate lighting for cloud groups", "");
            tweaks.Add(newTweak);

            newTweak = new Tweak("CLOUDS_CIRRUS_LIGHTING", Category.Clouds, "Cirrus lighting", "");
            newTweak.parameters.Add(new Parameter("LightingRatio", "Lighting", 1, 1, 0, 2, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("SaturateRatio", "Saturation", 1, 1, 0, 2, UIType.TextBox));
            tweaks.Add(newTweak);

            newTweak = new Tweak("CLOUDS_CLOUD_VOLUME", Category.Clouds, "Cloud light scattering", "");
            newTweak.parameters.Add(new Parameter("ScatteringFactor", "Scattering factor", 0.5, 0.5, 0.1, 3, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("LightingFactor", "Lighting factor", 0.5, 0.5, 0.01, 2, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("NoPattern", "Don't use cloud lighting patterns", 0, 0, 0, 1, UIType.Checkbox));
            tweaks.Add(newTweak);

            newTweak = new Tweak("CLOUDS_CLOUDS_LIGHTING_TUNING", Category.Clouds, "Cloud lighting tuning", "");
            newTweak.parameters.Add(new Parameter("CloudLightFactor", "Lighting factor", 0.85, 0.85, 0.1, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("CloudSaturateFactor", "Saturation factor", 0.33, 0.33, 0.1, 5, UIType.TextBox));
            tweaks.Add(newTweak);

            newTweak = new Tweak("CLOUDS_CLOUD_SATURATION", Category.Clouds, "Cloud saturation", "");
            newTweak.parameters.Add(new Parameter("ShadeFactor", "Saturation", 1, 1, 0, 3, UIType.TextBox));
            tweaks.Add(newTweak);

            newTweak = new Tweak("CLOUDS_CLOUD_SHADOWS_DEPTH_NEW", Category.Clouds, "Cloud shadow depth", "");
            newTweak.parameters.Add(new Parameter("FDepthFactor", "Shadow depth", 0.15, 0.15, 0.01, 100, UIType.TextBox));
            tweaks.Add(newTweak);

            newTweak = new Tweak("CLOUDS_CLOUD_SHADOWS_SIZE", Category.Clouds, "Cloud shadow extended size", "");
            tweaks.Add(newTweak);

            newTweak = new Tweak("CLOUDS_CLOUD_BRIGHTNESS_TWILIGHT", Category.Clouds, "Reduce cloud brightness at dawn/dusk/night", "");
            tweaks.Add(newTweak);

            newTweak = new Tweak("CLOUDS_CLOUD_SIZE", Category.Clouds, "Cloud puffs width and height scaling", "");
            newTweak.parameters.Add(new Parameter("CloudSizeHCoeff", "Horizontal", 0.5, 0.5, 0.3, 1, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("CloudSizeVCoeff", "Vertical", 0.5, 0.5, 0.3, 1, UIType.TextBox));
            tweaks.Add(newTweak);

            // -----------------------
            //  Atmosphere
            // -----------------------

            newTweak = new Tweak("ATMOSPHERE_HAZE_EFFECT", Category.Atmosphere, "Atmospheres Haze Effect", "");
            newTweak.parameters.Add(new Parameter("HazeEffectPower", "Power", 1.00, 1.00, 1.00, 1.00, UIType.TextBox)); //{tweak.parameters[0].value}
            newTweak.parameters.Add(new Parameter("HazeEffectDensity", "Density", 0.00000000050, 0.00000000050, 0.00000000050, 0.00000000050, UIType.TextBox)); //{tweak.parameters[1].value}
            newTweak.parameters.Add(new Parameter("HazeEffectColorRed", "Color Tone Red", 1.00, 1.00, 1.00, 1.00, UIType.TextBox)); //{tweak.parameters[2].value}
            newTweak.parameters.Add(new Parameter("HazeEffectColorGreen", "Color Tone Green", 1.00, 1.00, 1.00, 1.00, UIType.TextBox)); //{tweak.parameters[3].value}
            newTweak.parameters.Add(new Parameter("HazeEffectColorBlue", "Color Tone Blue", 1.00, 1.00, 1.00, 1.00, UIType.TextBox)); //{tweak.parameters[4].value}
            newTweak.parameters.Add(new Parameter("HazeEffectDensityDependsOnAltitude", "Density depends on altitude", 1, 1, 0, 1, UIType.Checkbox));//{tweak.parameters[5].value}
            newTweak.parameters.Add(new Parameter("HazeEffectAltitudeZero", "Altitude when density reaches zero", 15000, 15000, 15000, 15000, UIType.TextBox));//{tweak.parameters[6].value}
            tweaks.Add(newTweak);

            newTweak = new Tweak("ATMOSPHERE_RAYLEIGH_SCATTERING", Category.Atmosphere, "Atmosphere Rayleigh Scattering", "");
            newTweak.parameters.Add(new Parameter("RayleighScatteringPower", "Power", 2.75, 2.75, 2.75, 2.75, UIType.TextBox)); //{tweak.parameters[0].value}
            newTweak.parameters.Add(new Parameter("RayleighScatteringDensity", "Density", 0.0000000200, 0.0000000200, 0.0000000200, 0.0000000200, UIType.TextBox)); //{tweak.parameters[1].value}
            newTweak.parameters.Add(new Parameter("RayleighScatteringColorGreen", "Color Green", 0.060, 0.060, 0.060, 0.060, UIType.TextBox)); //{tweak.parameters[2].value}
            newTweak.parameters.Add(new Parameter("RayleighScatteringColorBlue", "Color Blue", 0.190, 0.190, 0.190, 0.190, UIType.TextBox)); //{tweak.parameters[3].value}
            newTweak.parameters.Add(new Parameter("RayleighScatteringDependsOnAltitude", "Density depends on altitude", 1, 1, 0, 1, UIType.Checkbox)); //{tweak.parameters[4].value}
            newTweak.parameters.Add(new Parameter("RayleighScatteringAltitudeDensityZero", "Altitude when density reaches zero", 15000, 15000, 15000, 15000, UIType.TextBox)); //{tweak.parameters[5].value}
            tweaks.Add(newTweak);

            newTweak = new Tweak("ATMOSPHERE_CLOUD_FOG", Category.Atmosphere, "Cloud Fog", "");
            newTweak.parameters.Add(new Parameter("Fog Influence", "Power", 1.00, 1.00, 1.00, 1.00, UIType.TextBox)); //{tweak.parameters[0].value}
            tweaks.Add(newTweak);

            newTweak = new Tweak("ATMOSPHERE_FOG_FIX", Category.Atmosphere, "Atmospheres Fog Fix", "");
            tweaks.Add(newTweak);

            newTweak = new Tweak("ATMOSPHERE_SKY_SATURATION", Category.Atmosphere, "Sky Saturation", "");
            newTweak.parameters.Add(new Parameter("SkySaturaion", "Sky Saturation", 1, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[0].value}
            tweaks.Add(newTweak);

            newTweak = new Tweak("ATMOSPHERE_PRECIP_OPACITY", Category.Atmosphere, "Precipitation Opacity", "");
            newTweak.parameters.Add(new Parameter("SnowOpacity", "Snow Opacity", 1, 1, 1, 1, UIType.TextBox)); //{tweak.parameters[0].value}
            newTweak.parameters.Add(new Parameter("RainOpacity", "Rain Opacity", 1, 1, 1, 1, UIType.TextBox)); //{tweak.parameters[1].value}
            tweaks.Add(newTweak);

            //// -----------------------
            ////  Lighting
            //// -----------------------

            newTweak = new Tweak("LIGHTING_OBJECT_LIGHTING", Category.Lighting, "Object Lighting", "");
            newTweak.parameters.Add(new Parameter("ObjectDiffuse", "Object Diffuse", 1, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[0].value}
            newTweak.parameters.Add(new Parameter("ObjectAmbient", "Object Ambient", 1, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[1].value}
            newTweak.parameters.Add(new Parameter("ObjectDiffuseMoon", "Object Diffuse Moon", 1, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[2].value}
            tweaks.Add(newTweak);

            newTweak = new Tweak("LIGHTING_COCKPIT_LIGHTING", Category.Lighting, "Cockpit Lighting", "");
            newTweak.parameters.Add(new Parameter("CockpitDiffuse", "Cockpit Diffuse", 1, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[0].value}
            newTweak.parameters.Add(new Parameter("CockpitAmbient", "Cockpit Ambient", 1, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[1].value}
            newTweak.parameters.Add(new Parameter("CockpitSaturation", "Cockpit Saturation", 1, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[2].value}
            tweaks.Add(newTweak);

            newTweak = new Tweak("LIGHTING_AUTOGEN_LIGHTING", Category.Lighting, "Autogen Lighting", "");
            newTweak.parameters.Add(new Parameter("AutogenLightsBrightness", "Autogen Lights Brightness", 1, 1, 1, 1, UIType.TextBox)); //{tweak.parameters[0].value}
            newTweak.parameters.Add(new Parameter("AutogenLightsSaturation", "Autogen Lights Saturation", 1, 1, 1, 1, UIType.TextBox)); //{tweak.parameters[1].value}
            tweaks.Add(newTweak);

            // -----------------------
            //  Terrain
            // -----------------------

            newTweak = new Tweak("TERRAIN_REFLECTANCE", Category.Terrain, "Terrain Reflectance", "Adjust how much the terrain reflects, 0.25 is the default p3d pbr terrain reflectance");
            newTweak.parameters.Add(new Parameter("TerrainReflectance", "Terrain Reflectance", 0.02, 0.02, 0.001, 1, UIType.TextBox));
            tweaks.Add(newTweak);
                                                                                                                                                                                                                                                                                                                                                                                                                                
            newTweak = new Tweak("TERRAIN_LIGHTING", Category.Terrain, "Terrain Lighting", "Terrain Lighting Diffuse - Diffuse Lighting Factor of the terrain\r\nTerrain Lighting Ambient - Ambient Lighting Factor of the terrain\r\nTerrain Lighting Moon - This value defines how much the moonlight affects the terrain at night\r\n\r\nPro Tip: Use Expressions to darken the ambient/diffuse only at a given time\r\n             Something like this  saturate(0.5 + cb_mSun.mDiffuse.g/0.33)");
            newTweak.parameters.Add(new Parameter("TerrainLightingDiffuse", "Terrain Lighting Diffuse", 1, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("TerrainLightingAmbient", "Terrain Lighting Ambient", 1, 1, 0.01, 5, UIType.TextBox));
            newTweak.parameters.Add(new Parameter("TerrainLightingMoon", "Terrain Lighting Moon", 1, 1, 0.01, 5, UIType.TextBox));
            tweaks.Add(newTweak);

            newTweak = new Tweak("TERRAIN_Saturation", Category.Terrain, "Terrain Saturation", "Saturates the terrain, 1.0 is the default p3d saturation\r\nIncrease or decrease as you like");
            newTweak.parameters.Add(new Parameter("TerrainSaturation", "Terrain Saturation", 1, 1, 0.01, 5, UIType.TextBox));
            tweaks.Add(newTweak);

            newTweak = new Tweak("TERRAIN_EMISSIVE_LIGHTING", Category.Terrain, "Terrain Emissive Lighting", "Changes the terrain emissive lighting");
            newTweak.parameters.Add(new Parameter("TerrainEmissiveBrightness", "Terrain Emissive Brightness", 1, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[0].value}
            newTweak.parameters.Add(new Parameter("TerrainEmissiveSaturation", "Terrain Emissive Saturation", 1, 1, 0.01, 5, UIType.TextBox)); //{tweak.parameters[1].value}
            tweaks.Add(newTweak);


            // -----------------------
            //  PBR
            // -----------------------

            newTweak = new Tweak("ADVANCED_PBR", Category.PBR, "Advanced PBR", "A more advanced PBR approach implemented in P3D using Diffuse Image Based Lighting");
            newTweak.parameters.Add(new Parameter("AdvancedPBRDiffuseColor", "Diffuse Color Brightness", 1.5, 1.5, 1.5, 1.5, UIType.TextBox));//{tweak.parameters[0].value}
            newTweak.parameters.Add(new Parameter("AdvancedPBRIBLSaturation", "IBL Saturation", 0.9, 0.9, 0.9, 0.9, UIType.TextBox));//{tweak.parameters[1].value}
            newTweak.parameters.Add(new Parameter("AdvancedPBRIBLSpecular", "IBL Specular Intensity", 0.7, 0.7, 0.7, 0.7, UIType.TextBox));//{tweak.parameters[2].value}
            newTweak.parameters.Add(new Parameter("AdvancedPBRAircraftAmbientLighting", "Aircraft Ambient Lighting", 1.3, 1.3, 1.3, 1.3, UIType.TextBox));//{tweak.parameters[3].value}
            newTweak.parameters.Add(new Parameter("AdvancedPBROverallAmbientDay", "Ambient Lighting Day", 0.3, 0.3, 0.3, 0.3, UIType.TextBox));//{tweak.parameters[4].value}
            newTweak.parameters.Add(new Parameter("AdvancedPBROverallAmbientNight", "Ambient Lighting Night", 0.1, 0.1, 0.1, 0.1, UIType.TextBox));//{tweak.parameters[5].value}
            newTweak.parameters.Add(new Parameter("AdvancedPBRCockpitAmbientLighting", "Cockpit Ambient Lighting", 2, 2, 2, 2, UIType.TextBox));//{tweak.parameters[6].value}
            newTweak.parameters.Add(new Parameter("AdvancedPBRAircraftReflectance", "Aircraft Reflectance Intensity", 0.2, 0.2, 0.2, 0.2, UIType.TextBox));//{tweak.parameters[7].value}
            newTweak.parameters.Add(new Parameter("VCIBL", "Cockpit IBL", 1, 1, 0, 1, UIType.Checkbox));//{tweak.parameters[8].value}
            newTweak.parameters.Add(new Parameter("AdvancedPBRAircraftAmbientLightingNight", "Aircraft Ambient Lighting Night", 0.5, 0.5, 0.5, 0.5, UIType.TextBox));//{tweak.parameters[9].value}
            newTweak.parameters.Add(new Parameter("DynamicLightIntensityNight", "Dynamic Lighting Intensity", 1, 1, 1, 1, UIType.TextBox));//{tweak.parameters[10].value}
            newTweak.parameters.Add(new Parameter("AdvancedPBRAircraftDirectLightingDuskDawn", "Dusk/Dawn Highlights", 6.5, 6.5, 6.5, 6.5, UIType.TextBox));//{tweak.parameters[11].value}
            newTweak.parameters.Add(new Parameter("AdvancedPBRAircraftOcclusionFactor", "PBR Aircraft Occlusion Factor", 1, 1, 1, 1, UIType.TextBox));//{tweak.parameters[12].value}
            tweaks.Add(newTweak);

            // -----------------------
            //  HDR Section
            // -----------------------

            newTweak = new Tweak("HDR_TONEMAP", Category.HDR, "Alternate tonemap adjustment", "This tweak replaces the default P3D tonemapper with a new tonemapper.");
            newTweak.parameters.Add(new Parameter("toneMapExposure", "Tonemap Exposure", 1.2, 1.2, 1.2, 1.2, UIType.TextBox));//{tweak.parameters[0].value}
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
