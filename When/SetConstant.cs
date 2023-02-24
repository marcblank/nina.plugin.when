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
using NINA.Sequencer.Container;
using System.Diagnostics;
using Castle.Core.Internal;

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

        public bool IsSetConstant { get; set; } = false;

        [JsonProperty]
        public string Constant {
            get => constant;
            set {
                if (value == constant) {
                    return;
                }
                // ** Fix when Constant can be an expression
                if (constant != value || Parent == null) {
                    if (ConstantExpression.IsValid(this, Dummy, value, out double val, null)) {
                    }
                    constant = value;
                    ConstantExpression.FlushKeys();
                    ConstantExpression.UpdateConstants(this);
                }
                RaisePropertyChanged();
                if (Parent != null) {
                    foreach (var val in Parent.Items) {
                        if (val is SetConstant) {
                            ConstantExpression.Evaluate(val, "CValueExpr", "CValue", "", null);
                        }
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

        public bool DuplicateName { get; set; } = false;
        
        private string cValueExpr = "0";
        [JsonProperty]
        public string CValueExpr {
            get => cValueExpr;
            set {
                if (cValueExpr == value) {
                    return;
                }
                cValueExpr = value;
                ConstantExpression.Evaluate(this, "CValueExpr", "CValue", "");
                ConstantExpression.FlushKeys();
                ConstantExpression.UpdateConstants(this);
                RaisePropertyChanged("CValueExpr");
                if (Parent == ConstantExpression.GlobalContainer) {
                    //WhenPlugin.UpdateGlobalConstants();
                    foreach (var val in Parent.Items) {
                        if (val is SetConstant) {
                            ConstantExpression.Evaluate(val, "CValueExpr", "CValue", "", null);
                        }
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

        private bool IsAttachedToRoot() {
            ISequenceContainer p = Parent;
            while (p != null) {
                if (p is SequenceRootContainer) {
                    return true;
                }
                p = p.Parent;
            }
            return false;
        }

        private ISequenceContainer LastParent { get; set; }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            
            if (IsAttachedToRoot()) {
                ConstantExpression.FlushKeys();
            } else if (LastParent != null) {
                ConstantExpression.FlushContainerKeys(LastParent);
                return;
            }
            
            ConstantExpression.UpdateConstants(this);
            ConstantExpression.GlobalContainer.Validate();
            LastParent = Parent;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SetConstant)}, Constant: {Constant}, Value: {CValueExpr}";
        }

        public bool Validate() {
            if (!IsAttachedToRoot()) return true;

            var i = new List<string>();
            ConstantExpression.Evaluate(this, "CValueExpr", "CValue", "", i);

            if (DuplicateName) {
                i.Add("Duplicate name in the same instruction set!");
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
