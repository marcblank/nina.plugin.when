#region "copyright"

/*
    Copyright © 2021-2024 Marc Blank <marc.blank@live.com> & Associates

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WhenPlugin.When {
    [Serializable()]
    public class CustomObstruction {
        private double[] declinations;
        private double[] westObstructions;
        private double[] eastObstructions;
        
        /// <summary>
        /// This class represents an obstruction profile around the meridian. 
        /// </summary>

        public CustomObstruction() {
            declinations = new double[2];
            westObstructions = new double[2];
            eastObstructions = new double[2];
            declinations[0] = 0;
            declinations[1] = 180;
            westObstructions[0] = 0;
            westObstructions[1] = 0;
            eastObstructions[0] = 0;
            eastObstructions[1] = 0;
        }
        public CustomObstruction(IDictionary<double, double[]> obstructionMap) {
            this.declinations = obstructionMap.Keys.ToArray();
            var obstructions = obstructionMap.Values;
            var westList = new List<double>();
            var eastList = new List<double>();
            foreach (double[] obstruction in obstructions) {
                westList.Add(obstruction[0]);
                eastList.Add(obstruction[1]);
            }
            this.westObstructions = westList.ToArray();
            this.eastObstructions = eastList.ToArray();
        }

        public double GetEastObstruction(double declination) {
           // if (declination < -90 || declination > 90) { declination = Utility.Utility.EuclidianModulus(declination, 360); }
            return Accord.Math.Tools.Interpolate1D(declination, declinations, eastObstructions, 0, 0);
        }
        public double GetWestObstruction(double declination) {
            // if (declination < -90 || declination > 90) { declination = Utility.Utility.EuclidianModulus(declination, 360); }
            return Accord.Math.Tools.Interpolate1D(declination, declinations, westObstructions, 0, 0);
        }

        /*
        public double GetMaxAltitude() {
            return this.altitudes.Max();
        }

        public double GetMinAltitude() {
            return this.altitudes.Min();
        }
        */

        public static CustomObstruction FromReader(TextReader sr)
        {
            var obstructionMap = new SortedDictionary<double, double[]>();

            string line;
            while ((line = sr.ReadLine()?.Trim()) != null) {
                // Lines starting with # are comments
                if (line.StartsWith("#")) { continue; }

                var columns = line.Split(' ');
                if (columns.Length == 3) {
                    if (double.TryParse(columns[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var declination)) {
                        if (double.TryParse(columns[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var westObstruction)) {
                            if (double.TryParse(columns[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var eastObstruction)) {
                                double[] obs = new double[2];
                                obs[0] = westObstruction;
                                obs[1] = eastObstruction;
                                obstructionMap[declination] = obs;
                            } else {
                                Logger.Warning($"Invalid value for East obstruction {columns[0]}");
                            }
                        } else {
                            Logger.Warning($"Invalid value for West obstruction {columns[0]}");
                        }
                    } else {
                        Logger.Warning($"Invalid value for declination {columns[0]}");
                    }
                } else {
                    Logger.Warning($"Invalid line for declination values {line}");
                }
            }

            if (obstructionMap.Count < 2)
            {
                throw new ArgumentException("Obstructions file does not contain enough entries or is invalid");
            }
            // Groom Incomplete Data
            // 1) No 0 or 360 Azimuth is detected => Find the nearest datapoint and add it to the list
            // 2) No 0 Azimuth is present but 360 Azimuth is present => Add value from 360 to 0
            // 2) No 360 Azimuth is present but 0 Azimuth is present => Add value from 0 to 360
            if (!obstructionMap.ContainsKey(0))
            {
                var nearestMinusNinety = obstructionMap.Keys.OrderBy(x => Math.Abs(x)).First();
                obstructionMap[0] = obstructionMap[nearestMinusNinety];
            }
            if (!obstructionMap.ContainsKey(180))
            {
                var nearestPlusNinety = obstructionMap.Keys.OrderByDescending(x => Math.Abs(x)).First();
                obstructionMap[180] = obstructionMap[nearestPlusNinety];
            }
            var obstruction = new CustomObstruction(obstructionMap);
            return obstruction;
        }

        public static CustomObstruction FromAPCCReader(TextReader sr, double latitude)
        {
            var obstructionMap = new SortedDictionary<double, double[]>();

            string line;
            while ((line = sr.ReadLine()?.Trim()) != null) {
                // Lines starting with # are comments
                if (line.StartsWith("#")) { continue; }

                var columns = line.Split('#');
                if (columns.Length == 3) {
                    if (double.TryParse(columns[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var meridianAngle)) {
                        if (double.TryParse(columns[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var westObstruction)) {
                            if (double.TryParse(columns[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var eastObstruction)) {
                                double[] obs = new double[2];
                                obs[0] = -westObstruction * 60 + 1;
                                obs[1] = eastObstruction * 60 + 1;
                                var declination = meridianAngle - 90 + latitude;
                                obstructionMap[declination] = obs;
                            } else {
                                Logger.Warning($"Invalid value for East obstruction {columns[0]}");
                            }
                        } else {
                            Logger.Warning($"Invalid value for West obstruction {columns[0]}");
                        }
                    } else {
                        Logger.Warning($"Invalid value for declination {columns[0]}");
                    }
                } else {
                    Logger.Warning($"Invalid line -{line}-");
                }
            }

            if (obstructionMap.Count < 2) {
                throw new ArgumentException("Obstructions file does not contain enough entries or is invalid");
            }

            // Groom Incomplete Data
            // 1) No 0 or 360 Azimuth is detected => Find the nearest datapoint and add it to the list
            // 2) No 0 Azimuth is present but 360 Azimuth is present => Add value from 360 to 0
            // 2) No 360 Azimuth is present but 0 Azimuth is present => Add value from 0 to 360
            if (!obstructionMap.ContainsKey(0)) {
                var nearestMinusNinety = obstructionMap.Keys.OrderBy(x => Math.Abs(x)).First();
                obstructionMap[0] = obstructionMap[nearestMinusNinety];
            }
            if (!obstructionMap.ContainsKey(180)) {
                var nearestPlusNinety = obstructionMap.Keys.OrderByDescending(x => Math.Abs(x)).First();
                obstructionMap[180] = obstructionMap[nearestPlusNinety];
            }
            var obstruction = new CustomObstruction(obstructionMap);
            return obstruction;
        }

        /// <summary>
        /// Creates an instance of the custom obstruction object to calculate obstructions based on a given declination out of a file
        /// The Obstructions file must consist of a list of declination, west-side obstruction and east-side obstructions triplets that 
        /// are separated by a space and line breaks.
        /// West-side and East-side obstructions are in minutes, and they indicate the maximum time before the meridian (West-side)
        /// and minimum time after the meridian (East-side) that it's safe for the telescope to be at, for a given declination.
        /// The West-side obstruction can be a nagative value to indicate how fat the telescope can safely track after the meridian
        /// Since a meridian flip is possible both on the arc of meridian between the Pole and the horizon opposite to the Pole ("above" 
        /// the Pole), and on the arc between the Pole and the horizon next to the Pole ("below" the Pole), is is necessary to "expand"
        /// the declination range used in the file. Values between -90 degrees and 90 degrees will be used to represent the arc of meridian
        /// "above" the Pole, and values between 90 and 180 degrees will be used to represent the arc "below" the Pole. So, 85 degrees 
        /// of celestial declination on the arc from the Pole to the horizon next to it will be represented in the file by 95 degrees (180-85),
        /// 60 degrees of celestial declination will be represented by 120 degrees (180-60), and so on.
        /// A minimum of two points are required. In between triplets, the values are interpolated
        /// Lines starting with '#' character will be treated as comments and therefore ignored
        /// </summary>
        /// <example>
        /// # File Example
        /// # The following line means that, at declination zero, the telescope can track 10 minutes after meridian, but can 
        /// # also safely flip right after the meridian
        /// 0 -10 0
        /// # This line says that at dec. +30 deg the telescope can safely track up t the meridian, and flip right after 
        /// # the target has passed the meridian
        /// 40 0 0
        /// # This line says that at dec. +45 deg the telescope should stop tracking at least 20 minutes before
        /// # the meridian, can can flip no earlier than 30 minutes after the target has crossed the meridian
        /// 45 20 30
        /// # This line says that at dec. +50 deg the telescope can again safely track up to the meridian, and can flip
        /// # right after the target has crossed the meridian
        /// 50 0 0
        /// </example>
        /// <param name="filePath">The file pointing to the horizon file</param>
        /// <returns>An instance of CustomHorizon</returns>
        public static CustomObstruction FromFile(string filePath, double latitude) {
            if (File.Exists(filePath)) {
                using (var fs = File.OpenRead(filePath)) {
                    using (var sr = new StreamReader(fs)) {
                        if (filePath.EndsWith(".mlm")) {
                            return FromAPCCReader(sr, latitude);
                        } else {
                            return FromReader(sr);
                        }
                    }
                }
            } else {
                throw new FileNotFoundException("Obstruction file not found", filePath);
            }
        }
    }
}
