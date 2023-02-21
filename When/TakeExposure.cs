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

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Take Exposure +")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Imaging_TakeExposure_Description")]
    [ExportMetadata("Icon", "CameraSVG")]
    [ExportMetadata("Category", "Constants Enhanced")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TakeExposure : SequenceItem, IExposureItem, IValidatable, IInstructionResults {
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IImageSaveMediator imageSaveMediator;
        private IImageHistoryVM imageHistoryVM;
        private IProfileService profileService;

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
            Results = new InstructionResult();
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
                Results = new InstructionResult()
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


        private string gainExpr = "0";
        [JsonProperty]
        public string GainExpr {
            get => gainExpr;
            set {
                gainExpr = value;
                ConstantExpression.Evaluate(this, "GainExpr", "Gain", CameraInfo.DefaultGain);
                RaisePropertyChanged("GainExpr");
            }
        }

        private int gain = 0;

        [JsonProperty]
        public int Gain { get => gain; 
            set {
                gain = value; 
                RaisePropertyChanged();
            } 
        }

        public string ValidateGain (double gain) {
            return (gain < -1 || gain > 1000) ? BAD_GAIN : string.Empty;
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

        public InstructionResult Results { get; set; }

        private ObservableCollection<string> _imageTypes;

        public ObservableCollection<string> ImageTypes {
            get {
                if (_imageTypes == null) {
                    _imageTypes = new ObservableCollection<string>();

                    Type type = typeof(CaptureSequence.ImageTypes);
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

        public InstructionResult GetResults () {
            Object value;
            while (!Results.TryGetValue("_READY_", out value)) {
                Thread.Sleep(100);
            }
            while (!Results.TryGetValue("_ImageUri_", out value)) {
                Thread.Sleep(100);
            }
            return Results;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var capture = new CaptureSequence() {
                ExposureTime = ExposureTime,
                Binning = Binning,
                Gain = Gain,
                Offset = Offset,
                ImageType = ImageType,
                ProgressExposureCount = ExposureCount,
                TotalExposureCount = ExposureCount + 1,
            };

            Results.Clear();

            if (!handlerInit) {
                imagingMediator.ImagePrepared += ProcessResults;
                imageSaveMediator.ImageSaved += ImageSaved;
                handlerInit = true;
            }
 
            var imageParams = new PrepareImageParameters(null, false);
            if (IsLightSequence()) {
                imageParams = new PrepareImageParameters(true, true);
            }

            var target = RetrieveTarget(this.Parent);

            var exposureData = await imagingMediator.CaptureImage(capture, token, progress);

            var imageData = await exposureData.ToImageData(progress, token);

            var prepareTask = imagingMediator.PrepareImage(imageData, imageParams, token);

            if (target != null) {
                imageData.MetaData.Target.Name = target.DeepSkyObject.Name; /// NameAsAscii;
                imageData.MetaData.Target.Coordinates = target.InputCoordinates.Coordinates;
                imageData.MetaData.Target.PositionAngle = target.PositionAngle;
            }

            ISequenceContainer parent = Parent;
            while (parent != null && !(parent is SequenceRootContainer)) {
                parent = parent.Parent;
            }
            if (parent is SequenceRootContainer item) {
                imageData.MetaData.Sequence.Title = item.SequenceTitle;
            }

            await imageSaveMediator.Enqueue(imageData, prepareTask, progress, token);

            if (IsLightSequence()) {
                imageHistoryVM.Add(imageData.MetaData.Image.Id, await imageData.Statistics, ImageType);
            }

            ExposureCount++;

        }

        private bool handlerInit = false;

        private void BeforeFinalizeImageSaved(object sender, BeforeFinalizeImageSavedEventArgs e, Task t) {

        }

        private void ImageSaved(object sender, ImageSavedEventArgs e) {
            Uri fileName = e.PathToImage;
            try { 
                Results.Add("_ImageUri_", fileName);
            } catch (Exception ex) {
                return;
            }

        }

        public void AddResult(object src, string propName) {
            if (src.HasProperty(propName)) {
                Results.Add(propName, src.GetType().GetProperty(propName).GetValue(src, null));
            }
        }
        
        private void ProcessResults(object sender, ImagePreparedEventArgs e) {
            StarDetectionAnalysis a = (StarDetectionAnalysis)e.RenderedImage.RawImageData.StarDetectionAnalysis;

            // Clean out any old results since this instruction may be called many times
            Results.Clear();
            
            // These are from AF or HocusFocus
            Results.Add("HFR", Math.Round(a.HFR, 3));
            Results.Add("DetectedStars", a.DetectedStars);
            // Add these if they exist
            AddResult(a, "Eccentricity");
            AddResult(a, "EccentricityMAD");
            AddResult(a, "FWHM");
            AddResult(a, "FWHMMAD");  
            // We should also get guider info as well...

            foreach (var header in e.RenderedImage.RawImageData.MetaData.GenericHeaders) {
                IGenericMetaDataHeader h = header as IGenericMetaDataHeader;
                if (h != null) {
                    string key = h.Key;
                    try {
                        if (h is StringMetaDataHeader) { // int double bool DateTime
                            Results.Add(key, ((StringMetaDataHeader)h).Value);
                        } else if (h is IntMetaDataHeader) {
                            Results.Add(key, ((IntMetaDataHeader)h).Value);
                        } else if (h is DoubleMetaDataHeader) {
                            Results.Add(key, ((DoubleMetaDataHeader)h).Value);
                        } else if (h is BoolMetaDataHeader) {
                            Results.Add(key, ((BoolMetaDataHeader)h).Value);
                        } else if (h is DateTimeMetaDataHeader) {
                            Results.Add(key, ((DateTimeMetaDataHeader)h).Value);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
            Results.Add("_READY_", true);
        }

        private bool IsLightSequence() {
            return ImageType == CaptureSequence.ImageTypes.SNAPSHOT || ImageType == CaptureSequence.ImageTypes.LIGHT;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        private InputTarget RetrieveTarget(ISequenceContainer parent) {
            if (parent != null) {
                var container = parent as IDeepSkyObjectContainer;
                if (container != null) {
                    return container.Target;
                } else {
                    return RetrieveTarget(parent.Parent);
                }
            } else {
                return null;
            }
        }

        private static string BAD_GAIN = "Gain must be between -1 and 1000";
  
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

            if (ValidateGain(Gain) != String.Empty) {
                i.Add(BAD_GAIN);
            }

            RaisePropertyChanged("ExposureTimeExpr");
            Issues = i;
            return i.Count == 0;
        }

        public override void ResetProgress() {
            base.ResetProgress();
            Results.Clear();
        }

        public override TimeSpan GetEstimatedDuration() {
            return TimeSpan.FromSeconds(this.ExposureTime);
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(TakeExposure)}, ExposureTime {ExposureTime}, Gain {Gain}, Offset {Offset}, ImageType {ImageType}, Binning {Binning?.Name}";
        }
    }
}