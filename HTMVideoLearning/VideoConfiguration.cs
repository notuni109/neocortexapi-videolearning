using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoLibrary;

namespace HTMVideoLearning
{
    public class VideoConfiguration
    {
        public readonly int FrameWidth;
        public readonly int FrameHeight;
        public readonly double FrameRate;
        public readonly int[] NumberOfColumns;
        public readonly ColorMode ColorMode;
        public readonly int[] InputBits;
        public VideoConfiguration(int FrameWidth, int FrameHeight, double FrameRate, ColorMode colorMode, int NumberOfColumns)
        {
            this.FrameWidth = FrameWidth;  
            this.FrameHeight = FrameHeight;
            this.FrameRate = FrameRate;
            this.ColorMode = colorMode;
            this.NumberOfColumns = new int[] { NumberOfColumns };
            int inputBits = this.FrameWidth * this.FrameHeight * (int)this.ColorMode;
            this.InputBits= new int[] { inputBits } ;
        }
    }
}
