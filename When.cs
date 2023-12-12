using WhenPlugin.When.Properties;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Image.ImageData;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Settings = WhenPlugin.When.Properties.Settings;
using System.Reflection.Metadata;
using WhenPlugin.When;
using NINA.Sequencer.Container;
using Namotion.Reflection;
using System.Reflection;
using System.Drawing;
using System.Windows.Media;
using System.Windows;
using NINA.Equipment.Interfaces.Mediator;

namespace WhenPlugin.When {
    /// <summary>
    /// This class exports the IPluginManifest interface and will be used for the general plugin information and options
    /// The base class "PluginBase" will populate all the necessary Manifest Meta Data out of the AssemblyInfo attributes. Please fill these accoringly
    /// 
    /// An instance of this class will be created and set as datacontext on the plugin options tab in N.I.N.A. to be able to configure global plugin settings
    /// The user interface for the settings will be defined by a DataTemplate with the key having the naming convention "When_Options" where When corresponds to the AssemblyTitle - In this template example it is found in the Options.xaml
    /// </summary>
    [Export(typeof(IPluginManifest))]
    public class WhenPlugin : PluginBase, INotifyPropertyChanged {
        private readonly IPluginOptionsAccessor pluginSettings;
        private readonly IProfileService profileService;

        // Implementing a file pattern
        private GeometryGroup ConstantsIcon = (GeometryGroup)Application.Current.Resources["Pen_NoFill_SVG"];

        [ImportingConstructor]
        public WhenPlugin(IProfileService profileService, IOptionsVM options, IImageSaveMediator imageSaveMediator, 
            ISwitchMediator switchMediator, IWeatherDataMediator weatherDataMediator, ICameraMediator cameraMediator, IDomeMediator domeMediator,
                IFlatDeviceMediator flatMediator, IFilterWheelMediator filterWheelMediator, IRotatorMediator rotatorMediator) {
            if (Settings.Default.UpdateSettings) {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Settings.Default);
            }

            // This helper class can be used to store plugin settings that are dependent on the current profile
            this.pluginSettings = new PluginOptionsAccessor(profileService, Guid.Parse(this.Identifier));
            this.profileService = profileService;
            // React on a changed profile
            profileService.ProfileChanged += ProfileService_ProfileChanged;

            // Hook into image saving for adding FITS keywords or image file patterns
            CreateGlobalSetConstants(this);
            SetConstant.WhenPluginObject = this;
            ConstantExpression.InitMediators(switchMediator, weatherDataMediator, cameraMediator, domeMediator, flatMediator, filterWheelMediator, profileService, rotatorMediator);
        }

        public override Task Teardown() {
            // Make sure to unregister an event when the object is no longer in use. Otherwise garbage collection will be prevented.
            profileService.ProfileChanged -= ProfileService_ProfileChanged;
            return base.Teardown();
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            // Rase the event that this profile specific value has been changed due to the profile switch
            Globals.Items.Clear();
            CreateGlobalSetConstants(this);
            RaisePropertyChanged(nameof(ProfileSpecificNotificationMessage));
        }

        private Task ImageSaveMediator_BeforeImageSaved(object sender, BeforeImageSavedEventArgs e) {
            // Insert the example FITS keyword of a specific data type into the image metadata object prior to the file being saved
            // FITS keywords have a maximum of 8 characters. Comments are options. Comments that are too long will be truncated.

            string exampleKeywordComment = "This is a {0} keyword";

            // string
            string exampleStringKeywordName = "STRKEYWD";
            string exampleStringKeywordValue = "Example";
            e.Image.MetaData.GenericHeaders.Add(new StringMetaDataHeader(exampleStringKeywordName, exampleStringKeywordValue, string.Format(exampleKeywordComment, "string")));

            // integer
            string exampleIntKeywordName = "INTKEYWD";
            int exampleIntKeywordValue = 5;
            e.Image.MetaData.GenericHeaders.Add(new IntMetaDataHeader(exampleIntKeywordName, exampleIntKeywordValue, string.Format(exampleKeywordComment, "integer")));

            // double
            string exampleDoubleKeywordName = "DBLKEYWD";
            double exampleDoubleKeywordValue = 1.3d;
            e.Image.MetaData.GenericHeaders.Add(new DoubleMetaDataHeader(exampleDoubleKeywordName, exampleDoubleKeywordValue, string.Format(exampleKeywordComment, "double")));

            // Classes also exist for other data types:
            // BoolMetaDataHeader()
            // DateTimeMetaDataHeader()

            return Task.CompletedTask;
        }

        public SequenceContainer Globals {
            get => ConstantExpression.GlobalContainer;
            set { }
        }

        private void CreateGlobalSetConstants (WhenPlugin plugin) {
            Globals.Name = "Global Constants";
            var def = Properties.Settings.Default;
            Globals.Items.Add(new SetConstant() { Constant = Name1, CValueExpr = Value1, AllProfiles = All1, GlobalName = "Name1", GlobalValue = "Value1", GlobalAll = "All1" });
            Globals.Items.Add(new SetConstant() { Constant = Name2, CValueExpr = Value2, AllProfiles = All2, GlobalName = "Name2", GlobalValue = "Value2", GlobalAll = "All2" });
            Globals.Items.Add(new SetConstant() { Constant = Name3, CValueExpr = Value3, AllProfiles = All3, GlobalName = "Name3", GlobalValue = "Value3", GlobalAll = "All3" });
            Globals.Items.Add(new SetConstant() { Constant = Name4, CValueExpr = Value4, AllProfiles = All4, GlobalName = "Name4", GlobalValue = "Value4", GlobalAll = "All4" });
            Globals.Items.Add(new SetConstant() { Constant = Name5, CValueExpr = Value5, AllProfiles = All5, GlobalName = "Name5", GlobalValue = "Value5", GlobalAll = "All5" });
            Globals.Items.Add(new SetConstant() { Constant = Name6, CValueExpr = Value6, AllProfiles = All6, GlobalName = "Name6", GlobalValue = "Value6", GlobalAll = "All6" });
            Globals.Items.Add(new SetConstant() { Constant = Name7, CValueExpr = Value7, AllProfiles = All7, GlobalName = "Name7", GlobalValue = "Value7", GlobalAll = "All7" });
            Globals.Items.Add(new SetConstant() { Constant = Name8, CValueExpr = Value8, AllProfiles = All8, GlobalName = "Name8", GlobalValue = "Value8", GlobalAll = "All8" });
            Globals.Items.Add(new SetConstant() { Constant = Name9, CValueExpr = Value9, AllProfiles = All9, GlobalName = "Name9", GlobalValue = "Value9", GlobalAll = "All9" });
            Globals.Items.Add(new SetConstant() { Constant = Name10, CValueExpr = Value10, AllProfiles = All10, GlobalName = "Name10", GlobalValue = "Value10", GlobalAll = "All10" });

            foreach (var item in Globals.Items) {
                item.AttachNewParent(Globals);
                item.Icon = ConstantsIcon;
                item.Name = "Global Constant";
            }

            Globals.Validate();
            RaisePropertyChanged("Globals");
        }

        public string Name1 {
            get {
                if (!All1) {
                    return pluginSettings.GetValueString(nameof(Name1), Settings.Default.Name1);
                } else {
                    return Settings.Default.Name1;
                }
            }
            set {
                if (!All1) {
                    pluginSettings.SetValueString(nameof(Name1), value);
                } else {
                    Settings.Default.Name1 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Value1 {
            get {
                if (!All1) {
                    return pluginSettings.GetValueString(nameof(Value1), Settings.Default.Value1);
                } else {
                    return Settings.Default.Value1;
                }
            }
            set {
                if (!All1) {
                    pluginSettings.SetValueString(nameof(Value1), value);
                } else {
                    Settings.Default.Value1 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Name2 {
            get {
                if (!All2) {
                    return pluginSettings.GetValueString(nameof(Name2), Settings.Default.Name2);
                } else {
                    return Settings.Default.Name2;
                }
            }
            set {
                if (!All2) {
                    pluginSettings.SetValueString(nameof(Name2), value);
                } else {
                    Settings.Default.Name2 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Value2 {
            get {
                if (!All2) {
                    return pluginSettings.GetValueString(nameof(Value2), Settings.Default.Value2);
                } else {
                    return Settings.Default.Value2;
                }
            }
            set {
                if (!All2) {
                    pluginSettings.SetValueString(nameof(Value2), value);
                } else {
                    Settings.Default.Value2 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Name3 {
            get {
                if (!All3) {
                    return pluginSettings.GetValueString(nameof(Name3), Settings.Default.Name3);
                } else {
                    return Settings.Default.Name3;
                }
            }
            set {
                if (!All3) {
                    pluginSettings.SetValueString(nameof(Name3), value);
                } else {
                    Settings.Default.Name3 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Value3 {
            get {
                if (!All3) {
                    return pluginSettings.GetValueString(nameof(Value3), Settings.Default.Value4);
                } else {
                    return Settings.Default.Value3;
                }
            }
            set {
                if (!All3) {
                    pluginSettings.SetValueString(nameof(Value3), value);
                } else {
                    Settings.Default.Value3 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Name4 {
            get {
                if (!All4) {
                    return pluginSettings.GetValueString(nameof(Name4), Settings.Default.Name4);
                } else {
                    return Settings.Default.Name4;
                }
            }
            set {
                if (!All4) {
                    pluginSettings.SetValueString(nameof(Name4), value);
                } else {
                    Settings.Default.Name4 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Value4 {
            get {
                if (!All4) {
                    return pluginSettings.GetValueString(nameof(Value4), Settings.Default.Value4);
                } else {
                    return Settings.Default.Value4;
                }
            }
            set {
                if (!All4) {
                    pluginSettings.SetValueString(nameof(Value4), value);
                } else {
                    Settings.Default.Value4 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Name5 {
            get {
                if (!All5) {
                    return pluginSettings.GetValueString(nameof(Name5), Settings.Default.Name5);
                } else {
                    return Settings.Default.Name5;
                }
            }
            set {
                if (!All5) {
                    pluginSettings.SetValueString(nameof(Name5), value);
                } else {
                    Settings.Default.Name5 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Value5 {
            get {
                if (!All5) {
                    return pluginSettings.GetValueString(nameof(Value5), Settings.Default.Value5);
                } else {
                    return Settings.Default.Value5;
                }
            }
            set {
                if (!All5) {
                    pluginSettings.SetValueString(nameof(Value5), value);
                } else {
                    Settings.Default.Value5 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Name6 {
            get {
                if (!All6) {
                    return pluginSettings.GetValueString(nameof(Name6), Settings.Default.Name6);
                } else {
                    return Settings.Default.Name6;
                }
            }
            set {
                if (!All6) {
                    pluginSettings.SetValueString(nameof(Name6), value);
                } else {
                    Settings.Default.Name6 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Value6 {
            get {
                if (!All6) {
                    return pluginSettings.GetValueString(nameof(Value6), Settings.Default.Value6);
                } else {
                    return Settings.Default.Value6;
                }
            }
            set {
                if (!All6) {
                    pluginSettings.SetValueString(nameof(Value6), value);
                } else {
                    Settings.Default.Value6 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Name7 {
            get {
                if (!All7) {
                    return pluginSettings.GetValueString(nameof(Name7), Settings.Default.Name7);
                } else {
                    return Settings.Default.Name7;
                }
            }
            set {
                if (!All7) {
                    pluginSettings.SetValueString(nameof(Name7), value);
                } else {
                    Settings.Default.Name7 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Value7 {
            get {
                if (!All7) {
                    return pluginSettings.GetValueString(nameof(Value7), Settings.Default.Value7);
                } else {
                    return Settings.Default.Value7;
                }
            }
            set {
                if (!All7) {
                    pluginSettings.SetValueString(nameof(Value7), value);
                } else {
                    Settings.Default.Value7 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Name8 {
            get {
                if (!All8) {
                    return pluginSettings.GetValueString(nameof(Name8), Settings.Default.Name8);
                } else {
                    return Settings.Default.Name8;
                }
            }
            set {
                if (!All8) {
                    pluginSettings.SetValueString(nameof(Name8), value);
                } else {
                    Settings.Default.Name8 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Value8 {
            get {
                if (!All8) {
                    return pluginSettings.GetValueString(nameof(Value8), Settings.Default.Value8);
                } else {
                    return Settings.Default.Value8;
                }
            }
            set {
                if (!All8) {
                    pluginSettings.SetValueString(nameof(Value8), value);
                } else {
                    Settings.Default.Value8 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Name9 {
            get {
                if (!All9) {
                    return pluginSettings.GetValueString(nameof(Name9), Settings.Default.Name9);
                } else {
                    return Settings.Default.Name9;
                }
            }
            set {
                if (!All9) {
                    pluginSettings.SetValueString(nameof(Name9), value);
                } else {
                    Settings.Default.Name9 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Value9 {
            get {
                if (!All9) {
                    return pluginSettings.GetValueString(nameof(Value9), Settings.Default.Value9);
                } else {
                    return Settings.Default.Value9;
                }
            }
            set {
                if (!All9) {
                    pluginSettings.SetValueString(nameof(Value9), value);
                } else {
                    Settings.Default.Value9 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Name10 {
            get {
                if (!All10) {
                    return pluginSettings.GetValueString(nameof(Name10), Settings.Default.Name10);
                } else {
                    return Settings.Default.Name10;
                }
            }
            set {
                if (!All10) {
                    pluginSettings.SetValueString(nameof(Name10), value);
                } else {
                    Settings.Default.Name10 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string Value10 {
            get {
                if (!All10) {
                    return pluginSettings.GetValueString(nameof(Value10), Settings.Default.Value10);
                } else {
                    return Settings.Default.Value10;
                }
            }
            set {
                if (!All10) {
                    pluginSettings.SetValueString(nameof(Value10), value);
                } else {
                    Settings.Default.Value10 = value;
                }
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }



        public bool All1 {
            get { 
                return Settings.Default.All1;
            }
            set {
                Settings.Default.All1 = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public bool All2 {
            get {
                return Settings.Default.All2;
            }
            set {
                Settings.Default.All2 = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public bool All3 {
            get {
                return Settings.Default.All3;
            }
            set {
                Settings.Default.All3 = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public bool All4 {
            get {
                return Settings.Default.All4;
            }
            set {
                Settings.Default.All4 = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public bool All5 {
            get {
                return Settings.Default.All5;
            }
            set {
                Settings.Default.All5 = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public bool All6 {
            get {
                return Settings.Default.All6;
            }
            set {
                Settings.Default.All6 = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public bool All7 {
            get {
                return Settings.Default.All7;
            }
            set {
                Settings.Default.All7 = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public bool All8 {
            get {
                return Settings.Default.All8;
            }
            set {
                Settings.Default.All8 = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public bool All9 {
            get {
                return Settings.Default.All9;
            }
            set {
                Settings.Default.All9 = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public bool All10 {
            get {
                return Settings.Default.All10;
            }
            set {
                Settings.Default.All10 = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }


        private string GetValue(string name) {
            return pluginSettings.GetValueString(name, string.Empty);
        }

        private void SetValue(string name, string value) {
            pluginSettings.SetValueString(name, value);
            CoreUtil.SaveSettings(Settings.Default);
        }


        public string ProfileSpecificNotificationMessage {
            get {
                return pluginSettings.GetValueString(nameof(ProfileSpecificNotificationMessage), string.Empty);
            }
            set {
                pluginSettings.SetValueString(nameof(ProfileSpecificNotificationMessage), value);
                RaisePropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
