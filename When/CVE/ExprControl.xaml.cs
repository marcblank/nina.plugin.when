using NINA.Sequencer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
            TextBox tb = (TextBox)sender;
            BindingExpression be = tb.GetBindingExpression(TextBox.TextProperty);
            var exp = be.ResolvedSource as Expr;

            Dictionary<string, Symbol> syms = exp.Resolved;
            int cnt = syms.Count;
            if (cnt == 0) {
                tb.ToolTip = "No symbols used in this expression";
                return;
            }
            StringBuilder sb = new StringBuilder(cnt == 1 ? "Symbol: " : "Symbols: "));

            foreach (Symbol sym in syms.Values) {
                sb.Append(sym.Identifier.ToString());
                sb.Append(" (in ");
                sb.Append(sym.Parent.Name);
                sb.Append(")");
                if (--cnt > 0) sb.Append("; ");
            }
            tb.ToolTip = sb.ToString();
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

