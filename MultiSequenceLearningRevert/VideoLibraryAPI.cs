using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoLibrary;
using StartupCfg;

namespace MultiSequenceLearningRevert
{
    public class VideoLibraryAPI
    {
        /// <summary>
        /// Transforming the video from the provided folder path to dictionary of video's key(label_videoName), taking the list of its encoded frames(binarized) as its value
        /// </summary>
        /// <param name="trainingDatasetRoot">Training folder path, this contain subfolder. The names of the sub folder represents the label of the videos it contains</param>
        /// <returns></returns>
        public static Dictionary<string, List<int[]>> GetTrainingVideos(string trainingDatasetRoot, StartupConfig config)
        {
            Dictionary<string, List<int[]>> videoSet = new Dictionary<string, List<int[]>>();


            return videoSet;
        }
    }
    
}
