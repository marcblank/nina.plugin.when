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
            DependencyProperty.Register("Expr", typeof(String), typeof(ConstantControl), new PropertyMetadata("Foo", OnExprChanged));

        public String Expr {
            get { return (String)GetValue(ExprProperty); }
            set { SetValue(ExprProperty, value); }
        }

        private static void OnExprChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            //d.SetValue(ExprProperty, e.NewValue);
        }

        public static readonly DependencyProperty ValuProperty =
             DependencyProperty.Register("Valu", typeof(String), typeof(ConstantControl), new PropertyMetadata("Foo", OnValuChanged));

        public String Valu {
            get { return (String)GetValue(ValuProperty); }
            set { SetValue(ValuProperty, value); }
        }

        private static void OnValuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            d.SetValue(ValuProperty, e.NewValue);
        }

        public static readonly DependencyProperty ValidateProperty =
             DependencyProperty.Register("Validate", typeof(String), typeof(ConstantControl), new PropertyMetadata("Foo", OnValidateChanged));

        public String Validate {
            get { return (String)GetValue(ValidateProperty); }
            set { SetValue(ValidateProperty, value); }
        }

        private static void OnValidateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            d.SetValue(ValidateProperty, e.NewValue);
        }
    }
}

