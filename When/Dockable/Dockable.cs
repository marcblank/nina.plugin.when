using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WhenPlugin.When {
    /// <summary>
    /// This Class shows the basic principle on how to add a new panel to N.I.N.A. Imaging tab via the plugin interface
    /// In this example an altitude chart is added to the imaging tab that shows the altitude chart based on the position of the telescope    
    /// </summary>
    [Export(typeof(IDockableVM))]
    public class WhenPluginDockable : DockableVM {
        private INighttimeCalculator nighttimeCalculator;
        private ITelescopeMediator telescopeMediator;

        [ImportingConstructor]
        public WhenPluginDockable(
            IProfileService profileService,
            ITelescopeMediator telescopeMediator,
            INighttimeCalculator nighttimeCalculator) : base(profileService) {

            this.nighttimeCalculator = nighttimeCalculator;
            this.telescopeMediator = telescopeMediator;
            Title = "Powerups Panel";
            Target = null;

            // Some asynchronous initialization
            Task.Run(() => {
                NighttimeData = nighttimeCalculator.Calculate();
                nighttimeCalculator.OnReferenceDayChanged += NighttimeCalculator_OnReferenceDayChanged;
            });

            // Registering to profile service events to react on
            profileService.LocationChanged += (object sender, EventArgs e) => {
                Target?.SetDateAndPosition(NighttimeCalculator.GetReferenceDate(DateTime.Now), profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
            };

            profileService.HorizonChanged += (object sender, EventArgs e) => {
                Target?.SetCustomHorizon(profileService.ActiveProfile.AstrometrySettings.Horizon);
            };

            ExpressionString = "WindSpeed\0WindGust\0Temperature\0Altitude";
            BuildExprList();
            ConditionWatchdog = new ConditionWatchdog(UpdateData, TimeSpan.FromSeconds(5));
            ConditionWatchdog.Start();
        }

        public static ConditionWatchdog ConditionWatchdog { get; set; }

        private void BuildExprList() {
            string[] l = ExpressionString.Split('\0');
            foreach (string s in l) {
                SetVariable sv = new SetVariable();
                sv.AttachNewParent(rootytooty);
                ExpressionList.Add(new Expr(sv, s));
            }
        }

        private SequenceRootContainer rootytooty = new SequenceRootContainer();

        private void NighttimeCalculator_OnReferenceDayChanged(object sender, EventArgs e) {
            NighttimeData = nighttimeCalculator.Calculate();
            RaisePropertyChanged(nameof(NighttimeData));
        }

        private static Task UpdateData() {
            foreach (Expr e in ExpressionList) {
                e.Evaluate();
            }
            return Task.CompletedTask;
        }

        public NighttimeData NighttimeData { get; private set; }
        public TelescopeInfo TelescopeInfo { get; private set; }
        public DeepSkyObject Target { get; private set; }

        public string ExpressionString { get; private set; }

        public static IList<Expr> ExpressionList { get; private set; } = new List<Expr>();
    }
}
