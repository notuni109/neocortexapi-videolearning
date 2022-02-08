using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoLibrary;
using StartupCfg;
using System.Diagnostics;

namespace MultiSequenceLearningRevert
{
    public class VideoLibraryAPI
    {
        /// <summary>
        /// Transforming the video from the provided folder path to dictionary of video's key(label_videoName), taking the list of its encoded frames(binarized) as its value
        /// </summary>
        /// <param name="trainingDatasetRoot">Training folder path, this contain subfolder. The names of the sub folder represents the label of the videos it contains</param>
        /// <returns></returns>
        public static Dictionary<string, List<int[]>> GetTrainingVideos(StartupConfig config)
        {
            Dictionary<string, List<int[]>> videoSet = new Dictionary<string, List<int[]>>();

            string[] videoDatasetRootFolder = HelperFunction.GetVideoSetPaths(config.TrainingDatasetRoot);
            foreach (string path in videoDatasetRootFolder)
            {
                VideoSet vs = new(path, ColorMode.BLACKWHITE, config.frameWidth, config.frameHeight, config.frameRate);
                foreach (var video in vs.nVideoList)
                {
                    string videoKey = $"{video.label}__{video.name}";
                    List<int[]> encodedFrameList = new List<int[]>();

                    // Create a list of int bit array, each array is one frame
                    // Encoder for Image
                    foreach (var frame in video.nFrames)
                    {
                        encodedFrameList.Add(frame.EncodedBitArray);
                    }
                    videoSet.Add(videoKey, encodedFrameList);
                }

                vs.ExtractFrames(config.convertedVideoDir);
            }
            return videoSet;
        }
        /// <summary>
        /// Make a shorter video from original video
        /// </summary>
        /// <param name="videoPath">fullPath to the video</param>
        /// <param name="beginFrame">the begin capture index of the original vid</param>
        /// <param name="endFrame"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static List<int[]> GetTestVideo(string videoPath, int beginFrame, int endFrame, StartupConfig config)
        {
            NVideo video = new(videoPath, "", config.colorMode, config.frameWidth, config.frameHeight, config.frameRate);
            string videoKey = $"{video.label}__{video.name}";
            KeyValuePair<string, List<int[]>> res = new KeyValuePair<string, List<int[]>>(videoKey, new List<int[]>());
            
            // check if the parameter is valid
            if(beginFrame > endFrame || endFrame>video.nFrames.Count || beginFrame<0)
            {
                Debug.WriteLine("Invalid parameter");
                return res.Value;
            }
            int i = 0;
            while (i < endFrame)
            {
                if (i >= beginFrame)
                {
                    res.Value.Add(video.nFrames[i].EncodedBitArray);
                }
            }
            return res.Value;
        }
    }

}
