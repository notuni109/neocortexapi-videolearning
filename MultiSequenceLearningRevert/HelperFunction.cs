using StartupCfg;
namespace MultiSequenceLearningRevert
{
    public class HelperFunction
    {
        /// <summary>
        /// Print a line in Console with color and/or hightlight
        /// </summary>
        /// <param name="str">string to print</param>
        /// <param name="foregroundColor">Text color</param>
        /// <param name="backgroundColor">Hightlight Color</param>
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
        /// Return an array of directories inside the passed parent directories
        /// In this Experiment:
        /// Return an array of each video set 's directory 
        /// </summary>
        /// <param name="trainingFolderPath"></param>
        /// <returns></returns>
        public static string[] GetVideoSetPaths(string trainingFolderPath)
        {
            // remove the two outer quotation marks
            trainingFolderPath = trainingFolderPath.Replace("\"", "");
            string[] videoSetPaths = Array.Empty<string>();
            string testDir;
            if (Directory.Exists(trainingFolderPath))
            {
                testDir = trainingFolderPath;
                HelperFunction.WriteLineColor($"Inserted Path is found", ConsoleColor.Green);
                Console.WriteLine($"Begin reading directory: {trainingFolderPath} ...");
            }
            else
            {
                string currentDir = Directory.GetCurrentDirectory();
                HelperFunction.WriteLineColor($"The inserted path for the training folder is invalid. " +
                    $"If you have trouble adding the path, copy your training folder with name TrainingVideos to {currentDir}",ConsoleColor.Yellow);
                // Get the root path of training videos.
                testDir = $"{currentDir}\\TrainingVideos";
            }
            // Get all the folders that contain video sets under TrainingVideos/
            try
            {
                videoSetPaths = Directory.GetDirectories(testDir, "*", SearchOption.TopDirectoryOnly);
                HelperFunction.WriteLineColor("Complete reading directory ...");
                return videoSetPaths;
            }
            catch(Exception e)
            {
                WriteLineColor("=========== Caught exception ============", ConsoleColor.Magenta);
                WriteLineColor(e.Message, ConsoleColor.Magenta);
                return videoSetPaths;
            }
        }
        public static void RenderHelloScreen()
        {
            // TODO:
            // Write Experiment description
            // Using ILogger to start recording
            Console.WriteLine($"Hello NeocortexApi! Experiment {nameof(MultiSequenceLearning)} adapting Video as Sequences of Frames");
            HelperFunction.WriteLineColor($"Conducting experiment {nameof(MultiSequenceLearningRevert)} Noath2302");
        }
        /// <summary>
        /// Creating output folder for the Experiment
        /// </summary>
        /// <param name="startupConfig"></param>
        public static void CreateTemporaryFolders(ref StartupConfig startupConfig)
        {
            // Define first the desired properties of the frames
            startupConfig.outputFolder = "Run1ExperimentOutput";

            if (!Directory.Exists($"{startupConfig.outputFolder}"))
            {
                Directory.CreateDirectory(startupConfig.outputFolder);
            }
            startupConfig.convertedVideoDir = Path.Combine(startupConfig.outputFolder,"Converted");
            if (!Directory.Exists($"{startupConfig.convertedVideoDir}"))
            {
                Directory.CreateDirectory(startupConfig.convertedVideoDir);
            }
            startupConfig.testOutputFolder = Path.Combine(startupConfig.outputFolder,"TEST");
            if (!Directory.Exists(startupConfig.testOutputFolder))
            {
                Directory.CreateDirectory(startupConfig.testOutputFolder);
            }
        }

        /// <summary>
        /// <br>Get total number of frames in the video set</br>
        /// <br>IDEA:</br>
        /// <br>This was initially getting the unique frame pattern to put into the spatial Pooler</br>
        /// <br>pre-correlation can omit frames which has correlation over a certain threshold, treating them as the same pattern</br>
        /// </summary>
        /// <param name="trainingVideos"></param>
        /// <returns></returns>
        public static int GetTotalNumberOfFrames(Dictionary<string, List<int[]>> trainingVideos)
        {
            int frameCount = 0;
            foreach(var video in trainingVideos.Values)
            {
                frameCount+=video.Count;
            }
            return frameCount;
        }
    }
}
