using NINA.Sequencer.SequenceItem;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WhenPlugin.When {

    [Export(typeof(ResourceDictionary))]
    public partial class WhenTemplates : ResourceDictionary {

        public WhenTemplates() {
            InitializeComponent();
        }
        public void IfSwitch_PredicateToolTip(object sender, ToolTipEventArgs e) {
            TextBox predicateText = (TextBox)sender;
            IfSwitch ifSwitch = (IfSwitch)(predicateText.DataContext);
            predicateText.ToolTip = ifSwitch.ShowCurrentInfo();
        }
        public void WhenSwitch_PredicateToolTip(object sender, ToolTipEventArgs e) {
            TextBox predicateText = (TextBox)sender;
            WhenSwitch whenSwitch = (WhenSwitch)(predicateText.DataContext);
            predicateText.ToolTip = whenSwitch.ShowCurrentInfo();
        }

        public void IfConstant_PredicateToolTip(object sender, ToolTipEventArgs e) {
            try {
                ConstantControl predicateText = (ConstantControl)sender;
                IfConstant ifConstant = (IfConstant)(predicateText.DataContext);
                predicateText.ToolTip = ifConstant.ShowCurrentInfo();
            } catch (Exception) { }
        }

    }
}