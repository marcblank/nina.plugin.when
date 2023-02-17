using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WhenPlugin.When {

    public partial class ConstantControl : UserControl {
        public ConstantControl() {
            InitializeComponent();
        }

        public static readonly DependencyProperty ExprProperty =
            DependencyProperty.Register("Expr", typeof(String), typeof(ConstantControl), null);

        public String Expr { get; set; }

        public static readonly DependencyProperty ValuProperty =
             DependencyProperty.Register("Valu", typeof(String), typeof(ConstantControl), null);

        public String Valu { get; set; }

        public static readonly DependencyProperty ValidateProperty =
             DependencyProperty.Register("Validate", typeof(String), typeof(ConstantControl), null);

        public String Validate { get; set; }
 
        public static readonly DependencyProperty TypeProperty =
              DependencyProperty.Register("Type", typeof(String), typeof(ConstantControl), null);

        public String Type { get; set; }
    }
}

