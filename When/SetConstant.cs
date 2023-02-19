using WhenPlugin.When;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Settings = WhenPlugin.When.Properties.Settings;
using System.Windows.Media;
using NJsonSchema.Validation.FormatValidators;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Reflection;
using NINA.Profile;
using System.Configuration;
using System.Linq;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Define Constant")]
    [ExportMetadata("Description", "Sets a constant whose numeric value can be used in various instructions")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Constants Enhanced")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SetConstant : SequenceItem, IValidatable {
        [ImportingConstructor]
        public SetConstant() {
            Constant = "";
            Icon = Icon;
        }
        public SetConstant(SetConstant copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                CValueExpr = copyMe.CValueExpr;
                Icon = copyMe.Icon;
            }
        }

        public string GlobalName { get; set; }
        public string GlobalValue { get; set; }

        public string Dummy;

        public static WhenPlugin WhenPluginObject { get; set; }

        private string constant;

        [JsonProperty]
        public string Constant {
            get => constant;
            set {
                // ** Fix when Constant can be an expression
                if (constant != value) {
                    if (ConstantExpression.IsValid(this, Dummy, value, out double val, null)) {
                    }
                    constant = value;
                    ConstantExpression.FlushKeys();
                    ConstantExpression.UpdateConstants(this);
                }
                RaisePropertyChanged();
                if (Parent == ConstantExpression.GlobalContainer) {
                    foreach (IValidatable val in Parent.Items.Cast<IValidatable>()) {
                        val.Validate();
                    }
                }
                if (GlobalName!= null) {
                    PropertyInfo pi = WhenPluginObject.GetType().GetProperty(GlobalName);
                    pi?.SetValue(WhenPluginObject, value, null);
                }
                // Force every expression to re-evaluate
                ConstantExpression.GlobalContainer.Validate();
            }
        }
 
        private string cValueExpr = "0";
        [JsonProperty]
        public string CValueExpr {
            get => cValueExpr;
            set {
                cValueExpr = value;
                ConstantExpression.Evaluate(this, "CValueExpr", "CValue", "");
                ConstantExpression.FlushKeys();
                ConstantExpression.UpdateConstants(this);
                RaisePropertyChanged("CValueExpr");
                if (Parent == ConstantExpression.GlobalContainer) {
                    //WhenPlugin.UpdateGlobalConstants();
                    foreach (IValidatable val in Parent.Items.Cast<IValidatable>()) {
                        val.Validate();
                    }
                    if (GlobalName != null) {
                        PropertyInfo pi = WhenPluginObject.GetType().GetProperty(GlobalValue);
                        pi?.SetValue(WhenPluginObject, value, null);
                    }
                }
                ConstantExpression.GlobalContainer.Validate();
            }
        }

        private string cValue = "";

        [JsonProperty]
        public string CValue {
            get => cValue;
            set {
                cValue = value;
                RaisePropertyChanged();
            }
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            // Nothing to do here
            return Task.CompletedTask;
        }

        public override object Clone() {
            return new SetConstant(this) {
                Constant = Constant,
                CValueExpr = CValueExpr
            };
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            ConstantExpression.UpdateConstants(this);
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SetConstant)}, Constant: {Constant}, Value: {CValueExpr}";
        }

        public bool Validate() {
            var i = new List<string>();
            if (!ConstantExpression.Evaluate(this, "CValueExpr", "CValue", "", i)) {

            }
            Issues = i;
            if (Issues.Count > 0) {
                cValue = Double.NaN.ToString();
            }
            RaisePropertyChanged("CValueExpr");
            RaisePropertyChanged("CValue");
            return Issues.Count == 0;
        }
    }
}
