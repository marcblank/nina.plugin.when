#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Validations;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Model.Equipment;
using NINA.Core.Locale;
using NINA.Equipment.Model;
using NINA.Equipment.Equipment.MyCamera;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Image.Interfaces;
using NINA.Image.ImageData;
using Namotion.Reflection;
using System.Windows.Navigation;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Take Exposure +")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Imaging_TakeExposure_Description")]
    [ExportMetadata("Icon", "CameraSVG")]
    [ExportMetadata("Category", "Powerups (Enhanced Instructions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TakeExposure : SequenceItem, IExposureItem, IValidatable {
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IImageSaveMediator imageSaveMediator;
        private IImageHistoryVM imageHistoryVM;
        private IProfileService profileService;
        Task imageProcessingTask;

        [ImportingConstructor]
        public TakeExposure(IProfileService profileService, ICameraMediator cameraMediator, IImagingMediator imagingMediator, IImageSaveMediator imageSaveMediator, IImageHistoryVM imageHistoryVM) {
            ImageType = CaptureSequence.ImageTypes.LIGHT;
            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistoryVM = imageHistoryVM;
            this.profileService = profileService;
            CameraInfo = this.cameraMediator.GetInfo();
            EExpr = new Expr(this);
            GExpr = new Expr(this, "", "Integer");
            OExpr = new Expr(this, "", "Integer");

        }

        private TakeExposure(TakeExposure cloneMe) : this(cloneMe.profileService, cloneMe.cameraMediator, cloneMe.imagingMediator, cloneMe.imageSaveMediator, cloneMe.imageHistoryVM) {
            CopyMetaData(cloneMe);
            GExpr = new Expr(this, cloneMe.GExpr.Expression, "Integer");
            OExpr = new Expr(this, cloneMe.OExpr.Expression, "Integer");
            EExpr = new Expr(this, cloneMe.EExpr.Expression, "Integer");
        }

        public override object Clone() {
            var clone = new TakeExposure(this) {
                ExposureCount = 0,
                Binning = Binning,
                ImageType = ImageType,
            };

            if (clone.Binning == null) {
                clone.Binning = new BinningMode(1, 1);
            }

            return clone;
        }

        [JsonProperty]
        public Expr EExpr { get; set; }
        [JsonProperty]
        public Expr GExpr { get; set; }
        [JsonProperty]
        public Expr OExpr { get; set; }



        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }


        [JsonProperty]
        public string ExposureTimeExpr {
            get => null;
            set {
                EExpr.Expression = value;
            }
        }

        [JsonProperty]
        public string GainExpr {
            get => null;
            set {
                GExpr.Expression = value;
            }
        }

 
        [JsonProperty]
        public string OffsetExpr {
            get => null;
            set {
                OExpr.Expression = value;
            }
        }

        public string ValidateOffset(double offset) {
            return iValidateOffset(offset, new List<string>());
        }

        public string iValidateOffset(double gain, List<string> i) {
            var iCount = i.Count;

            CameraInfo = this.cameraMediator.GetInfo();
            if (!CameraInfo.Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            } else if (OExpr.Value < 0) {
                i.Add("Offset cannot be less than 0");
            } else if (CameraInfo.CanSetOffset && OExpr.Value > -1 && (OExpr.Value < CameraInfo.OffsetMin | OExpr.Value > CameraInfo.OffsetMax)) {
                i.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_Offset"], CameraInfo.OffsetMin, CameraInfo.OffsetMax, OExpr.Value));
            }

            if (iCount == i.Count) {
                return String.Empty;
            } else {
                return i[iCount];
            }
        }

        private BinningMode binning;

        [JsonProperty]
        public BinningMode Binning { get => binning; set { binning = value; RaisePropertyChanged(); } }

        private string imageType;

        [JsonProperty]
        public string ImageType { get => imageType; set { imageType = value; RaisePropertyChanged(); } }

        private int exposureCount;

        [JsonProperty]
        public int ExposureCount { get => exposureCount; set { exposureCount = value; RaisePropertyChanged(); } }

        private CameraInfo cameraInfo;

        public CameraInfo CameraInfo {
            get => cameraInfo;
            private set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<string> _imageTypes;

        public ObservableCollection<string> ImageTypes {
            get {
                if (_imageTypes == null) {
                    _imageTypes = new ObservableCollection<string>();

                    System.Type type = typeof(CaptureSequence.ImageTypes);
                    foreach (var p in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)) {
                        var v = p.GetValue(null);
                        _imageTypes.Add(v.ToString());
                    }
                }
                return _imageTypes;
            }
            set {
                _imageTypes = value;
                RaisePropertyChanged();
            }
        }

        private bool HandlerInit = false;

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
           var count = ExposureCount;
           var dsoContainer = RetrieveTarget(this.Parent);
           var specificDSOContainer = dsoContainer as DeepSkyObjectContainer;
           if (specificDSOContainer != null) {                
               count = specificDSOContainer.GetOrCreateExposureCountForItemAndCurrentFilter(this, 1)?.Count ?? ExposureCount;
           }
           var capture = new CaptureSequence() {
                ExposureTime = EExpr.Value,
                Binning = Binning,
                Gain = (int)GExpr.Value,
                Offset = (int)OExpr.Value,
                ImageType = ImageType,
                ProgressExposureCount = count,
                TotalExposureCount = count + 1,
            };

            
            var exposureData = await imagingMediator.CaptureImage(capture, token, progress);

            if (!HandlerInit) {
                imagingMediator.ImagePrepared += ProcessResults;
                HandlerInit = true;
            }

            var imageParams = new PrepareImageParameters(null, false);
            if (IsLightSequence()) {
                imageHistoryVM.Add(exposureData.MetaData.Image.Id, ImageType);
            }

            if (imageProcessingTask != null) {
                await imageProcessingTask;
            }
            imageProcessingTask = ProcessImageData(dsoContainer, exposureData, progress, token);

            if (specificDSOContainer != null) {
                specificDSOContainer.IncrementExposureCountForItemAndCurrentFilter(this, 1);
            }
            ExposureCount++;
        }

        private async Task ProcessImageData(IDeepSkyObjectContainer dsoContainer, IExposureData exposureData, IProgress<ApplicationStatus> progress, CancellationToken token) {
            try {
                var imageParams = new PrepareImageParameters(null, false);
                if (IsLightSequence()) {
                    imageParams = new PrepareImageParameters(true, true);
                }

                var imageData = await exposureData.ToImageData(progress, token);

                var prepareTask = imagingMediator.PrepareImage(imageData, imageParams, token);

                if (IsLightSequence()) {
                    imageHistoryVM.PopulateStatistics(imageData.MetaData.Image.Id, await imageData.Statistics);
                }

                if (dsoContainer != null) {
                    var target = dsoContainer.Target;
                    if (target != null) {
                        imageData.MetaData.Target.Name = target.DeepSkyObject.NameAsAscii;
                        imageData.MetaData.Target.Coordinates = target.InputCoordinates.Coordinates;
                        imageData.MetaData.Target.PositionAngle = target.PositionAngle;
                    }
                }

                ISequenceContainer parent = Parent;
                while (parent != null && !(parent is SequenceRootContainer)) {
                    parent = parent.Parent;
                }
                if (parent is SequenceRootContainer item) {
                    imageData.MetaData.Sequence.Title = item.SequenceTitle;
                }

                await imageSaveMediator.Enqueue(imageData, prepareTask, progress, token);

            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private bool IsLightSequence() {
            return ImageType == CaptureSequence.ImageTypes.SNAPSHOT || ImageType == CaptureSequence.ImageTypes.LIGHT;
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Validate();
        }

        private IDeepSkyObjectContainer RetrieveTarget(ISequenceContainer parent) {
            if (parent != null) {
                var container = parent as IDeepSkyObjectContainer;
                if (container != null) {
                    return container;
                } else {
                    return RetrieveTarget(parent.Parent);
                }
            } else {
                return null;
            }
        }

        static Object LastImageLock = new Object();

        static Symbol.Keys iLastImageResult;
        public static Symbol.Keys LastImageResults {
            get {
                lock (LastImageLock) {
                    return iLastImageResult;
                }
            }
            set {
                iLastImageResult = value;
            }
        }

        public double ExposureTime { get => EExpr.Value; set { } }
        public int Gain { get => (int)GExpr.Value; set { } }
        public int Offset { get => (int)OExpr.Value; set { } }

        private void AddOptionalResult(Symbol.Keys results, StarDetectionAnalysis a, string name) {
            if (a.HasProperty(name)) {
                var v = a.GetType().GetProperty(name).GetValue(a, null);
                if (v is double vDouble) {
                    results.Add(name, Math.Round(vDouble, 2));
                }
            }
        }

        private void ProcessResults(object sender, ImagePreparedEventArgs e) {
            lock (LastImageLock) {
                StarDetectionAnalysis a = (StarDetectionAnalysis)e.RenderedImage.RawImageData.StarDetectionAnalysis;

                // Clean out any old results since this instruction may be called many times
                Symbol.Keys results = new Symbol.Keys();

                // These are from AF or HocusFocus
                results.Add("HFR", Math.Round(a.HFR, 3));
                results.Add("DetectedStars", a.DetectedStars);

                // Add these if they exist
                AddOptionalResult(results, a, "Eccentricity");
                AddOptionalResult(results, a, "FWHM");

                // We should also get guider info as well...

                //foreach (var header in e.RenderedImage.RawImageData.MetaData.GenericHeaders) {
                //    IGenericMetaDataHeader h = header as IGenericMetaDataHeader;
                //    if (h != null) {
                //        string key = h.Key;
                //        try {
                //            if (h is StringMetaDataHeader) { // int double bool DateTime
                //                Results.Add(key, ((StringMetaDataHeader)h).Value);
                //            } else if (h is IntMetaDataHeader) {
                //                Results.Add(key, ((IntMetaDataHeader)h).Value);
                //            } else if (h is DoubleMetaDataHeader) {
                //                Results.Add(key, ((DoubleMetaDataHeader)h).Value);
                //            } else if (h is BoolMetaDataHeader) {
                //                Results.Add(key, ((BoolMetaDataHeader)h).Value);
                //            } else if (h is DateTimeMetaDataHeader) {
                //                Results.Add(key, ((DateTimeMetaDataHeader)h).Value);
                //            }
                //        } catch (Exception ex) {
                //            Console.WriteLine(ex.ToString());
                //        }
                //    }
                //}
                LastImageResults = results;
            }
        }

        public string ValidateGain(double gain) {
            return iValidateGain(gain, new List<string>());
        }

        public string iValidateGain(double gain, List<string> i) {
            var iCount = i.Count;

            CameraInfo = this.cameraMediator.GetInfo();
            if (!CameraInfo.Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            } else if (GExpr.Value < -1) {
                i.Add("Gain cannot be less than -1");
            } else if (CameraInfo.CanSetGain && GExpr.Value > -1 && (GExpr.Value < CameraInfo.GainMin || GExpr.Value > CameraInfo.GainMax)) {
                i.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_Gain"], CameraInfo.GainMin, CameraInfo.GainMax, GExpr.Value));
            }

            //Logger.Info("** Temp setting: " + profileService.ActiveProfile.CameraSettings.Temperature);

            if (iCount == i.Count) {
                return String.Empty;
            } else {
                return i[iCount];
            }
        }

        public bool Validate() {
            var i = new List<string>();
            CameraInfo = this.cameraMediator.GetInfo();
            if (!CameraInfo.Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            } else {
                if (CameraInfo.CanSetGain && GExpr.Value > -1 && (GExpr.Value < CameraInfo.GainMin || GExpr.Value > CameraInfo.GainMax)) {
                    i.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_Gain"], CameraInfo.GainMin, CameraInfo.GainMax, GExpr.Value));
                }
                if (CameraInfo.CanSetOffset && OExpr.Value > -1 && (OExpr.Value < CameraInfo.OffsetMin || OExpr.Value > CameraInfo.OffsetMax)) {
                    i.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_Offset"], CameraInfo.OffsetMin, CameraInfo.OffsetMax, OExpr.Value));
                }
                if (EExpr.Expression?.Length == 0) {
                    i.Add("There must be an exposure time set");
                }
            }

            var fileSettings = profileService.ActiveProfile.ImageFileSettings;

            if (string.IsNullOrWhiteSpace(fileSettings.FilePath)) {
                i.Add(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_FilePathEmpty"]);
            } else if (!Directory.Exists(fileSettings.FilePath)) {
                i.Add(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_FilePathInvalid"]);
            }

            if (GExpr.Default != CameraInfo.DefaultGain) {
                GExpr.Default = CameraInfo.DefaultGain;
                GExpr.Value = GExpr.Default;
            }

            if (OExpr.Default != CameraInfo.DefaultOffset) {
                OExpr.Default = CameraInfo.DefaultOffset;
            }

            GExpr.Validate();
            OExpr.Validate();
            EExpr.Validate();

            if (Parent != null && !Symbol.IsAttachedToRoot(Parent)) {
                Logger.Info("Foo");
            }

            Issues = i;
            return i.Count == 0;
        }

        public override void ResetProgress() {
            base.ResetProgress();
            LastImageResults?.Clear();
        }

        public override TimeSpan GetEstimatedDuration() {
            return TimeSpan.FromSeconds(this.EExpr.Value);
        }

        public override string ToString() {
            var currentGain = GExpr.Value == -1 ? CameraInfo.DefaultGain : GExpr.Value;
            var currentOffset = OExpr.Value == -1 ? CameraInfo.DefaultOffset : OExpr.Value;
            return $"Category: {Category}, Item: {nameof(TakeExposure)}, ExposureTime {EExpr.Value}, Gain {currentGain}, Offset {currentOffset}, ImageType {ImageType}, Binning {Binning?.Name ?? "1x1"}";
        }
    }
}