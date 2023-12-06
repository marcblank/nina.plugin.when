using NINA.Sequencer;
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
        public void LoopWhile_PredicateToolTip(object sender, ToolTipEventArgs e) {
            try {
                ConstantControl predicateText = (ConstantControl)sender;
                LoopWhile loopWhile = (LoopWhile)(predicateText.DataContext);
                predicateText.ToolTip = loopWhile.ShowCurrentInfo();
            } catch (Exception) { }
        }

        public void WhenSwitch_StopStartToolTip(object sender, ToolTipEventArgs e) {
            TextBlock startStopText = (TextBlock)sender;
            WhenUnsafe whenUnsafe = (WhenUnsafe)(startStopText.DataContext);
            if (whenUnsafe.Stopped && whenUnsafe.InFlight) {
                startStopText.ToolTip = "When Becomes Unsafe ended with an 'End Sequence' instruction, disabling the trigger. This button re-enables that trigger.";
            } else if (whenUnsafe.Stopped) {
                startStopText.ToolTip = "Allows the When Becomes Unsafe instructions that you previously paused to continue.";
            } else {
                startStopText.ToolTip = "Pauses the execution of the When Becomes Unsafe instructions.";
            }

        }

        public void ShowConstants(object sender, ToolTipEventArgs e) {
            TextBox tb = (TextBox)sender;
            ISequenceEntity item = (ISequenceEntity)tb.DataContext;
            var stack = ConstantExpression.GetKeyStack(item);
            if (stack == null || stack.Count == 0) {
                tb.ToolTip = "There are no valid, defined constants.";
            } else {
                tb.ToolTip = ConstantExpression.DissectExpression(item, tb.Text, stack);
            }
        }


    }
}