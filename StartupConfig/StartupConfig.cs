﻿using VideoLibrary;
namespace StartupCfg
{
    public class StartupConfig
    {

        public int frameWidth = 24;
        public int frameHeight = 24;
        public double frameRate = 0;

        public ColorMode colorMode = ColorMode.BLACKWHITE;
        public string outputFolder { get; set; }
        public string convertedVideoDir { get; set; }
        public string testOutputFolder { get; set; }

        //// adding condition for 
        //// Define HTM parameters
        //int[] inputBits = { frameWidth * frameHeight * (int)colorMode };

        /// <summary>
        /// The root folder where training videos are stored.
        /// </summary>
        public string TrainingDatasetRoot { get; set; }

        /// <summary>
        /// return the dimension of the encoded frame
        /// frameWidth * frameHeight * (int)colorMode
        /// </summary>
        /// <returns></returns>
        public int[] GetEncodedBitDimension()
        {
            return new int[] { frameWidth * frameHeight * (int)colorMode };
        }
    }
}