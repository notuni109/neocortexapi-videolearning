using VideoLibrary;
namespace StartupCfg
{
    public class StartupConfig
    {

        int frameWidth = 18;
        int frameHeight = 18;
        double frameRate = 12;

        ColorMode colorMode = ColorMode.BLACKWHITE;


        //// adding condition for 
        //// Define HTM parameters
        //int[] inputBits = { frameWidth * frameHeight * (int)colorMode };

        /// <summary>
        /// The root folder where training videos are stored.
        /// </summary>
        public string TrainingDatasetRoot { get; set; }
    }
}
