﻿using Accord.Diagnostics;
using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Interrupt Trigger")]
    [ExportMetadata("Description", "This trigger will stop execution after the currently running instruction, allowing you to add whatever instructions you want before proceeding.")]
    [ExportMetadata("Icon", "SequenceSVG")]
    [ExportMetadata("Category", "Sequencer")]
    [Export(typeof(ISequenceTrigger))]
    
    [JsonObject(MemberSerialization.OptIn)]
    public class InterruptTrigger : SequenceTrigger, IValidatable {

        private GeometryGroup HourglassIcon = (GeometryGroup)Application.Current.Resources["HourglassSVG"];

        [ImportingConstructor]
        public InterruptTrigger() {
            AddItem(TriggerRunner, new WaitIndefinitely() { Name="Wait Indefinitely", Icon = HourglassIcon }); ;
        }

        private void AddItem(SequentialContainer runner, ISequenceItem item) {
            runner.Items.Add(item);
            item.AttachNewParent(runner);
        }

        private InterruptTrigger(InterruptTrigger copyMe) {
            CopyMetaData(copyMe);
            Name = copyMe.Name;
            Icon = copyMe.Icon;
            TriggerRunner = (SequentialContainer)copyMe.TriggerRunner.Clone();
        }

        public override object Clone() {
            return new InterruptTrigger(this);
        }

        public bool InFlight { get; set; }

        public IList<string> Issues => new List<string>();

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            InFlight = true;
            try {
                await TriggerRunner.Run(progress, token);
            } finally {
                //InFlight = false;
            }
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            return !InFlight;
        }

        Random random = new Random();

        /// <summary>
        /// This string will be used for logging
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(InterruptTrigger)}";
        }

        public bool Validate() {
            // Make sure a proper tree is maintained
            foreach (ISequenceItem item in TriggerRunner.Items) {
                item.AttachNewParent(TriggerRunner);
            }
            return true;
        }
    }
}