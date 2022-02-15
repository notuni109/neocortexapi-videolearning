using NeoCortexApi;
using NeoCortexApi.Classifiers;
using NeoCortexApi.Entities;
using NeoCortexApi.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VideoLibrary;

namespace HTMVideoLearning
{
    class VideoLearning
    {
        #region Learning with HtmClassifier key as FrameKey
        public static void Run1(StartupConfig startupCfg = null, HtmConfig htmCfg = null)
        {
            Stopwatch sw = new();
            List<TimeSpan> RecordedTime = new();

            RenderHelloScreen();

            string trainingFolderPath = startupCfg?.TrainingDatasetRoot ?? null;

            if (String.IsNullOrEmpty(trainingFolderPath))
                trainingFolderPath = Console.ReadLine();

            sw.Start();

            string outputFolder, convertedVideoDir, testOutputFolder;

            CreateTemporaryFolders(out outputFolder, out convertedVideoDir, out testOutputFolder);

/*            int frameWidth = 18;
            int frameHeight = 18;
            double frameRate = 12;
            ColorMode colorMode = ColorMode.BLACKWHITE;


            // adding condition for 
            // Define HTM parameters
            int[] inputBits = { frameWidth * frameHeight * (int)colorMode };
            int[] numColumns = { 1024 };*/
            //Initiate configuration with frame width, frame height, frame rate, color mode and number of columns
            VideoConfiguration vidConf = new VideoConfiguration(18, 18, 12.0, ColorMode.BLACKWHITE, 1024);

            // Define Reader for Videos
            // Input videos are stored in different folders under TrainingVideos/
            // with their folder's names as label value. To get the paths of all folders:
            string[] videoDatasetRootFolder = GetVideoSetPaths(trainingFolderPath);
            string[] videoSetPaths = GetVideoSetPaths(trainingFolderPath);

            // A list of VideoSet object, each has the Videos and the name of the folder as Label, contains all the Data in TrainingVideos,
            // this List will be the core iterator in later learning and predicting
            List<VideoSet> videoData = new();

            // Iterate through every folder in TrainingVideos/ to create VideoSet: object that stores video of same folder/label
            foreach (string path in videoDatasetRootFolder)
            {
                VideoSet vs = new(path, vidConf.ColorMode, vidConf.FrameWidth, vidConf.FrameHeight, vidConf.FrameRate);
                videoData.Add(vs);
                vs.ExtractFrames(convertedVideoDir);
            }
            //Initiating HTM
            HtmConfig cfg = GetHTMConfig(vidConf.InputBits, vidConf.NumberOfColumns);

            var mem = new Connections(cfg);

            HtmClassifier<string, ComputeCycle> cls = new();

            CortexLayer<object, object> layer1 = new("L1");

            TemporalMemory tm = new();

            bool isInStableState = false;

            bool learn = true;

            int maxNumOfElementsInSequence = videoData[0].GetLongestFramesCountInSet();

            int maxCycles = 10;
            int newbornCycle = 0;

            HomeostaticPlasticityController hpa = new(mem, maxNumOfElementsInSequence * 150 * 3, (isStable, numPatterns, actColAvg, seenInputs) =>
            {
                if (isStable)
                    // Event should be fired when entering the stable state.
                    Debug.WriteLine($"STABLE: Patterns: {numPatterns}, Inputs: {seenInputs}, iteration: {seenInputs / numPatterns}");
                else
                    // Ideal SP should never enter unstable state after stable state.
                    Debug.WriteLine($"INSTABLE: Patterns: {numPatterns}, Inputs: {seenInputs}, iteration: {seenInputs / numPatterns}");

                // We are not learning in instable state.
                learn = isInStableState = isStable;

                // Clear all learned patterns in the classifier.
                cls.ClearState();

            }, numOfCyclesToWaitOnChange: 50);

            SpatialPoolerMT sp = new(hpa);
            sp.Init(mem);
            tm.Init(mem);
            layer1.HtmModules.Add("sp", sp);

            //
            // Training SP to get stable. New-born stage.
            //
            ///*
            //for (int i = 0; i < maxCycles; i++)
            while (isInStableState == false)
            {
                newbornCycle++;

                Console.WriteLine($"-------------- Newborn Cycle {newbornCycle} ---------------");

                foreach (VideoSet set in videoData)
                {
                    // Show Set Label/ Folder Name of each video set
                    WriteLineColor($"VIDEO SET LABEL: {set.VideoSetLabel}", ConsoleColor.Cyan);
                    foreach (NVideo vid in set.nVideoList)
                    {
                        // Show the name of each video
                        WriteLineColor($"    VIDEO NAME: {vid.name}", ConsoleColor.DarkCyan);
                        foreach (NFrame frame in vid.nFrames)
                        {
                            //Console.WriteLine($" -- {frame.FrameKey} --");

                            var lyrOut = layer1.Compute(frame.EncodedBitArray, learn);

                            if (isInStableState)
                                break;


                        }
                    }
                }

                if (isInStableState)
                    break;
            }
            //*/

            layer1.HtmModules.Add("tm", tm);

            // Accuracy Check
            double cycleAccuracy = 0;
            double lastCycleAccuracy = 0;
            int stableAccuracyCount = 0;
            long SP_TrainingTimeElapsed = sw.ElapsedMilliseconds;
            sw.Reset();
            sw.Start();
            for (int i = 0; i < maxCycles; i++)
            {
                List<double> setAccuracy = new();
                WriteLineColor($"------------- Cycle {i} -------------", ConsoleColor.Green);
                // Iterating through every video set
                foreach (VideoSet vs in videoData)
                {
                    List<double> videoAccuracy = new();
                    // Iterating through every video in a VideoSet
                    foreach (NVideo nv in vs.nVideoList)
                    {
                        List<NFrame> trainingVideo = nv.nFrames;
                        learn = true;

                        // Now training with SP+TM. SP is pretrained on the provided training videos.
                        // Learning each frame in a video
                        foreach (var currentFrame in trainingVideo)
                        {
                            Console.WriteLine($"--------------SP+TM {currentFrame.FrameKey} ---------------");

                            // Calculating SDR from the current Frame
                            var lyrOut = layer1.Compute(currentFrame.EncodedBitArray, learn) as ComputeCycle;

                            Console.WriteLine(string.Join(',', lyrOut.ActivColumnIndicies));
                            // lyrOut is null when the TM is added to the layer inside of HPC callback by entering of the stable state.

                            List<Cell> actCells;

                            WriteLineColor($"WinnerCell Count: {lyrOut.WinnerCells.Count}", ConsoleColor.Cyan);
                            WriteLineColor($"ActiveCell Count: {lyrOut.ActiveCells.Count}", ConsoleColor.Cyan);

                            if (lyrOut.ActiveCells.Count == lyrOut.WinnerCells.Count)
                            {
                                actCells = lyrOut.ActiveCells;
                            }
                            else
                            {
                                actCells = lyrOut.WinnerCells;
                            }
                            // Using HTMClassifier to assign the current frame key with the Collumns Indicies array
                            cls.Learn(currentFrame.FrameKey, actCells.ToArray());

                            // Checking Predicted Cells of the current frame
                            // From experiment the number of Predicted cells increase over cycles and reach stability later.
                            if (lyrOut.PredictiveCells.Count > 0)
                            {
                                WriteLineColor("Predicted Values for current frame: ", ConsoleColor.Yellow);

                                // Checking the Predicted Cells by printing them out
                                Cell[] cellArray = lyrOut.PredictiveCells.ToArray();
                                /*
                                foreach (Cell nCell in cellArray)
                                {
                                    WriteLineColor(nCell.ToString(), ConsoleColor.Yellow);

                                }
                                */
                                // HTMClassifier used Predicted Cells to infer learned frame key 
                                var predictedFrames = cls.GetPredictedInputValues(cellArray, 5);
                                WriteLineColor("Predicted next Frame's Label:",ConsoleColor.Yellow);
                                foreach (var item in predictedFrames)
                                {
                                    //Console.WriteLine($"Current Input: {currentFrame.FrameKey} \t| Predicted Input: {item.PredictedInput}");
                                    Console.WriteLine($"{item.PredictedInput} -- similarity{item.Similarity} -- NumberOfSameBit {item.NumOfSameBits}");
                                }
                            }
                            else
                            {
                                // If No Cells is predicted
                                //WriteLineColor($"CURRENT FRAME: {currentFrame.FrameKey}", ConsoleColor.Red);
                                WriteLineColor("NO CELLS PREDICTED  FOR THIS FRAME", ConsoleColor.Red);
                            }
                        }
                        // Inferring Mode
                        // 
                        learn = false;
                        List<List<string>> possibleOutcomeSerie = new();
                        possibleOutcomeSerie.Add(new List<string> { trainingVideo[0].FrameKey });
                        List<string> possibleOutcome = new();

                        foreach (NFrame currentFrame in trainingVideo)
                        {
                            // Inferring the current frame encoded bit array with learned SP
                            var lyrOut = layer1.Compute(currentFrame.EncodedBitArray, learn) as ComputeCycle;
                            var nextFramePossibilities = cls.GetPredictedInputValues(lyrOut.PredictiveCells.ToArray(), 1);
                            foreach (var predictedOutput in nextFramePossibilities)
                            {
                                possibleOutcome.Add(predictedOutput.PredictedInput);
                            }

                            possibleOutcomeSerie.Add(new(possibleOutcome));
                            possibleOutcome.Clear();
                        }

                        int matches = 0;

                        List<string> resultToWrite = new();
                        if(!Directory.Exists(Path.Combine(outputFolder,"ResultLog")))
                        {
                            Directory.CreateDirectory(Path.Combine(outputFolder,"ResultLog"));
                        }
                        string resultFileName = Path.Combine(outputFolder,"ResultLog",$"{nv.label}_{nv.name}_Cycle{i}");

                        for (int j = 0; j < possibleOutcomeSerie.Count - 1; j += 1)
                        {
                            string message = $"Expected : {trainingVideo[j].FrameKey} ||| GOT {string.Join(" --- ", possibleOutcomeSerie[j])}";
                            if (possibleOutcomeSerie[j].Contains(trainingVideo[j].FrameKey))
                            {
                                matches += 1;
                                WriteLineColor(message, ConsoleColor.Green);
                                resultToWrite.Add($"FOUND:   {message}");
                            }
                            else
                            {
                                WriteLineColor(message, ConsoleColor.Gray);
                                resultToWrite.Add($"NOTFOUND {message}");
                            }
                        }
                        double accuracy = matches / ((double)trainingVideo.Count - 1);
                        videoAccuracy.Add(accuracy);

                        if (accuracy > 0.9)
                        {
                            RecordResult(resultToWrite, resultFileName);
                        }
                        resultToWrite.Clear();
                        // Enter training phase again
                        learn = true;
                        tm.Reset(mem);
                    }
                    double currentSetAccuracy = videoAccuracy.Average();
                    WriteLineColor($"Video Set of Label: {vs.VideoSetLabel} reachs accuracy: {currentSetAccuracy * 100}%",ConsoleColor.Cyan);
                    setAccuracy.Add(currentSetAccuracy);
                }
                cycleAccuracy = setAccuracy.Average();
                WriteLineColor($"Accuracy in Cycle {i}: {cycleAccuracy*100}%");
                // Check if accuracy is stable
                if (lastCycleAccuracy == cycleAccuracy)
                {
                    stableAccuracyCount += 1;
                }
                else
                {
                    stableAccuracyCount = 0;
                }
                if (stableAccuracyCount >= 40 && cycleAccuracy > 0.9)
                {
                    List<string> outputLog = new();
                    if (!Directory.Exists(Path.Combine(outputFolder,"TEST")))
                    {
                        Directory.CreateDirectory(Path.Combine(outputFolder,"TEST"));
                    }
                    string fileName = Path.Combine(outputFolder,"TEST","saturatedAccuracyLog_Run1");
                    outputLog.Add($"Result Log for reaching saturated accuracy at cycleAccuracy {cycleAccuracy}");

                    outputLog.Add($"reaching stable after enter newborn cycle {newbornCycle}.");
                    outputLog.Add($"Elapsed time: {SP_TrainingTimeElapsed / 1000 / 60} min.");

                    for (int j = 0; j < videoData.Count; i += 1)
                    {
                        outputLog.Add($"{videoData[j].VideoSetLabel} reach average Accuracy {setAccuracy[j]}");
                    }
                    outputLog.Add($"Stop SP+TM after {i} cycles");
                    outputLog.Add($"Elapsed time: {sw.ElapsedMilliseconds / 1000 / 60} min.");

                    RecordResult(outputLog, fileName);
                    break;
                }
                else if (i == maxCycles - 1)
                {
                    List<string> outputLog = new();
                    if (!Directory.Exists(Path.Combine(outputFolder,"TEST")))
                    {
                        Directory.CreateDirectory(Path.Combine(outputFolder,"TEST"));
                    }
                    string fileName = Path.Combine(outputFolder,"TEST","MaxCycleReached");
                    outputLog.Add($"Result Log for stopping experiment with accuracy at cycleAccuracy {cycleAccuracy}");

                    outputLog.Add($"reaching stable after enter newborn cycle {newbornCycle}.");
                    outputLog.Add($"Elapsed time: {SP_TrainingTimeElapsed / 1000 / 60} min.");

                    for (int j = 0; j < videoData.Count; j += 1)
                    {
                        outputLog.Add($"{videoData[j].VideoSetLabel} reach average Accuracy {setAccuracy[j]}");
                    }
                    outputLog.Add($"Stop SP+TM after {i} cycles");
                    outputLog.Add($"Elapsed time: {sw.ElapsedMilliseconds / 1000 / 60} min.");
                    RecordResult(outputLog, fileName);
                    break;
                }
                lastCycleAccuracy = cycleAccuracy;
            }
            // Testing Section
            string userInput;
            
            WriteLineColor("Drag a Frame(Picture) to recall the learned videos (To Quit Write Q): ", ConsoleColor.Cyan);
            int testNo = 0;

            do
            {
                userInput = "";
                while (userInput == "")
                {
                    userInput = Console.ReadLine().Replace("\"", "");
                }
                if (userInput == "Q")
                {
                    break;
                }
                testNo += 1;
                NFrame inputFrame = new(new Bitmap(userInput), "TEST", "test", 0, vidConf.FrameWidth, vidConf.FrameHeight, vidConf.ColorMode);
                // Computing user input frame with trained layer 
                var lyrOut = layer1.Compute(inputFrame.EncodedBitArray, false) as ComputeCycle;
                var predictedInputValues = cls.GetPredictedInputValues(lyrOut.PredictiveCells.ToArray(), 5);
                foreach (var clsPredictionRes in predictedInputValues)
                {
                    bool nextPredictedFrameExists = true;
                    List<NFrame> frameSequence = new();
                    frameSequence.Add(inputFrame);
                    string predictedFrameKey = clsPredictionRes.PredictedInput;

                    NFrame currentFrame = null;
                    while (nextPredictedFrameExists && frameSequence.Count < 42)
                    {
                        foreach (var vs in videoData)
                        {
                            currentFrame = vs.GetNFrameFromFrameKey(predictedFrameKey);
                            if (currentFrame != null)
                            {
                                frameSequence.Add(currentFrame);
                                break;
                            }
                        }
                        WriteLineColor($"Predicted nextFrame: {predictedFrameKey}", ConsoleColor.Green);

                        var computedSDR = layer1.Compute(currentFrame.EncodedBitArray, false) as ComputeCycle;
                        var predictedNext = cls.GetPredictedInputValues(computedSDR.PredictiveCells.ToArray(), 3);

                        // Check for end of Frame sequence
                        if (predictedNext.Count == 0)
                        {
                            nextPredictedFrameExists = false;
                        }
                        else
                        {
                            predictedFrameKey = predictedNext[0].PredictedInput;
                        }
                    }
                    string dir = Path.Combine(testOutputFolder,$"Predicted from {Path.GetFileNameWithoutExtension(userInput)}");

                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    NVideo.CreateVideoFromFrames(
                        frameSequence,
                        Path.Combine(dir,$"testNo_{testNo}_FirstPossibility_{clsPredictionRes.Similarity}_FirstLabel_{clsPredictionRes.PredictedInput}"),
                        (int)(videoData[0].nVideoList[0].frameRate),
                        new Size((int)vidConf.FrameWidth, (int)vidConf.FrameHeight),
                        true);
                    if(nextPredictedFrameExists == false)
                    {
                        WriteLineColor("Drag a Frame(Picture) to recall the learned videos (To Quit Write Q): ", ConsoleColor.Cyan);
                    }
                }
            }
            while (userInput != "Q");
        }
        #endregion

        #region Learning with HTMClassifier key as Series of FrameKey (Sequence Learning)
        public static void Run2(StartupConfig startupCfg = null, HtmConfig htmCfg = null)
        {
            RenderHelloScreen();

            //initiate time capture
            Stopwatch sw = new();
            List<TimeSpan> RecordedTime = new();

            string trainingFolderPath = startupCfg?.TrainingDatasetRoot ?? null;

            if (String.IsNullOrEmpty(trainingFolderPath))
            {
                WriteLineColor("training Dataset path not detectected in startupConfig.json", ConsoleColor.Blue);
                WriteLineColor("Please drag the folder that contains the training files to the Console Window: ", ConsoleColor.Blue);
                WriteLineColor("sample set SmallTrainingSet/ is located in root directory", ConsoleColor.Blue);
                trainingFolderPath = Console.ReadLine();
            }

            // Starting experiment
            sw.Start();

            // Output folder initiation
            string outputFolder = "Run2ExperimentOutput";
            string convertedVideoDir = Path.Combine(outputFolder,"Converted");
            if (!Directory.Exists($"{convertedVideoDir}"))
            {
                Directory.CreateDirectory($"{convertedVideoDir}");
            }

            // Video Parameter 
           /*int frameWidth = 18;
            int frameHeight = 18;
            ColorMode colorMode = ColorMode.BLACKWHITE;
            double frameRate = 10;*/  
            //Initiate configuration
            VideoConfiguration vidConf2 = new VideoConfiguration(18, 18, 10.0, ColorMode.BLACKWHITE, 1024);
            
            // Define Reader for Videos
            // Input videos are stored in different folders under TrainingVideos/
            // with their folder's names as label value. To get the paths of all folders:
            string[] videoSetDirectories = GetVideoSetPaths(trainingFolderPath);

            // A list of VideoSet object, each has the Videos and the name of the folder as Label, contains all the Data in TrainingVideos,
            // this List will be the core iterator in later learning and predicting
            List<VideoSet> videoData = new();

            // Iterate through every folder in TrainingVideos/ to create VideoSet: object that stores video of same folder/label
            foreach (string path in videoSetDirectories)
            {
                VideoSet vs = new(path, vidConf2.ColorMode, vidConf2.FrameWidth, vidConf2.FrameHeight, vidConf2.FrameRate);
                videoData.Add(vs);
                // Output converted Videos to Output/Converted/
                vs.ExtractFrames(convertedVideoDir);
            }

            // Define HTM parameters

            //Initiating HTM
            HtmConfig cfg = GetHTMConfig(vidConf2.InputBits, vidConf2.NumberOfColumns);

            var mem = new Connections(cfg);

            HtmClassifier<string, ComputeCycle> cls = new();

            CortexLayer<object, object> layer1 = new("L1");

            TemporalMemory tm = new();

            bool isInStableState = false;

            bool learn = true;

            int maxCycles = 10;
            int newbornCycle = 0;

            HomeostaticPlasticityController hpa = new(mem, 30 * 150 * 3, (isStable, numPatterns, actColAvg, seenInputs) =>
              {
                  if (isStable)
                    // Event should be fired when entering the stable state.
                    Console.WriteLine($"STABLE: Patterns: {numPatterns}, Inputs: {seenInputs}, iteration: {seenInputs / numPatterns}");
                  else
                    // Ideal SP should never enter unstable state after stable state.
                    Console.WriteLine($"INSTABLE: Patterns: {numPatterns}, Inputs: {seenInputs}, iteration: {seenInputs / numPatterns}");

                // We are not learning in instable state.
                learn = isInStableState = isStable;

                // Clear all learned patterns in the classifier.
                //cls.ClearState();

            }, numOfCyclesToWaitOnChange: 50);

            SpatialPoolerMT sp = new(hpa);
            sp.Init(mem);
            tm.Init(mem);
            layer1.HtmModules.Add("sp", sp);

            //
            // Training SP to get stable. New-born stage.
            //
            ///*
            ///normally change it to while, only for less working time use the for loop
            for (int i = 0; i < maxCycles; i++)
            /*while (isInStableState == false)*/
            {
                newbornCycle++;
                Console.WriteLine($"-------------- Newborn Cycle {newbornCycle} ---------------");
                foreach (VideoSet set in videoData)
                {
                    // Show Set Label/ Folder Name of each video set
                    WriteLineColor($"VIDEO SET LABEL: {set.VideoSetLabel}", ConsoleColor.Cyan);
                    foreach (NVideo vid in set.nVideoList)
                    {
                        Console.WriteLine($" Video: {vid.name}.");
                        // Name of the Video That is being trained 
                        WriteLineColor($"    VIDEO NAME: {vid.name}", ConsoleColor.DarkCyan);
                        foreach (NFrame frame in vid.nFrames)
                        {
                            Console.Write(".");
                            var lyrOut = layer1.Compute(frame.EncodedBitArray, learn);
                            if (isInStableState)
                                break;
                        }
                        Console.WriteLine();
                    }
                }

                if (isInStableState)
                    break;
            }
            //*/

            layer1.HtmModules.Add("tm", tm);
            List<int[]> stableAreas = new();

            int cycle = 0;
            int matches = 0;

            List<string> lastPredictedValue = new();

            foreach (VideoSet vd in videoData)
            {
                foreach (NVideo nv in vd.nVideoList)
                {
                    int maxPrevInputs = nv.nFrames.Count - 1;
                    List<string> previousInputs = new();
                    cycle = 0;
                    learn = true;

                    sw.Reset();
                    sw.Start();
                    /*int maxMatchCnt = 0;*/
                    //
                    // Now training with SP+TM. SP is pretrained on the given VideoSet.
                    // There is a little difference between a input pattern set and an input video set,
                    // The reason is because a video consists of continously altering frame, not distinct values like the sequence learning of Scalar value.
                    // Thus Learning with sp alone was kept
                    double lastCycleAccuracy = 0;
                    int saturatedAccuracyCount = 0;
                    bool isCompletedSuccessfully = false;

                    for (int i = 0; i < maxCycles; i++)
                    {
                        matches = 0;
                        cycle++;

                        Console.WriteLine($"-------------- Cycle {cycle} ---------------");

                        foreach (var currentFrame in nv.nFrames)
                        {
                            Console.WriteLine($"-------------- {currentFrame.FrameKey} ---------------");
                            var lyrOut = layer1.Compute(currentFrame.EncodedBitArray, learn) as ComputeCycle;

                            Console.WriteLine(string.Join(',', lyrOut.ActivColumnIndicies));
                            // lyrOut is null when the TM is added to the layer inside of HPC callback by entering of the stable state.

                            previousInputs.Add(currentFrame.FrameKey);
                            if (previousInputs.Count > (maxPrevInputs + 1))
                                previousInputs.RemoveAt(0);

                            // In the pretrained SP with HPC, the TM will quickly learn cells for patterns
                            // In that case the starting sequence 4-5-6 might have the sam SDR as 1-2-3-4-5-6,
                            // Which will result in returning of 4-5-6 instead of 1-2-3-4-5-6.
                            // HtmClassifier allways return the first matching sequence. Because 4-5-6 will be as first
                            // memorized, it will match as the first one.
                            if (previousInputs.Count < maxPrevInputs)
                                continue;

                            string key = GetKey(previousInputs);
                            List<Cell> actCells;

                            WriteLineColor($"WinnerCell Count: {lyrOut.WinnerCells.Count}", ConsoleColor.Cyan);
                            WriteLineColor($"ActiveCell Count: {lyrOut.ActiveCells.Count}", ConsoleColor.Cyan);

                            if (lyrOut.ActiveCells.Count == lyrOut.WinnerCells.Count)
                            {
                                actCells = lyrOut.ActiveCells;
                            }
                            else
                            {
                                actCells = lyrOut.WinnerCells;
                            }

                            // Remember the key with corresponding SDR
                            WriteLineColor($"Current learning Key: {key}", ConsoleColor.Magenta);
                            cls.Learn(key, actCells.ToArray());

                            if (learn == false)
                                Console.WriteLine($"Inference mode");

                            Console.WriteLine($"Col  SDR: {Helpers.StringifyVector(lyrOut.ActivColumnIndicies)}");
                            Console.WriteLine($"Cell SDR: {Helpers.StringifyVector(actCells.Select(c => c.Index).ToArray())}");

                            if (lastPredictedValue.Contains(key))
                            {
                                matches++;
                                Console.WriteLine($"Match. Actual value: {key} - Predicted value: {key}");
                                lastPredictedValue.Clear();
                            }
                            else
                            {
                                Console.WriteLine($"Mismatch! Actual value: {key} - Predicted values: {String.Join(',', lastPredictedValue)}");
                                lastPredictedValue.Clear();
                            }

                            if (lyrOut.PredictiveCells.Count > 0)
                            {
                                //var predictedInputValue = cls.GetPredictedInputValue(lyrOut.PredictiveCells.ToArray());
                                var predictedInputValues = cls.GetPredictedInputValues(lyrOut.PredictiveCells.ToArray(), 3);

                                foreach (var item in predictedInputValues)
                                {
                                    Console.WriteLine($"Current Input: {currentFrame.FrameKey} \t| Predicted Input: {item.PredictedInput}");
                                    lastPredictedValue.Add(item.PredictedInput);
                                }
                            }
                            else
                            {
                                Console.WriteLine($"NO CELLS PREDICTED for next cycle.");
                                lastPredictedValue.Clear();
                            }
                        }

                        double accuracy;

                        accuracy = (double)matches / ((double)nv.nFrames.Count - 1.0) * 100.0; // Use if with reset
                                                                                               //accuracy = (double)matches / (double)nv.nFrames.Count * 100.0; // Use if without reset


                        Console.WriteLine($"Cycle: {cycle}\tMatches={matches} of {nv.nFrames.Count}\t {accuracy}%");
                        if (accuracy == lastCycleAccuracy)
                        {
                            // The learning may result in saturated accuracy
                            // Unable to learn to higher accuracy, Exit
                            saturatedAccuracyCount += 1;
                            if (saturatedAccuracyCount >= 50 && lastCycleAccuracy > 80)
                            {
                                List<string> outputLog = new();
                                if(!Directory.Exists(Path.Combine($"{outputFolder}","TEST")))
                                {
                                    Directory.CreateDirectory(Path.Combine($"{outputFolder}","TEST"));
                                }
                                string fileName = Path.Combine(outputFolder,"TEST",$"saturatedAccuracyLog_{nv.label}_{nv.name}");
                                outputLog.Add($"Result Log for reaching saturated accuracy at {accuracy}");
                                outputLog.Add($"Label: {nv.label}");
                                outputLog.Add($"Video Name: {nv.name}");
                                outputLog.Add($"Stop after {cycle} cycles");
                                outputLog.Add($"Elapsed time: {sw.ElapsedMilliseconds / 1000 / 60} min.");
                                outputLog.Add($"reaching stable after enter newborn cycle {newbornCycle}.");
                                RecordResult(outputLog, fileName);

                                isCompletedSuccessfully = true;

                                break;
                            }
                        }
                        lastCycleAccuracy = accuracy;
                        //learn = true;
                        // Reset Temporal memory after learning 1 time the video/sequence
                        tm.Reset(mem);
                    }

                    if (isCompletedSuccessfully == false)
                    {
                        Console.WriteLine($"The experiment didn't complete successully. Exit after {maxCycles}!");
              
                    }
                    Console.WriteLine("------------ END ------------");
                    previousInputs.Clear();
                }
            }
            //Testing Section
            string userInput;
            string testOutputFolder = Path.Combine(outputFolder,"TEST");
            if (!Directory.Exists(testOutputFolder))
            {
                Directory.CreateDirectory(testOutputFolder);
            }

            int testNo = 0;

            // Test from startupConfig.json
            foreach (var testFilePath in startupCfg.TestFiles)
            {
                testNo = PredictImageInput(videoData, cls, layer1, testFilePath, testOutputFolder, testNo);
            }


            // Manual input from user
            WriteLineColor("Drag an image as input to recall the learned Video or type (Write Q to quit): ");

            userInput = Console.ReadLine().Replace("\"", "");

            while (userInput!="Q")
            {
                testNo = PredictImageInput(videoData, cls, layer1, userInput, testOutputFolder, testNo);
                userInput = Console.ReadLine().Replace("\"", "");
            }
        }

        #endregion

        #region private help method
        /// <summary>
        /// Predict series from input Image.
        /// <br>Process:</br>
        /// <br>Binarize input image</br>
        /// <br>Convert the binarized image to SDR via Spatial Pooler</br>
        /// <br>Get Predicted Cells from Compute output </br>
        /// <br>Compare the predicted Cells with learned HTMClassifier</br>
        /// <br>Create predicted image sequence as Video from classifier output and video database videoData </br>
        /// </summary>
        /// <param name="frameWidth">image framewidth</param>
        /// <param name="frameHeight"></param>
        /// <param name="colorMode"></param>
        /// <param name="videoData"></param>
        /// <param name="cls"></param>
        /// <param name="layer1"></param>
        /// <param name="userInput"></param>
        /// <param name="testOutputFolder"></param>
        /// <param name="testNo"></param>
        /// <returns></returns>
        private static int PredictImageInput(List<VideoSet> videoData, HtmClassifier<string, ComputeCycle> cls, CortexLayer<object, object> layer1, string userInput, string testOutputFolder, int testNo)
        {
            // TODO: refactor video library for easier access to these properties
            int frameWidth = videoData[0].nVideoList[0].frameWidth;
            int frameHeight = videoData[0].nVideoList[0].frameHeight;
            ColorMode colorMode = videoData[0].nVideoList[0].colorMode;

            string Outputdir = $"{testOutputFolder}" + @"\" + $"Predicted from {Path.GetFileNameWithoutExtension(userInput)}";
            if (!Directory.Exists(Outputdir))
            {
                Directory.CreateDirectory(Outputdir);
            }
            testNo += 1;
            // Save the input Frame as NFrame
            NFrame inputFrame = new(new Bitmap(userInput), "TEST", "test", 0, frameWidth, frameHeight, colorMode);
            inputFrame.SaveFrame(Outputdir + @"\" + $"Converted_{Path.GetFileName(userInput)}");
            // Compute the SDR of the Frame
            var lyrOut = layer1.Compute(inputFrame.EncodedBitArray, false) as ComputeCycle;

            // Use HTMClassifier to calculate 5 possible next Cells Arrays
            var predictedInputValue = cls.GetPredictedInputValues(lyrOut.PredictiveCells.ToArray(), 5);


            foreach (var serie in predictedInputValue)
            {
                WriteLineColor($"Predicted Serie:", ConsoleColor.Green);
                string s = serie.PredictedInput;
                WriteLineColor(s);
                Console.WriteLine("\n");
                //Create List of NFrame to write to Video
                List<NFrame> outputNFrameList = new();
                string Label = "";
                List<string> frameKeyList = s.Split("-").ToList();
                foreach (string frameKey in frameKeyList)
                {
                    foreach (var vs in videoData)
                    {
                        foreach (var vd in vs.nVideoList)
                        {
                            foreach (var nf in vd.nFrames)
                            {
                                if (nf.FrameKey == frameKey)
                                {
                                    Label = nf.label;
                                    outputNFrameList.Add(nf);
                                    break;
                                }
                            }
                        }
                    }
                }

                // Create output video
                NVideo.CreateVideoFromFrames(
                    outputNFrameList,
                    $"{Outputdir}" + @"\" + $"testNo_{testNo}_Label{Label}_similarity{serie.Similarity}_No of same bit{serie.NumOfSameBits}.mp4",
                    (int)videoData[0].nVideoList[0].frameRate,
                    new Size((int)videoData[0].nVideoList[0].frameWidth, (int)videoData[0].nVideoList[0].frameHeight),
                    true);
            }

            return testNo;
        }
        /// <summary>
        /// Writing experiment result to write to a text file
        /// </summary>
        /// <param name="possibleOutcomeSerie"></param>
        /// <param name="inputVideo"></param>
        private static void RecordResult(List<string> result, string fileName)
        {
            File.WriteAllLines($"{fileName}.txt", result);
        }

        /// <summary>
        /// <para>Initiate the settings of HTM</para>
        /// </summary>
        /// <param name="inputBits">number of bit in input array</param>
        /// <param name="numColumns">number of columns in SDR</param>
        /// <returns></returns>
        private static HtmConfig GetHTMConfig(int[] inputBits, int[] numColumns)
        {
            HtmConfig htm = new(inputBits, numColumns)
            {
                Random = new ThreadSafeRandom(42),

                CellsPerColumn = 15,
                GlobalInhibition = true,
                //LocalAreaDensity = -1,
                NumActiveColumnsPerInhArea = 0.02 * numColumns[0],
                PotentialRadius = (int)(0.15 * inputBits[0]),
                //InhibitionRadius = 15,

                MaxBoost = 10.0,
                DutyCyclePeriod = 100,
                MinPctOverlapDutyCycles = 0.75,
                MaxSynapsesPerSegment = (int)(0.02 * numColumns[0]),
                StimulusThreshold = (int)0.05 * numColumns[0],
                //ActivationThreshold = 15,
                //ConnectedPermanence = 0.5,

                // Learning is slower than forgetting in this case.
                //PermanenceDecrement = 0.15,
                //PermanenceIncrement = 0.15,

                // Used by punishing of segments.
            };
            return htm;
        }
        /// <summary>
        /// Get the key for HTMClassifier learning stage.
        /// The key here is a serie of frames' keys, seperated by "-"
        /// </summary>
        /// <param name="prevInputs"></param>
        /// <returns></returns>
        private static string GetKey(List<string> prevInputs)
        {
            string key = string.Join("-", prevInputs);
            return key;
        }

        // Hello screen
        // TODO: adding instruction/ introduction/ experiment flow
        private static void RenderHelloScreen()
        {
            WriteLineColor($"Hello NeoCortexApi! Conducting experiment {nameof(VideoLearning)} CodeBrakers");
        }


        /// <summary>
        /// Create folders required for the experiment.
        /// </summary>
        /// <param name="outputFolder"></param>
        /// <param name="convertedVideoDir"></param>
        /// <param name="testOutputFolder"></param>
        private static void CreateTemporaryFolders(out string outputFolder, out string convertedVideoDir, out string testOutputFolder)
        {
            // Define first the desired properties of the frames
            outputFolder = "Run1ExperimentOutput";
            if (!Directory.Exists($"{outputFolder}"))
            {
                Directory.CreateDirectory($"{outputFolder}");
            }
            convertedVideoDir = $"{outputFolder}" + @"\" + "Converted";
            if (!Directory.Exists($"{convertedVideoDir}"))
            {
                Directory.CreateDirectory($"{convertedVideoDir}");
            }
            testOutputFolder = $"{outputFolder}" + @"\" + "TEST";
            if (!Directory.Exists(testOutputFolder))
            {
                Directory.CreateDirectory(testOutputFolder);
            }
        }
        #endregion
        /// <summary>
        /// Print a line in Console with color and/or hightlight
        /// <param name="str">string to print</param>
        /// <param name="foregroundColor">Text color</param>
        /// <param name="backgroundColor">Hightlight Color</param>
        /// </summary>
        public static void WriteLineColor(
            string str,
            ConsoleColor foregroundColor = ConsoleColor.White,
            ConsoleColor backgroundColor = ConsoleColor.Black)
        {
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;
            Console.WriteLine(str);
            Console.ResetColor();
        }
        /// <summary>
        /// Gets directories inside the passed parent directory
        /// <param name="folderPath">Absolute path of the parent folder</param>
        /// <returns>Returns an array of each video set's directory</returns>
        /// </summary>
        public static string[] GetVideoSetPaths(string folderPath)
        {
            // remove the two outer quotation marks
            folderPath = folderPath.Replace("\"", "");
            string[] videoSetPaths = Array.Empty<string>();
            string testDir;
            if (Directory.Exists(folderPath))
            {
                testDir = folderPath;
                WriteLineColor($"Inserted Path is found", ConsoleColor.Green);
                Console.WriteLine($"Begin reading directory: {folderPath} ...");
            }
            else
            {
                string currentDir = Directory.GetCurrentDirectory();
                WriteLineColor($"The inserted path for the training folder is invalid. " +
                    $"If you have trouble adding the path, copy your training folder with name TrainingVideos to {currentDir}", ConsoleColor.Yellow);
                // Get the root path of training videos
                testDir = $"{currentDir}\\TrainingVideos";
            }
            // Get all the folders that contain video sets under TrainingVideos
            try
            {
                videoSetPaths = Directory.GetDirectories(testDir, "*", SearchOption.TopDirectoryOnly);
                WriteLineColor("Complete reading directory ...");
                return videoSetPaths;
            }
            catch (Exception e)
            {
                WriteLineColor("=========== Caught exception ============", ConsoleColor.Magenta);
                WriteLineColor(e.Message, ConsoleColor.Magenta);
                return videoSetPaths;
            }
        }
    }
}
