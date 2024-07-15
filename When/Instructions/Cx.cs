﻿using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using NINA.Sequencer.Validations;
using System.Collections.Generic;
using System.Reflection;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Constant")]
    [ExportMetadata("Description", "Creates a Constant whose numeric value can be used in various instructions")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class SetConstant : Symbol, IValidatable {

        [ImportingConstructor]
        public SetConstant() : base() {
            Name = Name;
            Icon = Icon;
        }
        public SetConstant(SetConstant copyMe) : base(copyMe) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Name = copyMe.Name;
                Icon = copyMe.Icon;               
            }
        }

        public override object Clone() {
            SetConstant clone = new SetConstant(this);
 
            clone.Identifier = Identifier;
            clone.Definition = Definition;
            clone.Expr = Expr;
            return clone;
       }

        public override string ToString() {
            if (Expr != null) {
                return $"Constant: {Identifier}, Definition: {Definition}, Parent {Parent?.Name} Dirty: {Expr.Dirty}";

            } else {
                return $"Constant: {Identifier}, Definition: {Definition}, Parent {Parent?.Name} Expr: null";
            }
        }

        public override bool Validate() {
            if (!IsAttachedToRoot()) return true;

            IList<string> i = new List<string>();

            if (Identifier.Length == 0 || Definition.Length == 0) {
                i.Add("A name and a value must be specified");
            } else  if (!Regex.IsMatch(Identifier, VALID_SYMBOL)) {
                i.Add("The name of a Constant must be alphanumeric");
            } else if (IsDuplicate) {
                i.Add("The Constant is already defined here; this definition will be ignored.");
            }

            Expr.Validate();
            foreach (var kvp in Expr.Resolved) {
                if (kvp.Value == null || kvp.Value is SetVariable) {
                    i.Add("Constant definitions may not include Variables");
                    break;
                }
            }

            Issues = i;
            RaisePropertyChanged("Issues");
            return i.Count == 0;
         }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            // Doesn't Execute
            return Task.CompletedTask;
        }

        // Global Constants

        private string globalName = null;
        public string GlobalName {
            get => globalName;
            set {
                globalName = value;
            }
        }

        public void SetGlobalName (string name) {
            PropertyInfo pi = WhenPluginObject.GetType().GetProperty(GlobalName);
            pi?.SetValue(WhenPluginObject, name, null);
        }

        private string globalValue = null;
        public string GlobalValue {
            get => globalValue;
            set {
                globalValue = value;
            }
        }

        public void SetGlobalValue(string expr) {
            PropertyInfo pi = WhenPluginObject.GetType().GetProperty(GlobalValue);
            pi?.SetValue(WhenPluginObject, expr, null);
        }

        public string GlobalAll { get; set; }

        public string Dummy;

        private bool allProfiles = true;

        public bool AllProfiles {
            get => allProfiles;
            set {
                if (GlobalName != null) {
                    PropertyInfo pi = WhenPluginObject.GetType().GetProperty(GlobalAll);
                    pi?.SetValue(WhenPluginObject, value, null);
                }
                allProfiles = value;
            }
        }

        // Legacy

        [JsonProperty]
        public string Constant {
            get => null;
            set {
                if (value != null) Identifier = value;
            }
        }
        
        [JsonProperty]
        public string CValueExpr {
            get => null;
            set {
                if (value != null) Definition = value;
            }
        }


    }
}
