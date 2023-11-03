using Castle.Core.Internal;
using NINA.Sequencer.SequenceItem;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WhenPlugin.When {

    public partial class ConstantControlVariable : UserControl {
        public ConstantControlVariable() {
            InitializeComponent();
        }

        public static readonly DependencyProperty ExprProperty =
            DependencyProperty.Register("Expr", typeof(String), typeof(ConstantControlVariable), null);

        public String Expr { get; set; }

        public static readonly DependencyProperty ValuProperty =
             DependencyProperty.Register("Valu", typeof(String), typeof(ConstantControlVariable), null);

        public String Valu { get; set; }

        public static readonly DependencyProperty ValidateProperty =
             DependencyProperty.Register("Validate", typeof(String), typeof(ConstantControlVariable), null);

        public String Validate { get; set; }

        public static readonly DependencyProperty TypeProperty =
              DependencyProperty.Register("Type", typeof(String), typeof(ConstantControlVariable), null);

        public String Type { get; set; }

        public void ShowConstants(object sender, ToolTipEventArgs e) {
            TextBox tb = (TextBox)sender;
            ISequenceItem item = (ISequenceItem)tb.DataContext;
            var stack = ConstantExpression.GetKeyStack(item);
            if (stack.IsNullOrEmpty()) {
                tb.ToolTip = "There are no valid, defined constants.";
            } else {
                tb.ToolTip = ConstantExpression.DissectExpression(item, tb.Text, stack);
            }
        }
        public void IfConstant_PredicateToolTip(object sender, ToolTipEventArgs e) {
            TextBox predicateText = (TextBox)sender;
            IfConstant ifConstant = (IfConstant)(predicateText.DataContext);
            predicateText.ToolTip = ifConstant.ShowCurrentInfo();
        }


    }
}

