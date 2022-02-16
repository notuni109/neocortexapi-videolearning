using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoLibrary;

namespace HTMVideoLearning
{
    public class VideoConfig
    {
        /// <summary>
        /// video frame width in pixels
        /// </summary>
        public readonly int FrameWidth;
        /// <summary>
        /// video frame height in pixels
        /// </summary>
        public readonly int FrameHeight;
        /// <summary>
        /// <br>video's frame rate</br>
        /// <br>must always smaller than original video</br>
        /// </summary>
        public readonly double FrameRate;
        /// <summary>
        /// color encoding
        /// </summary>
        public ColorMode ColorMode;
        /// <summary>
        /// <br>color encoding but in interger</br>
        /// <br>this is used untill we can deserialize enum from json</br>
        /// </summary>
        public int ColorModeInt;
        /// <summary>
        /// The root folder where training videos are stored.
        /// </summary>
        public string TrainingDatasetRoot { get; set; }

        /// <summary>
        /// test file path array after the experiment runs
        /// </summary>
        public string[] TestFiles { get; set; }

        public VideoConfig(int FrameWidth, int FrameHeight, double FrameRate, ColorMode colorMode)
        {
            this.FrameWidth = FrameWidth;  
            this.FrameHeight = FrameHeight;
            this.FrameRate = FrameRate;
            this.ColorMode = colorMode;
        }

        /// <summary>
        /// <br>temporary solution for deserializing</br>
        /// <br>this is used untill we can deserialize enum from json</br>
        /// </summary>
        public void initColorMode()
        {
            this.ColorMode = (ColorMode)ColorModeInt;
        } 
    }
}
