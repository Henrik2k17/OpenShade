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

            newTweak = new Tweak("INITIAL_TWEAK", Category.EnhancedAtmospherics, "Enhanced Atmospherics Atmosphere", "");
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

            newTweak = new Tweak("INITIAL_TWEAK", Category.EnhancedAtmospherics, "Enhanced Atmospherics Clouds", "");
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

        public static void GeneratePostProcessData(List<PostProcess> postProcesses)
        {
            var newPost = new PostProcess("POSTPROCESS_SHADER Sepia", "Sepia", 0, "Sepia desaturates and colorizes the image with a specified color. The most obvious application is the traditional old-time photograph effect where the image colors are faded and then overlayed with a yellowish color, but other effects are possible for those seeking a quick two-in-one desaturation/toning effect on the image.");
            newPost.parameters.Add(new Parameter("ColorToneX,ColorToneY,ColorToneZ", "Color Tone", new RGB(1.4, 1.1, 0.9), new RGB(1.4, 1.1, 0.9), 0, 2.55, UIType.RGB, "ColorTone values 0.00 to 2.55 can be thought of as equivalents to 0 to 255.To find a sepia color, open the GIMP color chooser and note the RGB values of the selected color.\r\n\r\nSuppose we want to add a dark yellow. RGB = 173, 171, 59. Add a decimal point two places to the left in each value to produce 1.73, 1.71, and 0.59. Use those values for ColorTone."));
            newPost.parameters.Add(new Parameter("GreyPower", "Grey Power", 0.11, 0.11, 0, 1, UIType.Text, "Desaturates the image this much before tinting.\r\n\r\nIf GreyPower is over 1.00, then the image will appear over - whitened."));
            newPost.parameters.Add(new Parameter("SepiaPower", "Sepia Power", 0.58, 0.58, 0, 1, UIType.Text, "How strong the tint color should be.\r\n\r\nLower values are best used with SepiaPower.Increasing SepiaPower too much causes the image to appear colorized.Values 0.13 to 0.25 are best.Not too much, not too little."));
            newPost.parameters.Add(new Parameter("DayNightUse", "Time usage", "0", "0", new List<string>() { "Always", "Day only", "Night only", "Twilight", "Twilight+Day", "Twilight+Night" }, UIType.Combobox, ""));
            postProcesses.Add(newPost);

            newPost = new PostProcess("POSTPROCESS_SHADER Curves", "Curves", 1, "");
            newPost.parameters.Add(new Parameter("Curves_mode", "Curves Mode", 0, 0, 0, 0, UIType.Text));
            newPost.parameters.Add(new Parameter("Curves_contrast", "Curves Contrast", 0.65, 0.65, 0, 0, UIType.Text));
            newPost.parameters.Add(new Parameter("Curves_formula", "Curves Formula", 5, 5, 0, 0, UIType.Text));
            newPost.parameters.Add(new Parameter("DayNightUse", "Time usage", "0", "0", new List<string>() { "Always", "Day only", "Night only", "Twilight", "Twilight+Day", "Twilight+Night" }, UIType.Combobox, ""));
            postProcesses.Add(newPost);

            newPost = new PostProcess("POSTPROCESS_SHADER Levels", "Levels", 2, "Used sparingly, Levels will trim off excess whiteness, and it will darken shadows and other dark areas that appear too “washed out” when they should be darker.\r\n\r\nOn the other hand, visual detail is lost when used excessively, and drastic scene changes can be produced.This is either good or bad depending upon the desired effect.In short, Levels is an effect best used for minor touchups to the resulting image.");
            newPost.parameters.Add(new Parameter("Levels_black_point", "Black point", 16, 16, 0, 255, UIType.Text, "Anything below this value to 0 becomes solid black."));
            newPost.parameters.Add(new Parameter("Levels_white_point", "White point", 235, 235, 0, 255, UIType.Text, "Anything above this value to 255 becomes solid white."));
            newPost.parameters.Add(new Parameter("DayNightUse", "Time usage", "0", "0", new List<string>() { "Always", "Day only", "Night only", "Twilight", "Twilight+Day", "Twilight+Night" }, UIType.Combobox, ""));
            postProcesses.Add(newPost);

            newPost = new PostProcess("POSTPROCESS_SHADER LiftGammaGain", "Lift Gamma Gain", 3, "Lift Gamma Gain effect provides a fine amount of control over how gamma is applied to an image. While the Tonemap effect provides a basic gamma control for basic gamma application, Lift Gamma Gain allows for more precise gamma control over the brightness of shadow areas, midrange areas, and bright areas, and it can do so at the color level with RGB values.");
            newPost.parameters.Add(new Parameter("RGB_LiftX,RGB_LiftY,RGB_LiftZ", "RGB Lift", new RGB(1, 1, 1), new RGB(1, 1, 1), 0, 2, UIType.RGB, "Lowering RGB Lift makes dark areas darker. Raising RGB Lift makes dark areas lighter."));
            newPost.parameters.Add(new Parameter("RGB_GammaX,RGB_GammaY,RGB_GammaZ", "RGB Gamma", new RGB(1, 1, 1), new RGB(1, 1, 1), 0, 2, UIType.RGB, ""));
            newPost.parameters.Add(new Parameter("RGB_GainX,RGB_GainY,RGB_GainZ", "RGB Gain", new RGB(1, 1, 1), new RGB(1, 1, 1), 0, 2, UIType.RGB, "Raising RGB Gain makes light areas lighter. Lowering RGB Gain makes light areas darker."));
            newPost.parameters.Add(new Parameter("DayNightUse", "Time usage", "0", "0", new List<string>() { "Always", "Day only", "Night only", "Twilight", "Twilight+Day", "Twilight+Night" }, UIType.Combobox, ""));
            postProcesses.Add(newPost);

            newPost = new PostProcess("POSTPROCESS_SHADER Technicolor", "Technicolor", 4, "Technicolor attempts to recreate a pseudo-Technicolor effect by modifying the colors of the image enough to emulate the three-strip film process used by movie studios to produce color movies during the 1930s through 1950s.");
            newPost.parameters.Add(new Parameter("TechniAmount", "Techni Amount", 0.4, 0.4, 0, 1, UIType.Text, "Higher = more desaturated, color lessens, image colors appear faded\r\nLower = more color, color increases"));
            newPost.parameters.Add(new Parameter("TechniPower", "Techni Power", 4, 4, 0, 8, UIType.Text, "Higher = Closer to original white levels. 8 = Original whites.\r\nLower = More whites, brighter image"));
            newPost.parameters.Add(new Parameter("redNegativeAmount", "redNegativeAmount", 0.88, 0.88, 0, 1, UIType.Text, "Reducing this value adds more of Red"));
            newPost.parameters.Add(new Parameter("greenNegativeAmount", "Green Negative Amount", 0.88, 0.88, 0, 1, UIType.Text, "Reducing this value adds more of Green"));
            newPost.parameters.Add(new Parameter("blueNegativeAmount", "Blue Negative Amount", 0.88, 0.88, 0, 1, UIType.Text, "Reducing this value adds more of Blue"));
            newPost.parameters.Add(new Parameter("DayNightUse", "Time usage", "0", "0", new List<string>() { "Always", "Day only", "Night only", "Twilight", "Twilight+Day", "Twilight+Night" }, UIType.Combobox, ""));
            postProcesses.Add(newPost);

            newPost = new PostProcess("POSTPROCESS_SHADER Technicolor2", "Technicolor 2", 5, "Technicolor attempts to recreate a pseudo-Technicolor effect by modifying the colors of the image enough to emulate the three-strip film process used by movie studios to produce color movies during the 1930s through 1950s.");
            newPost.parameters.Add(new Parameter("ColorStrengthR,ColorStrengthG,ColorStrengthB", "Color Strength", new RGB(0.2, 0.2, 0.2), new RGB(0.2, 0.2, 0.2), 0, 0, UIType.RGB, "Higher means darker and more intense colors."));
            newPost.parameters.Add(new Parameter("Brightness", "Brightness", 1, 1, 0.5, 1.5, UIType.Text, "Higher means brighter image."));
            newPost.parameters.Add(new Parameter("Saturation", "Saturation", 1, 1, 0, 1.5, UIType.Text, "Additional saturation control since this effect tends to oversaturate the image."));
            newPost.parameters.Add(new Parameter("Strength", "Strength", 1, 1, 0, 1, UIType.Text, ""));
            newPost.parameters.Add(new Parameter("DayNightUse", "Time usage", "0", "0", new List<string>() { "Always", "Day only", "Night only", "Twilight", "Twilight+Day", "Twilight+Night" }, UIType.Combobox, ""));
            postProcesses.Add(newPost);

            newPost = new PostProcess("POSTPROCESS_SHADER Vibrance", "Vibrance", 6, "Vibrance saturates or desaturates the image by a specified color. Vibrance offers more flexibility over which color influences the image.\r\n\r\nVibrance does not remove all colors the way the monochrome effect produces a black and white image, but vibrance will make colors more colorful or less colorful depending upon the values used to adjust the effect.Faded colors can give the image a washed out film effect that other effects can refine.");
            newPost.parameters.Add(new Parameter("Vibrance", "Vibrance", 0.2, 0.2, -1, 1, UIType.Text, "Specifies how much to saturate (+) or desturate (-) the image."));
            newPost.parameters.Add(new Parameter("Vibrance_RGB_balanceX,Vibrance_RGB_balanceY,Vibrance_RGB_balanceZ", "Vibrance RGB Balance", new RGB(1, 1, 1), new RGB(1, 1, 1), -10, 10, UIType.RGB, "Gives priority to a given RGB color. The range for each color channel is -10.00 to 10.00. Each RGB channel value is multiplied by the value of Vibrance to produce the final adjustment that specifies how much to saturate or desaturate that color."));
            newPost.parameters.Add(new Parameter("DayNightUse", "Time usage", "0", "0", new List<string>() { "Always", "Day only", "Night only", "Twilight", "Twilight+Day", "Twilight+Night" }, UIType.Combobox, ""));
            postProcesses.Add(newPost);

            newPost = new PostProcess("POSTPROCESS_SHADER DPX3", "Cineon DPX", 7, "Cineon DPX setting allows limited post-production image effects that (somewhat) resemble the results obtained with the Cineon System released by Kodak sometime around 1992-1993.");
            newPost.parameters.Add(new Parameter("Red,Green,Blue", "RGB", new RGB(8, 8, 8), new RGB(8, 8, 8), 1, 15, UIType.RGB, ""));
            newPost.parameters.Add(new Parameter("RedC,GreenC,BlueC", "RGB C", new RGB(0.36, 0.36, 0.34), new RGB(0.36, 0.36, 0.34), 0.2, 0.5, UIType.RGB, ""));
            newPost.parameters.Add(new Parameter("Contrast", "Contrast", 0.1, 0.1, 0, 1, UIType.Text, ""));
            newPost.parameters.Add(new Parameter("Saturation", "Saturation", 3, 3, 0, 8, UIType.Text, ""));
            newPost.parameters.Add(new Parameter("Colorfulness", "Colorfulness", 2.5, 2.5, 0.1, 2.5, UIType.Text, ""));
            newPost.parameters.Add(new Parameter("Strength", "Strength", 0.2, 0.2, 0, 1, UIType.Text, ""));
            newPost.parameters.Add(new Parameter("DayNightUse", "Time usage", "0", "0", new List<string>() { "Always", "Day only", "Night only", "Twilight", "Twilight+Day", "Twilight+Night" }, UIType.Combobox, ""));
            postProcesses.Add(newPost);

            newPost = new PostProcess("POSTPROCESS_SHADER Tonemap", "Tonemap", 8, "Tonemap is an effect that adjusts a variety of related image enhancements that include gamma, saturation, bleach, exposure, and color removal.\r\n\r\nTonemap is a useful “many-in-one” effect. Other effects might offer a greater degree of control over the image, but if only minor modifications are needed by using one effect, then Tonemap has its place.");
            newPost.parameters.Add(new Parameter("Gamma", "Gamma", 1, 1, 0, 2, UIType.Text, "Adjusts gamma. However, this gamma control is limited. If only minor gamma adjustment is needed, then this should be enough, but for more accurate gamma control over the image, set the Tonemap Gamma to 1.000 (neutral) and use the Lift Gamma Gain effect instead."));
            newPost.parameters.Add(new Parameter("Exposure", "Exposure", 0, 0, -1, 1, UIType.Text, "Makes the image brighter. Brightens the darks so more detail is visible in dark areas."));
            newPost.parameters.Add(new Parameter("Saturation", "Saturation", 0, 0, -1, 1, UIType.Text, "Saturates or desaturates colors to make them more or less intense.Use negative values to desaturate colors, and use positive values to saturate colors.Saturation = 0 is the neutral setting and has no effect."));
            newPost.parameters.Add(new Parameter("Bleach", "Bleach", 0, 0, 0, 1, UIType.Text, "Avoid setting bleach above 1. The image becomes darker after that, not lighter. Whites become blacks while blacks remain dark. Small values, such as 0.020, are best."));
            newPost.parameters.Add(new Parameter("Defog", "Defog", 0, 0, 0, 1, UIType.Text, "Defog and FogColor work together. FogColor specifies a color to remove (in decimal RGB format), and Defog specifies how much of that color to remove."));
            newPost.parameters.Add(new Parameter("FogColorX,FogColorY,FogColorZ", "FogColor RGB", new RGB(0, 0, 2.55), new RGB(0, 0, 2.55), 0, 2.55, UIType.RGB, "This operates differently than might be expected because lower values preserve more color. Higher values indicate more of that color to be removed."));
            newPost.parameters.Add(new Parameter("DayNightUse", "Time usage", "0", "0", new List<string>() { "Always", "Day only", "Night only", "Twilight", "Twilight+Day", "Twilight+Night" }, UIType.Combobox, ""));
            postProcesses.Add(newPost);

            newPost = new PostProcess("POSTPROCESS_SHADER LumaSharpen", "Luma Sharpen", 9, "Lumasharpen sharpens the image to enhance details. The end result is similar to what would be seen after an image has been enhanced using the Unsharp Mask filter in GIMP or Photoshop.");
            newPost.parameters.Add(new Parameter("Sharp_strength", "Sharp Strength", 0.65, 0.65, 0.1, 3, UIType.Text, "Strength of the sharpening"));
            newPost.parameters.Add(new Parameter("Sharp_clamp", "Sharp Clamp", 0.035, 0.035, 0, 0, UIType.Text, "Limits maximum amount of sharpening a pixel recieves"));
            newPost.parameters.Add(new Parameter("Pattern", "Pattern", 2, 2, 1, 4, UIType.Text, "Choose a sample pattern. 1 = Fast, 2 = Normal, 3 = Wider, 4 = Pyramid shaped."));
            newPost.parameters.Add(new Parameter("Offset_bias", "Offset Bias", 1, 1, 0, 6, UIType.Text, "Offset bias adjusts the radius of the sampling pattern."));
            newPost.parameters.Add(new Parameter("DayNightUse", "Time usage", "0", "0", new List<string>() { "Always", "Day only", "Night only", "Twilight", "Twilight+Day", "Twilight+Night" }, UIType.Combobox, ""));
            postProcesses.Add(newPost);

            newPost = new PostProcess("POSTPROCESS_SHADER Colourfulness", "Colourfulness", 10, "The name says it all");
            newPost.parameters.Add(new Parameter("colourfulness", "Colourfulness", 0.4, 0.4, -1, 2, UIType.Text, "Degree of colourfulness, 0 = neutral"));
            newPost.parameters.Add(new Parameter("lim_luma", "Lim Luma", 0.7, 0.7, 0.1, 1, UIType.Text, "Lower values allow more change near clipping"));
            newPost.parameters.Add(new Parameter("DayNightUse", "Time usage", "0", "0", new List<string>() { "Always", "Day only", "Night only", "Twilight", "Twilight+Day", "Twilight+Night" }, UIType.Combobox, ""));
            postProcesses.Add(newPost);

        }
        
    }


}
