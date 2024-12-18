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
using System.Text.RegularExpressions;
using NINA.Sequencer.Container;
using NINA.Core.Utility.Converters;
using System.Diagnostics;
using NINA.Sequencer.Conditions;
using System.Runtime.Serialization;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using Accord.IO;
using System.Text;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "For Each in Array")]
    [ExportMetadata("Description", "Iterates over the elements of an Array, executing the embedded instructions for each")]
    [ExportMetadata("Icon", "ArraySVG")]
    [ExportMetadata("Category", "Powerups (Fun-ctions)")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]

    public class ForEachInArray : ForEachList, IValidatable {

        [ImportingConstructor]
        public ForEachInArray() : base() {
        }

        public ForEachInArray(ForEachInArray copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                ValueVariable = copyMe.ValueVariable;
                IndexVariable = copyMe.IndexVariable;
                Array = copyMe.Array;
            }
        }

        private string indexVariable = "";

        [JsonProperty]
        public string IndexVariable {
            get => indexVariable;
            set {
                if (Parent == null) {
                    //return;
                }
                indexVariable = value;
                RaisePropertyChanged();
            }
        }

        private string valueVariable = "";

        [JsonProperty]
        public string ValueVariable {
            get => valueVariable;
            set {
                if (Parent == null) {
                    //return;
                }
                valueVariable = value;
                RaisePropertyChanged();
            }
        }

        private string array = "";

        [JsonProperty]
        public string Array {
            get => array;
            set {
                if (Parent == null) {
                    //return;
                }
                array = value;
                RaisePropertyChanged();
            }
        }
        public override object Clone() {
            ForEachInArray ic = new ForEachInArray(this);
            ic.Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem));
            foreach (var item in ic.Items) {
                item.AttachNewParent(ic);
            }
            AttachNewParent(Parent);
            if (ic.Conditions.Count == 0) {
                ic.Add(new LoopCondition());
            }
            return ic;
        }


        public new string ValidateArguments () {

            if (IndexVariable == null || IndexVariable.Length == 0) {
                return "There must be an index variable specified";
            }
            if (ValueVariable == null || ValueVariable.Length == 0) {
                return "There must be a value variable specified";
            }

            VTokens = new string[] { IndexVariable, ValueVariable };

            return null;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(ForEachInArray)}, Variable: {Variable}, List Expression: {ListExpression}";
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Array == null || Array.Length == 0) {
                throw new SequenceEntityFailedException("An Array must be specified and must have been initialized");
            }

            if (!Symbol.Arrays.ContainsKey(Array)) {
                throw new SequenceEntityFailedException("The Array specified does not exist");
            }

            Symbol.Array a;
            if (!Symbol.Arrays.TryGetValue(Array, out a)) {
                throw new SequenceEntityFailedException("Huh?  Key exists but not Array??");
            }

            ETokens = new string[a.Count];
            int i = 0;
            foreach (var kvp in a) {
                ETokens[i++] = kvp.Key + "," + kvp.Value;
            }

            if (Conditions.Count > 0) {
                LoopCondition lp = Conditions[0] as LoopCondition;
                if (lp != null) {
                    lp.Iterations = ETokens.Length;
                }
            }

            Variable = IndexVariable + "," + ValueVariable;
            StringBuilder sb = new StringBuilder();
            foreach (string e in ETokens) {
                sb.Append(e);
                sb.Append(";");
            }
            ListExpression = sb.ToString();

            return base.Execute(progress, token);
        }

        public new bool Validate() {

            var i = new List<string>();
            if (!IsAttachedToRoot()) return true;

            string e = ValidateArguments();
            if (e != null) {
                i.Add(e);
            }

            Issues = i;
            RaisePropertyChanged("Issues");
            return i.Count == 0;
        }
    }
}
