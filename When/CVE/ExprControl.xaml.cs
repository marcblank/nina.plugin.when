using NINA.Sequencer;
using System;
using System.Windows;
using System.Windows.Controls;

namespace WhenPlugin.When {

    public partial class ExprControl : UserControl {
        public ExprControl() {
            InitializeComponent();
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(ExprControl), null);

        public string Label { get; set; }

        public static readonly DependencyProperty ExpProperty =
             DependencyProperty.Register("Exp", typeof(Expr), typeof(ExprControl), null);

        public Expr Exp { get; set; }

 
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

