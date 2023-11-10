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
using NINA.Astrometry;
using NINA.Equipment.Equipment.MyCamera;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using System.Windows.Media;
using Accord;
using NINA.Image.Interfaces;
using NINA.Image.ImageData;
using Namotion.Reflection;
using NINA.Core.Utility.Notification;
using System.Windows.Media.Converters;
using Nikon;
using Castle.Core.Internal;
using System.Runtime.CompilerServices;
using NINA.Profile;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using Google.Protobuf.WellKnownTypes;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Take Exposure +")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Imaging_TakeExposure_Description")]
    [ExportMetadata("Icon", "CameraSVG")]
    [ExportMetadata("Category", "Constants Enhanced")]
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
            Gain = -1;
            Offset = -1;
            ImageType = CaptureSequence.ImageTypes.LIGHT;
            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistoryVM = imageHistoryVM;
            this.profileService = profileService;
            CameraInfo = this.cameraMediator.GetInfo();
        }

        private TakeExposure(TakeExposure cloneMe) : this(cloneMe.profileService, cloneMe.cameraMediator, cloneMe.imagingMediator, cloneMe.imageSaveMediator, cloneMe.imageHistoryVM) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            var clone = new TakeExposure(this) {
                ExposureTime = ExposureTime,
                ExposureTimeExpr = ExposureTimeExpr,
                ExposureCount = 0,
                Binning = Binning,
                GainExpr = GainExpr,
                Gain = Gain,
                Offset = Offset,
                ImageType = ImageType,
            };

            if (clone.Binning == null) {
                clone.Binning = new BinningMode(1, 1);
            }

            return clone;
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        private string exposureTimeExpr = "0";

        [JsonProperty]
        public string ExposureTimeExpr {
            get => exposureTimeExpr;
            set {
                exposureTimeExpr = value;
                ConstantExpression.Evaluate(this, "ExposureTimeExpr", "ExposureTime", 0);
                RaisePropertyChanged("ExposureTimeExpr");
            }
        }

        private double exposureTime;

        [JsonProperty]
        public double ExposureTime { get => exposureTime; set { exposureTime = value; RaisePropertyChanged("ExposureTime"); } }


        private string gainExpr = "";
        [JsonProperty]
        public string GainExpr {
            get => gainExpr;
            set {
                gainExpr = value;
                ConstantExpression.Evaluate(this, "GainExpr", "Gain", CameraInfo.DefaultGain);
                RaisePropertyChanged("GainExpr");
            }
        }

        private int gain = -1;

        [JsonProperty]
        public int Gain { get => gain;
            set {
                gain = value;
                RaisePropertyChanged();
            }
        }

        private int offset;

        [JsonProperty]
        public int Offset { get => offset; set { offset = value; RaisePropertyChanged(); } }

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
                ExposureTime = ExposureTime,
                Binning = Binning,
                Gain = Gain,
                Offset = Offset,
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

        static ConstantExpression.Keys iLastImageResult;
        public static ConstantExpression.Keys LastImageResults {
            get {
                lock (LastImageLock) {
                    return iLastImageResult;
                }
            }
            set {
                iLastImageResult = value;
            }
        }

        private void AddOptionalResult(ConstantExpression.Keys results, StarDetectionAnalysis a, string name) {
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
                ConstantExpression.Keys results = new ConstantExpression.Keys();

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
            } else if (Gain < -1) {
                i.Add("Gain cannot be less than -1");
            } else if (CameraInfo.CanSetGain && Gain > -1 && (Gain < CameraInfo.GainMin || Gain > CameraInfo.GainMax)) {
                i.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_Gain"], CameraInfo.GainMin, CameraInfo.GainMax, Gain));
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
                if (CameraInfo.CanSetGain && Gain > -1 && (Gain < CameraInfo.GainMin || Gain > CameraInfo.GainMax)) {
                    i.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_Gain"], CameraInfo.GainMin, CameraInfo.GainMax, Gain));
                }
                if (CameraInfo.CanSetOffset && Offset > -1 && (Offset < CameraInfo.OffsetMin || Offset > CameraInfo.OffsetMax)) {
                    i.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_Offset"], CameraInfo.OffsetMin, CameraInfo.OffsetMax, Offset));
                }
            }

            var fileSettings = profileService.ActiveProfile.ImageFileSettings;

            if (string.IsNullOrWhiteSpace(fileSettings.FilePath)) {
                i.Add(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_FilePathEmpty"]);
            } else if (!Directory.Exists(fileSettings.FilePath)) {
                i.Add(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_FilePathInvalid"]);
            }

            ConstantExpression.Evaluate(this, "ExposureTimeExpr", "ExposureTime", 0, i);
            ConstantExpression.Evaluate(this, "GainExpr", "Gain", -1, i);

            RaisePropertyChanged("ExposureTimeExpr");
            Issues = i;
            return i.Count == 0;
        }

        public override void ResetProgress() {
            base.ResetProgress();
            LastImageResults?.Clear();
        }

        public override TimeSpan GetEstimatedDuration() {
            return TimeSpan.FromSeconds(this.ExposureTime);
        }

        public override string ToString() {
            var currentGain = Gain == -1 ? CameraInfo.DefaultGain : Gain;
            var currentOffset = Offset == -1 ? CameraInfo.DefaultOffset : Offset;
            return $"Category: {Category}, Item: {nameof(TakeExposure)}, ExposureTime {ExposureTime}, Gain {currentGain}, Offset {currentOffset}, ImageType {ImageType}, Binning {Binning?.Name ?? "1x1"}";
        }
    }
}