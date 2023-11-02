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
using NINA.Core.Enum;
using NINA.Sequencer;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Set Variable")]
    [ExportMetadata("Description", "If the variable has been previously defined, its value will become the result of the specified expression")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Constants Enhanced")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class ResetVariable : SequenceItem, IValidatable {
        [ImportingConstructor]
        public ResetVariable() {
            Variable = "";
            Icon = Icon;
        }
        public ResetVariable(ResetVariable copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                CValueExpr = copyMe.CValueExpr;
                Icon = copyMe.Icon;
            }
        }

        public string Dummy;

        public static WhenPlugin WhenPluginObject { get; set; }

        private string variable;

        public bool IsSetvariable { get; set; } = false;

        [JsonProperty]
        public string Variable {
            get => variable;
            set {
                if (value == variable) {
                    return;
                }
                variable = value;
                RaisePropertyChanged();
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
                RaisePropertyChanged("CValueExpr");
                RaisePropertyChanged("CValue");
                ConstantExpression.GlobalContainer.Validate();
            }
        }

        private string cValue = "Undefined";

        public string ValidateVariable(double var) {
            ISequenceEntity p = ConstantExpression.FindKeyContainer(Parent, Variable);
            if (p == null) {
                return "Not Yet Defined";
            }
            if (p is ISequenceContainer sc) {
                foreach (ISequenceEntity item in sc.Items) {
                    if (item is SetVariable sv && sv.Variable.Equals(Variable)) {
                        if (item.Status == SequenceEntityStatus.FINISHED) {
                            return String.Empty;
                        } else {
                            break;
                        }
                    }
                }
            }
            return "Not Yet Defined";
        }

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
            // Find the SetVariable for this variable
            ISequenceEntity p = ConstantExpression.FindKeyContainer(Parent, Variable);
            if (p == null) {
                throw new SequenceEntityFailedException("Variable is undefined.");
            }
            if (p is ISequenceContainer sc) {
                foreach (ISequenceEntity item in sc.Items) {
                    if (item is SetVariable sv && sv.Variable.Equals(Variable)) {
                        // Found it!
                        ConstantExpression.Evaluate(this, "CValueExpr", "CValue", "");
                        sv.CValue = cValue;
                        sv.CValueExpr = cValue;
                        RaisePropertyChanged("CValueExpr");
                        RaisePropertyChanged("CValue");
                        ConstantExpression.UpdateConstants(this);
                        return Task.CompletedTask;
                    }
                }
            }
            // Change its CValueExpr or CValue?
            Status = SequenceEntityStatus.FAILED;
            return Task.CompletedTask;
        }

        public override object Clone() {
            return new ResetVariable(this) {
                Variable = variable,
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
            LastParent = Parent;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(ResetVariable)}, Variable: {variable}, ValueExpr: {CValueExpr}, Value: {CValue}";
        }

        public bool Validate() {
            if (!IsAttachedToRoot()) return true;

            var i = new List<string>();

            if (DuplicateName) {
                i.Add("Duplicate name in the same instruction set!");
            }

            RaisePropertyChanged("CValue");
            RaisePropertyChanged("CValueExpr");
            
            Issues = i;
            if (Issues.Count > 0) {
                cValue = Double.NaN.ToString();
            }
            return Issues.Count == 0;
        }
    }
}
