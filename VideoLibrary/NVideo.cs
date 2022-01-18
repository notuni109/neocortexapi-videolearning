using Emgu.CV;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
namespace VideoLibrary
{
    /// <summary>
    /// <para>
    /// <br>Represent a single video, which contains</br>
    /// <br>Name of the video, but not including suffix format</br>
    /// <br>List of int[], with each int[] is a frame in chronological order start - end</br>
    /// <br>The Image can be scaled
    /// </para>
    /// </summary>
    public class NVideo
    {
        public string name;
        public List<NFrame> nFrames;
        public string label;

        private readonly ColorMode colorMode;
        public readonly int frameWidth;
        public readonly int frameHeight;
        public readonly double frameRate;
        /// <summary>
        /// Generate a Video object
        /// </summary>
        /// <param name="videoPath">full path to the video</param>
        /// <param name="colorMode">Color mode to encode each frame in the Video, see enum VideoSet.ColorMode</param>
        /// <param name="frameHeight">height in pixels of the video resolution</param>
        /// <param name="frameWidth">width in pixels of the video resolution</param>
        public NVideo(string videoPath, string label, ColorMode colorMode, int frameWidth, int frameHeight, double frameRate)
        {
            this.colorMode = colorMode;
            this.frameWidth = frameWidth;
            this.frameHeight = frameHeight;
            this.frameRate = frameRate;
            this.label = label;

            this.nFrames = new List<NFrame>();
            this.name = Path.GetFileNameWithoutExtension(videoPath);

            var fromBitmaps = ReadVideo(videoPath, frameRate);
            for (int i = 0; i < fromBitmaps.Count; i++)
            {
                NFrame tempFrame = new NFrame(fromBitmaps[i], name, label, i, frameWidth, frameHeight, colorMode);
                nFrames.Add(tempFrame);
            }
        }
        /// <summary>
        /// <para>Method to read a video into a list of Bitmap, from video path to a list of Bitmap</para>
        /// The current implementation uses OpenCV wrapper emgu
        /// </summary>
        /// <param name="videoPath"> full path of the video to be read </param>
        /// <returns>List of Bitmaps</returns>
        private static List<Bitmap> ReadVideo(string videoPath, double framerate = 0)
        {
            List<Bitmap> videoBitmapArray = new List<Bitmap>();

            // Create VideoFrameReader object for the video from videoPath
            VideoCapture vd = new(videoPath);

            double step = 1;
            // New step for iterating the video in a lower frameRate when specified
            double framerateDefault = vd.Get(Emgu.CV.CvEnum.CapProp.Fps);
            if (framerate != 0)
            {
                step = framerateDefault / framerate;
            }
            Mat currentFrame = new();
            int count = 0;
            int currentFrameIndex = 0;
            int stepCount = 0;
            while (currentFrame != null)
            {
                currentFrame = vd.QueryFrame();
                if (count == currentFrameIndex)
                {
                    if (currentFrame == null)
                    {
                        break;
                    }
                    stepCount += 1;
                    currentFrameIndex = (int)(stepCount * step);
                    videoBitmapArray.Add(currentFrame.ToBitmap());
                }
                count += 1;
            }
            vd.Dispose();
            return videoBitmapArray;
        }
        public int[] GetEncodedFrame(string key)
        {
            foreach (NFrame nf in nFrames)
            {
                if (nf.FrameKey == key)
                {
                    return nf.EncodedBitArray;
                }
            }
            return new int[] { 4, 2, 3 };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bitmapList">indexing a list of objects for sorting,searching and manipulating</param>
        /// <param name="videoOutputPath">Full path of the output of the video</param>
        /// <param name="frameRate">Rate of the frame </param>
        /// <param name="dimension">Height & Width of objects</param>
        /// <param name="isColor">Color is True or False</param>
        /// <param name="codec"> null </param>
        public static void NFrameListToVideo(List<NFrame> bitmapList, string videoOutputPath, int frameRate, Size dimension, bool isColor, char[] codec = null )
        {
            if( codec == null)
            {
                codec = new char[] {'M', 'P', '4', 'V'};
            }
            int fourcc = VideoWriter.Fourcc(codec[0], codec[1], codec[2], codec[3]);
            //There was a -1 instead of fourcc, Now fourcc is required for selecting codec, it requires 4 parameters as input
            using (VideoWriter videoWriter = new($"{videoOutputPath}.mp4", fourcc, (int)frameRate, dimension, isColor))
            {
                foreach (NFrame frame in bitmapList)
                {
                    Bitmap tempBitmap = frame.IntArrayToBitmap(frame.EncodedBitArray);
                    videoWriter.Write(tempBitmap.ToMat());
                }
            }
        }
    }
}
