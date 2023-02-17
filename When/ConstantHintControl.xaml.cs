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
            DependencyProperty.Register("HintExpr", typeof(String), typeof(ConstantHintControl), null);

        public String HintExpr { get; set; }

        public static readonly DependencyProperty HintValuProperty =
            DependencyProperty.Register("HintValu", typeof(String), typeof(ConstantHintControl), null);

        public String HintValu { get; set; }

        public static readonly DependencyProperty DefaultExprProperty =
             DependencyProperty.Register("DefaultExpr", typeof(String), typeof(ConstantHintControl), null);

        public String DefaultExpr { get; set; }

        public static readonly DependencyProperty HintValidateProperty =
             DependencyProperty.Register("HintValidate", typeof(String), typeof(ConstantHintControl), null);

        public String HintValidate { get; set; }
    }
}

