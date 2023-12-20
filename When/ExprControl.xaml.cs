using NINA.Sequencer;
using System;
using System.Windows;
using System.Windows.Controls;

namespace WhenPlugin.When {

    public partial class ExprControl : UserControl {
        public ExprControl() {
            InitializeComponent();
        }

        public static readonly DependencyProperty ValProperty =
            DependencyProperty.Register("Val", typeof(string), typeof(ExprControl), null);

        public string Val { get; set; }

        public static readonly DependencyProperty ErrProperty =
             DependencyProperty.Register("Err", typeof(string), typeof(ExprControl), null);

        public string Err { get; set; }


        public static readonly DependencyProperty ValidateProperty =
             DependencyProperty.Register("Validate", typeof(String), typeof(ExprControl), null);

        public String Validate { get; set; }
 
        public static readonly DependencyProperty TypeProperty =
              DependencyProperty.Register("Type", typeof(String), typeof(ExprControl), null);

        public String Type { get; set; }

        public void ShowConstants(object sender, ToolTipEventArgs e) {
            //TextBox tb = (TextBox)sender;
            //ISequenceEntity item = (ISequenceEntity)tb.DataContext;
            //var stack = ConstantExpression.GetKeyStack(item);
            //if (stack == null || stack.Count == 0) {
            //    tb.ToolTip = "There are no valid, defined constants.";
            //} else {
            //    tb.ToolTip = ConstantExpression.DissectExpression(item, tb.Text, stack);
            //}
        }
        public void IfConstant_PredicateToolTip(object sender, ToolTipEventArgs e) {
        //    TextBox predicateText = (TextBox)sender;
        //    IfConstant ifConstant = (IfConstant)(predicateText.DataContext);
        //    predicateText.ToolTip = ifConstant.ShowCurrentInfo();
        //
        //
        }


    }
}

