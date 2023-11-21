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
            ISwitchMediator switchMediator, IWeatherDataMediator weatherDataMediator, ICameraMediator cameraMediator, IDomeMediator domeMediator) {
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
            ConstantExpression.InitMediators(switchMediator, weatherDataMediator, cameraMediator, domeMediator);

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
            Globals.Items.Add(new SetConstant() { Constant = Name1, CValueExpr = Value1, GlobalName = "Name1", GlobalValue = "Value1" });
            Globals.Items.Add(new SetConstant() { Constant = Name2, CValueExpr = Value2, GlobalName = "Name2", GlobalValue = "Value2" });
            Globals.Items.Add(new SetConstant() { Constant = Name3, CValueExpr = Value3, GlobalName = "Name3", GlobalValue = "Value3" });
            Globals.Items.Add(new SetConstant() { Constant = Name4, CValueExpr = Value4, GlobalName = "Name4", GlobalValue = "Value4" });
            Globals.Items.Add(new SetConstant() { Constant = Name5, CValueExpr = Value5, GlobalName = "Name5", GlobalValue = "Value5" });
            Globals.Items.Add(new SetConstant() { Constant = Name6, CValueExpr = Value6, GlobalName = "Name6", GlobalValue = "Value6" });
            Globals.Items.Add(new SetConstant() { Constant = Name7, CValueExpr = Value7, GlobalName = "Name7", GlobalValue = "Value7" });
            Globals.Items.Add(new SetConstant() { Constant = Name8, CValueExpr = Value8, GlobalName = "Name8", GlobalValue = "Value8" });
            Globals.Items.Add(new SetConstant() { Constant = Name9, CValueExpr = Value9, GlobalName = "Name9", GlobalValue = "Value9" });
            Globals.Items.Add(new SetConstant() { Constant = Name10, CValueExpr = Value10, GlobalName = "Name10", GlobalValue = "Value10" });

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
                return pluginSettings.GetValueString(nameof(Name1), Settings.Default.Name1);
            }
            set {
                pluginSettings.SetValueString(nameof(Name1), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Value1 {
            get {
                return pluginSettings.GetValueString(nameof(Value1), Settings.Default.Value1);
            }
            set {
                pluginSettings.SetValueString(nameof(Value1), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Name2 {
            get {
                return pluginSettings.GetValueString(nameof(Name2), Settings.Default.Name2); //Settings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Name2), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Value2 {
            get {
                return pluginSettings.GetValueString(nameof(Value2), Settings.Default.Value2); ; //Settings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Value2), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Name3 {
            get {
                return pluginSettings.GetValueString(nameof(Name3), Settings.Default.Name3); //Settings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Name3), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Value3 {
            get {
                return pluginSettings.GetValueString(nameof(Value3), Settings.Default.Value3); //Settings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Value3), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Name4 {
            get {
                return pluginSettings.GetValueString(nameof(Name4), Settings.Default.Name4); //Settings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Name4), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Value4 {
            get {
                return pluginSettings.GetValueString(nameof(Value4), Settings.Default.Value4); //Settings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Value4), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Name5 {
            get {
                return pluginSettings.GetValueString(nameof(Name5), Settings.Default.Name5); //Settings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Name5), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Value5 {
            get {
                return pluginSettings.GetValueString(nameof(Value5), Settings.Default.Value5); //Settings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Value5), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Name6 {
            get {
                return pluginSettings.GetValueString(nameof(Name6), Settings.Default.Name6); //Settings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Name6), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Value6 {
            get {
                return pluginSettings.GetValueString(nameof(Value6), Settings.Default.Value6); //Settings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Value6), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Name7 {
            get {
                return pluginSettings.GetValueString(nameof(Name7), Settings.Default.Name7); //Settings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Name7), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Value7 {
            get {
                return pluginSettings.GetValueString(nameof(Value7), Settings.Default.Value7); //Settings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Value7), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Name8 {
            get {
                return pluginSettings.GetValueString(nameof(Name8), Settings.Default.Name8);
            }
            set {
                pluginSettings.SetValueString(nameof(Name8), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Value8 {
            get {
                return pluginSettings.GetValueString(nameof(Value8), Settings.Default.Value8); //Settings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Value8), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Name9 {
            get {
                return pluginSettings.GetValueString(nameof(Name9), Settings.Default.Name9); //Setings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Name9), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Value9 {
            get {
                return pluginSettings.GetValueString(nameof(Value9), Settings.Default.Value9); //Settings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Value9), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Name10 {
            get {
                return pluginSettings.GetValueString(nameof(Name10), Settings.Default.Name10); //Settings.Default.Name10;
            }
            set {
                pluginSettings.SetValueString(nameof(Name10), value);
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }
        public string Value10 {
            get {
                return pluginSettings.GetValueString(nameof(Value10), Settings.Default.Value10); // Default.Value10;
            }
            set {
                pluginSettings.SetValueString(nameof(Value10), value);
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
