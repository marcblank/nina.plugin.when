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
            DependencyProperty.Register("Expr", typeof(String), typeof(ConstantControl), new PropertyMetadata("Foo", OnChanged));
        
        public String Expr {
            get { return (String)GetValue(ExprProperty); }
            set { SetValue(ExprProperty, value); }
        }

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            d.SetValue(ExprProperty, e.NewValue);
        }

        //<local:ConstantControl Margin = "0,0,10,5" Expr="{Binding Path=IterationsExpr, Mode=TwoWay}" />


    }
}

