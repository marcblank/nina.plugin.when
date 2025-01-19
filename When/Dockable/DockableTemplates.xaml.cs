using Accord.Math;
using AvalonDock.Controls;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
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

        public void DragFeedback(object sender, GiveFeedbackEventArgs e) {
        }

        public void DropExpr(object sender, DragEventArgs e) {
            if (e.Source is TextBlock tb && tb.DataContext is DockableExpr de) {
                Grid gg = tb.Parent as Grid;
                if (gg != null) {
                    gg.Opacity = 1;
                    gg.Background = OldBackground;
                }
            }

            Expr target = ((FrameworkElement)sender).DataContext as Expr;
            ObservableCollection<DockableExpr> exprs = WhenPluginDockable.ExpressionList;
            if (target == null) return;
            int targetIndex = -1;

            for (int i = 0; i < exprs.Count; i++) {
                if (exprs[i] == target) {
                    targetIndex = i;
                }
            }

            if (targetIndex < 0) return;

            string data = (string)e.Data.GetData(DataFormats.StringFormat);
            Int32 sourceIndex = Int32.Parse(data);

            if (targetIndex == sourceIndex) return;

            DockableExpr source = exprs[sourceIndex];
            if (targetIndex > sourceIndex) {
                for (int i = sourceIndex + 1; i <= targetIndex; i++) {
                    exprs[i - 1] = exprs[i];
                }
            } else {
                for (int i = sourceIndex - 1; i >= targetIndex; i--) {
                    exprs[i + 1] = exprs[i];
                }
            }
            exprs[targetIndex] = source;
            WhenPluginDockable.SaveDockableExprs();
            Logger.Info("Item " + e.Data.GetData(DataFormats.StringFormat) + " dropped at " + ((FrameworkElement)sender).DataContext);
        }

        public void DragEnter(object sender, DragEventArgs e) {
            if (e.Source is TextBlock tb && tb.DataContext is DockableExpr de) {
                Grid gg = tb.Parent as Grid;
                if (gg != null) {
                    OldBackground = gg.Background;
                    gg.Background = new SolidColorBrush(Colors.LightBlue);
                    gg.Opacity = .75;
                }
            }
           
            Logger.Info("Enter");

        }

        public Brush OldBackground { get; private set; }

        public void DragLeave(object sender, DragEventArgs e) {
            if (e.Source is TextBlock tb && tb.DataContext is DockableExpr de) {
                Grid gg = tb.Parent as Grid;
                if (gg != null) {
                    gg.Opacity = 1;
                    gg.Background = OldBackground;
                }
            }
            Logger.Info("Leave");

        }

        private void PreviewDragOver(object sender, DragEventArgs e) {
            e.Handled = true;
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

            TextBox tb = null;
            foreach (UIElement item in g.Children) {
                if (item is TextBox) {
                    tb = (TextBox)item;
                    break;
                }
            }

            CreateDragDropWindow(tb);
            System.Windows.DragDrop.AddQueryContinueDragHandler(g, DragContinueHandler);
            System.Windows.DragDrop.AddGiveFeedbackHandler(g, DragFeedbackHandler);

            // Initiate the drag-and-drop operation.
            DragDrop.DoDragDrop(g, data, DragDropEffects.Move);
            Logger.Info("Done");
        }

        private Window _dragdropWindow;

        private void CreateDragDropWindow(Visual dragElement) {
            _dragdropWindow = new Window {
                WindowStyle = WindowStyle.None,
                Opacity=.7,
                AllowsTransparency = true,
                AllowDrop = false,
                Background = null,
                IsHitTestVisible = false,
                SizeToContent = SizeToContent.WidthAndHeight,
                Topmost = true,
                ShowInTaskbar = false
            };

            Rectangle r = new Rectangle {
                Width = ((FrameworkElement)dragElement).ActualWidth,
                Height = ((FrameworkElement)dragElement).ActualHeight,
                Fill = new VisualBrush(dragElement)
            };
            _dragdropWindow.Content = r;

            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);


            _dragdropWindow.Left = w32Mouse.X;
            _dragdropWindow.Top = w32Mouse.Y;
            _dragdropWindow.Show();
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point {
            public Int32 X;
            public Int32 Y;
        };

        public void DragFeedbackHandler(object sender, GiveFeedbackEventArgs e) {
            Mouse.SetCursor(Cursors.Hand);
            e.Handled = true;
        }
        
        public void DragContinueHandler(object sender, QueryContinueDragEventArgs e) {
            if (e.Action == DragAction.Continue && e.KeyStates != DragDropKeyStates.LeftMouseButton) {
                _dragdropWindow.Close();
            } else {
                Win32Point w32Mouse = new Win32Point();
                GetCursorPos(ref w32Mouse);
                _dragdropWindow.Left = w32Mouse.X + 10;
                _dragdropWindow.Top = w32Mouse.Y + 10;
                //_dragdropWindow.Show();
            }
        }

    }
}
