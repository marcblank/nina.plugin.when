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

    public partial class ExprHintControl : UserControl {
        public ExprHintControl() {
            InitializeComponent();
        }

        public static readonly DependencyProperty ExpProperty =
            DependencyProperty.Register("Exp", typeof(Expr), typeof(ExprHintControl), null);

        public Expr Exp { get; set; }

        public static readonly DependencyProperty ValProperty =
            DependencyProperty.Register("Val", typeof(String), typeof(ExprHintControl), null);

        public String Val { get; set; }

        public static readonly DependencyProperty DefaultProperty =
             DependencyProperty.Register("Default", typeof(String), typeof(ExprHintControl), null);

        public String Default { get; set; }

        public void ShowConstants(object sender, ToolTipEventArgs e) {
            TextBox tb = (TextBox)sender;
            ISequenceItem item = (ISequenceItem)tb.DataContext;
            var stack = ConstantExpression.GetKeyStack(item);
            tb.ToolTip = ConstantExpression.DissectExpression(item, tb.Text, stack);
        }

    }
}

