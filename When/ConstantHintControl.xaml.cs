using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WhenPlugin.When {

    public partial class ConstantHintControl : UserControl {
        public ConstantHintControl() {
            InitializeComponent();
        }

        public static readonly DependencyProperty HintExprProperty =
            DependencyProperty.Register("HintExpr", typeof(String), typeof(ConstantHintControl), new PropertyMetadata("Foo", OnHintExprChanged));

        public String HintExpr {
            get { return (String)GetValue(HintExprProperty); }
            set { SetValue(HintExprProperty, value); }
        }

        private static void OnHintExprChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            d.SetValue(HintExprProperty, e.NewValue);
        }

        public static readonly DependencyProperty HintValuProperty =
             DependencyProperty.Register("HintValu", typeof(String), typeof(ConstantHintControl), new PropertyMetadata("Foo", OnHintValuChanged));

        public String HintValu {
            get { return (String)GetValue(HintValuProperty); }
            set { SetValue(HintValuProperty, value); }
        }
        

        private static void OnHintValuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            d.SetValue(HintValuProperty, e.NewValue);
        }

        public static readonly DependencyProperty DefaultExprProperty =
             DependencyProperty.Register("DefaultExpr", typeof(String), typeof(ConstantHintControl), new PropertyMetadata("Foo", OnDefaultExprChanged));

        public String DefaultExpr {
            get { return (String)GetValue(DefaultExprProperty); }
            set { SetValue(DefaultExprProperty, value); }
        }


        private static void OnDefaultExprChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            d.SetValue(DefaultExprProperty, e.NewValue);
        }

        public static readonly DependencyProperty HintValidateProperty =
             DependencyProperty.Register("HintValidate", typeof(String), typeof(ConstantHintControl), new PropertyMetadata("Foo", OnHintValidateChanged));

        public String HintValidate {
            get { return (String)GetValue(HintValidateProperty); }
            set { SetValue(HintValidateProperty, value); }
        }


        private static void OnHintValidateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            d.SetValue(HintValidateProperty, e.NewValue);
        }


    }
}

