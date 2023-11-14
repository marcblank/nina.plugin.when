﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using System.Windows.Media;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Serialization;
using NINA.Sequencer;
using NINA.ViewModel.Sequencer;
using System.Reflection;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using NINA.Core.Enum;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "End Sequence")]
    [ExportMetadata("Description", "Ends the currenty running sequence; the End Sequence instructions will run")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Sequencer")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class EndSequence : SequenceItem, IValidatable {

        static protected ISequenceMediator sequenceMediator;
        static protected ISequenceNavigationVM sequenceNavigationVM;
        private static IProfileService profileService;
        private protected ISequence2VM s2vm;

        public static int instanceNumber = 0;

        [ImportingConstructor]
        public EndSequence(ISequenceMediator seqMediator, IProfileService pService) {
            sequenceMediator = seqMediator;
            profileService = pService;

            // Get the various NINA components we need
            if (sequenceNavigationVM == null) {
                FieldInfo fi = sequenceMediator.GetType().GetField("sequenceNavigation", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fi != null) {
                    sequenceNavigationVM = (ISequenceNavigationVM)fi.GetValue(sequenceMediator);
                    s2vm = sequenceNavigationVM.Sequence2VM;
                }
            } else if (s2vm == null) {
                s2vm = sequenceNavigationVM.Sequence2VM;
            }
        }

        public EndSequence(EndSequence copyMe) : this(sequenceMediator, profileService) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
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


        private async Task<bool> SkipToEndOfSequence() {
            var sequencer = s2vm.Sequencer;
            var startContainer = sequencer.MainContainer.Items[0] as ISequenceContainer;
            var targetContainer = sequencer.MainContainer.Items[1] as ISequenceContainer;
            if (startContainer.Status == SequenceEntityStatus.RUNNING) {
                await startContainer.Interrupt();
                await Task.Delay(100);
            }
            if (targetContainer.Status == SequenceEntityStatus.RUNNING) {
                await targetContainer.Interrupt();
            }

            foreach (var item in targetContainer.Items) {
                item.Status = SequenceEntityStatus.FINISHED;
            }

            sequenceNavigationVM.Sequence2VM.StartSequenceCommand.Execute(true);

            return true;
        }


        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Info("EndSequence running...");
            ISequenceEntity p = Parent;
            while (p != null) {
                if (p is Runner r && r.cts != null) {
                    Logger.Info("Stopping runner cts");
                    r.cts.Cancel();
                    break; // return Task.CompletedTask;
                }
                p = p.Parent;
            }
            if (s2vm != null) {
                _ = SkipToEndOfSequence();
                //FieldInfo fi = s2vm.GetType().GetField("cts", BindingFlags.Instance | BindingFlags.NonPublic);
                //if (fi != null) {
                    // CancellationTokenSource cts = (CancellationTokenSource)fi.GetValue(s2vm);
                    //Logger.Info("Stopping sequencer");
                    //cts?.Cancel();
                //}
            }
            return Task.CompletedTask;
        }

        public override object Clone() {
            return new EndSequence(this) {
            };
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(EndSequence)}";
        }

        public bool Validate() {
            var i = new List<string>();
            Issues = i;
            return i.Count == 0;
        }
    }
}
