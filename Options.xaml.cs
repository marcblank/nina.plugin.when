using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WhenPlugin.When {

    [Export(typeof(ResourceDictionary))]
    partial class Options : ResourceDictionary {

        public Options() {
            InitializeComponent();
        }

        public void ShowConstants(object sender, ToolTipEventArgs e) {
            Symbol.ShowSymbols(sender);
        }

    }
}