using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using NCalc;
using Castle.Core.Internal;
using NINA.Core.Utility.Notification;
using NINA.Core.Enum;
using System.Linq;
using System.Text;
using Accord.IO;
using Namotion.Reflection;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Interfaces;
using NINA.Sequencer.SequenceItem.Utility;
using System.Windows;
using NINA.Equipment.Equipment.MyWeatherData;
using System.Windows.Controls;
using System.Diagnostics;
using NINA.Core.Utility;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "If Switch/Weather (Deprecated; use If)")]
    [ExportMetadata("Description", "Executes an instruction set if the expression, based on current switch and/or weather values, is true")]
    [ExportMetadata("Icon", "SwitchesSVG")]
    [ExportMetadata("Category", "Switch")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    
    public class IfSwitch : IfCommand, IValidatable, IIfWhenSwitch {
        private ISwitchMediator switchMediator;
        private IWeatherDataMediator weatherMediator;

        [ImportingConstructor]
        public IfSwitch(ISwitchMediator switchMediator, IWeatherDataMediator weatherMediator) {
            Predicate = "";
            Instructions = new IfContainer();
            Instructions.AttachNewParent(Parent);
            Instructions.PseudoParent = this;
            this.switchMediator = switchMediator;
            this.weatherMediator = weatherMediator;
        }

        public IfSwitch(IfSwitch copyMe) : this(copyMe.switchMediator, copyMe.weatherMediator) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Predicate = copyMe.Predicate;
                Instructions = (IfContainer)copyMe.Instructions.Clone();
                Instructions.AttachNewParent(Parent);
                Instructions.PseudoParent = this;
            }
        }

        public override object Clone() {
            return new IfSwitch(this) {
            };
        }

         public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
 
            if (Predicate.IsNullOrEmpty()) {
                Status = SequenceEntityStatus.FAILED;
                return;
            }

            while (true) {
                try {
                    object result = IfWhenSwitch.EvaluatePredicate(Predicate, switchMediator, weatherMediator);
                    if (result == null) {
                        // Syntax error...
                        Logger.Info("IfSwitch: There is a syntax error in your predicate expression.");
                        Status = NINA.Core.Enum.SequenceEntityStatus.FAILED;
                        return;
                    }

                    if (result != null && result is Boolean && (Boolean)result) {
                        Logger.Info("IfSwitch: If Predicate is true!");
                        Runner runner = new Runner(Instructions, null, progress, token);
                        await runner.RunConditional();
                        if (runner.ShouldRetry) {
                            runner.ResetProgress();
                            runner.ShouldRetry = false;
                            Logger.Info("IfSwitch; retrying the failed instruction");
                            continue;
                        }
                    } else {
                        return;
                    }
                } catch (ArgumentException ex) {
                    Logger.Info("IfSwitch error: " + ex.Message);
                    Status = SequenceEntityStatus.FAILED;
                }
                return;
            }
        }

        [JsonProperty]
        public string Predicate { get; set; }

        [JsonProperty]
        public string PredicateValue { get; set; }  

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(IfSwitch)}, Predicate: {Predicate}";
        }

        public new bool Validate() {
            CommonValidate();

            var i = new List<string>();

            if (Predicate.IsNullOrEmpty()) {
                i.Add("Expression cannot be empty!");
            }

            try {
                IfWhenSwitch.EvaluatePredicate(Predicate, switchMediator, weatherMediator);
            } catch (Exception ex) {
                i.Add("Error in expression: " + ex.Message);
            }

            PredicateValue = "";

            
            if (switchMediator.GetInfo().Connected || weatherMediator.GetInfo().Connected) { }
            else {
                i.Add("Neither switch nor weather device is connected");
                Issues = i;
                return false;
            }

            Switches = IfWhenSwitch.GetSwitchList(switchMediator, weatherMediator);
            RaisePropertyChanged("Switches");

            Issues = i;
            return i.Count == 0;
        }

        public string ShowCurrentInfo() {
            return IfWhenSwitch.ShowCurrentInfo(Predicate, switchMediator, weatherMediator);
        }


        public bool Check() {
            object result = IfWhenSwitch.EvaluatePredicate(Predicate, switchMediator, weatherMediator);
            if (result == null) {
                return true;
            }
            return (result != null && result is Boolean && (Boolean)result);
        }

        private IList<string> switches = new List<string>();

        public IList<string> Switches {
            get => switches;
            set {
                switches = value;
                RaisePropertyChanged();
            }
        }

     }
}
