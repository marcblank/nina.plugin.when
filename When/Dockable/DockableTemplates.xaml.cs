using Accord.Math;
using AvalonDock.Controls;
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
        public void CheckConversion(object sender, RoutedEventArgs e) {
            DockableExpr expr = (DockableExpr)((RadioButton)sender).DataContext;
            String conversionType = (string)((RadioButton)sender).Content;
            expr.ConversionType = conversionType;
            Logger.Info("Checked conversion box: " + conversionType);
        }

        public void DeleteExpr(object sender, RoutedEventArgs e) {
            DockableExpr expr = (DockableExpr)((Button)sender).DataContext;
            WhenPluginDockable.RemoveExpr(expr);
        }

        public void DropExpr(object sender, DragEventArgs e) {
            Logger.Info("Item " + e.Data.GetData(DataFormats.StringFormat) + " dropped at " + ((FrameworkElement)sender).DataContext);
        }

        public void DragEnter(object sender, DragEventArgs e) {
            Logger.Info("Enter");

        }

        public void DragLeave(object sender, DragEventArgs e) {
            Logger.Info("Leave");

        }


        private void MouseMove(object sender, MouseEventArgs e) {
            // If the mousebutton isn't pressed, return immediately;
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            // Cast the sender (TextBox) to 0a F0rameworkElement
            // So we can grab the DataContext
            FrameworkElement fe = sender as FrameworkElement;
            if (fe == null)
                return;

            Grid g = fe.Parent as Grid;
            if (g == null) {
                return;
            }

            bool found = false;
            int i = 0;
            foreach (DockableExpr expr in WhenPluginDockable.ExpressionList) {
                if (expr == g.DataContext) {
                    found = true;
                    break;
                }
                i++;
            }

            if (!found) {
                Logger.Warning("WTF?");
                return;
            }

            e.Handled = true;

            // Wrap the data.
            DataObject data = new DataObject();
            data.SetData(i.ToString());

            Logger.Info("Dragging item #" + i);

            // Initiate the drag-and-drop operation.
            DragDrop.DoDragDrop(g, data, DragDropEffects.Move);
        }

    }
}
