using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Enum;
using NINA.Core.Utility;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "If")]
    [ExportMetadata("Description", "Executes an instruction set if the Expression is True (or 1)")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class IfConstant : IfCommand, IValidatable, ITrueFalse {

        [ImportingConstructor]
        public IfConstant() {
            IfExpr = new Expr(this);
            Instructions = new IfContainer();
            Instructions.AttachNewParent(Parent);
            Instructions.PseudoParent = this;
            Instructions.Name = Name;
            Instructions.Icon = Icon;
        }

        public IfConstant(IfConstant copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                IfExpr = new Expr(this, copyMe.IfExpr.Expression);
                Instructions = (IfContainer)copyMe.Instructions.Clone();
                Instructions.AttachNewParent(Parent);
                Instructions.PseudoParent = this;
                Instructions.Name = copyMe.Name;
                Instructions.Icon = copyMe.Icon;
            }
        }

        public override object Clone() {
            return new IfConstant(this) {
            };
        }

        public bool Check() {

             return false;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            Logger.Info("Predicate: " + IfExpr.Expression);
            if (string.IsNullOrEmpty(IfExpr.Expression)) {
                Status = SequenceEntityStatus.FAILED;
                return;
            }

            try {
                // Always get latest data...
                await Symbol.UpdateSwitchWeatherData();
                
                if (IfExpr.ImageVolatile) {
                    Logger.Info("ImageVolatile");
                    while (TakeExposure.LastImageProcessTime < TakeExposure.LastExposureTIme) {
                        Logger.Info("Waiting 250ms for processing...");
                        progress?.Report(new ApplicationStatus() { Status = "" });
                        await CoreUtil.Wait(TimeSpan.FromMilliseconds(250), token, default);
                    }
                    // Get latest values
                    Logger.Info("ImageVolatile, new data");
                }

                IfExpr.Evaluate();

                if (!string.Equals(IfExpr.ValueString, "0", StringComparison.OrdinalIgnoreCase) && (IfExpr.Error == null)) {
                    Logger.Info("Predicate is true, " + IfExpr);
                    Runner runner = new Runner(Instructions, progress, token);
                    await runner.RunConditional();
                } else {
                    Logger.Info("Predicate is false, " + IfExpr);
                    return;
                }
            } catch (ArgumentException ex) {
                Logger.Info("If error: " + ex.Message);
                Status = SequenceEntityStatus.FAILED;
            }
        }

        [JsonProperty]
        public string Predicate {
            get => null;
            set {
                IfExpr.Expression = value;
                RaisePropertyChanged("IfExpr");

            }
        }

        private Expr _IfExpr;
        [JsonProperty]
        public Expr IfExpr {
            get => _IfExpr;
            set {
                _IfExpr = value;
                RaisePropertyChanged();
            }
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(IfConstant)}, Expr: {IfExpr}";
        }

        public IList<string> Switches { get; set; } = null;

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            IfExpr.Evaluate();
        }

        public new bool Validate() {

            CommonValidate();

            var i = new List<string>();
            Expr.CheckExprError(IfExpr, i);

            Switches = Symbol.GetSwitches();
            RaisePropertyChanged("Switches");

            IfExpr.Validate();
 
            Issues = i;
            return i.Count == 0;
        }
    }
}
