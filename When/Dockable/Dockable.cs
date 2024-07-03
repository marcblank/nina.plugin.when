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
        public WhenPluginDockable(IProfileService profileService) : base(profileService) {
            Title = "Powerups Panel";

            ExpressionString = "WindSpeed\0WindGust\0Temperature\0Altitude";
            BuildExprList();
        }

        private void BuildExprList() {
            string[] l = ExpressionString.Split('\0');
            foreach (string s in l) {
                SetVariable sv = new SetVariable();
                sv.AttachNewParent(PseudoRoot);
                ExpressionList.Add(new Expr(sv, s));
            }
        }

        private SequenceRootContainer PseudoRoot = new SequenceRootContainer();

        public static Task UpdateData() {
            foreach (Expr e in ExpressionList) {
                e.Evaluate();
            }
            return Task.CompletedTask;
        }

        public string ExpressionString { get; private set; }

        public static IList<Expr> ExpressionList { get; private set; } = new List<Expr>();
    }
}
