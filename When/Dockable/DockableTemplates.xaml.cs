using Accord.Math;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WhenPlugin.When {
    /// <summary>
    /// Interaction logic for DockableTemplates.xaml
    ///
    /// </summary>
    /// 
    [Export(typeof(ResourceDictionary))]
    public partial class DockableTemplates : ResourceDictionary {
        public DockableTemplates() {
            InitializeComponent();
        }

        public void OpenTooltip(object sender, ToolTipEventArgs e) {
            ((DockableExpr)((TextBlock)sender).DataContext).IsOpen = true;
            e.Handled = true;
        }

        public void CheckDisplay(object sender, RoutedEventArgs e) {
            DockableExpr expr = (DockableExpr)((RadioButton)sender).DataContext;
            String displayType = (string)((RadioButton)sender).Content;
            expr.DisplayType = displayType;
            Logger.Info("Checked display box: " + displayType);
        }

        public void DeleteExpr(object sender, RoutedEventArgs e) {
            DockableExpr expr = (DockableExpr)((Button)sender).DataContext;
            WhenPluginDockable.RemoveExpr(expr);
        }
    }
}
