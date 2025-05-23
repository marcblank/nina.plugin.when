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
using Antlr.Runtime;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;

namespace PowerupsLite.When {
    [ExportMetadata("Name", "Initialize Array")]
    [ExportMetadata("Description", "Creates or re-initializes an Array")]
    [ExportMetadata("Icon", "ArraySVG")]
    [ExportMetadata("Category", "Powerups Lite")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class InitializeArray : SequenceItem, IValidatable {

        [ImportingConstructor]
        public InitializeArray() : base() {
            Name = Name;
            Icon = Icon;
        }
        public InitializeArray(InitializeArray copyMe) : base(copyMe) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Name = copyMe.Name;
                Icon = copyMe.Icon;
            }
        }

        public static readonly String VALID_SYMBOL = "^[a-zA-Z][a-zA-Z0-9-+_]*$";


        [IsExpression]
        private string nameExpr;

        public override string ToString() {
                return $"Initialize Array: {NameExprExpression.StringValue}";
        }

        private IList<string> issues = new List<string>();
        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public bool Validate() {
            IList<string> i = new List<string>();

            if (NameExprExpression.StringValue != null) {
                if (NameExprExpression.StringValue.Length == 0) {
                    i.Add("A name for the Array must be specified");
                } else if (!Regex.IsMatch(NameExprExpression.StringValue, VALID_SYMBOL)) {
                    i.Add("The name of an Array must be alphanumeric");
                }
            }

            Expression.ValidateExpressions(i, NameExprExpression);

            Issues = i;
            RaisePropertyChanged("Issues");
            return i.Count == 0;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Array arr;
            NameExprExpression.Evaluate();
            if (Array.Arrays.TryGetValue(NameExprExpression.StringValue, out arr)) {
                arr.Clear();
            } else {
                Array.Arrays.TryAdd(NameExprExpression.StringValue, new Array());
            }
            return Task.CompletedTask;
        }
    }
}
