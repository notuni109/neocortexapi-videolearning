using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoLibrary;

namespace HTMVideoLearning
{
    public class StartupConfig
    {

        int frameWidth = 18;
        int frameHeight = 18;
        double frameRate = 12;
        //ColorMode colorMode = ColorMode.BLACKWHITE;


        //// adding condition for 
        //// Define HTM parameters
        /////int[] inputBits = { frameWidth * frameHeight * (int)colorMode };
        //int[] numColumns = { 1024 };

        /// <summary>
        /// The root folder where training videos are stored.
        /// </summary>
        public string TrainingDatasetRoot { get; set; }

        /// <summary>
        /// test file path array after the experiment runs
        /// </summary>
        public string[] TestFiles { get; set; }
    }
}
