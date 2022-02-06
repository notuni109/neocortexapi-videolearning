using System.Diagnostics;
using MultiSequenceLearningRevert;
using static MultiSequenceLearningRevert.MultiSequenceLearning;

namespace NeoCortexApiSample
{
    class Program
    {
        /// <summary>
        /// This sample shows a typical experiment code for SP and TM.
        /// You must start this code in debugger to follow the trace.
        /// and TM.
        /// </summary>
        /// <param name="args">
        /// <br>first argument is startUpConfig Json</br>
        /// <br>second argument is the htmConfig Json</br>
        /// </param>
        static void Main(string[] args)
        {
            RunMultiSequenceLearningExperiment(args);
        }

        private static void RunMultiSequenceLearningExperiment(string[] args)
        {
            // Prototype for building the prediction engine.
            MultiSequenceLearning experiment = new MultiSequenceLearning();

            var predictor = experiment.Run(args[0], args[1]);



            //var list1 = new NVideo().nFrames();
            //var list2 = new double[] { 2.0, 3.0, 4.0 };
            //var list3 = new double[] { 8.0, 1.0, 2.0 };

            //var listImage = new List<string>();

            //predictor.Reset();
            //PredictNextElement(predictor, listImage);

            //predictor.Reset();
            //PredictNextElement(predictor, list2);

            //predictor.Reset();
            //PredictNextElement(predictor, list3);
        }

        private static void PredictNextElement(HtmPredictionEngine predictor, List<int[]> inputFrames)
        {
            Debug.WriteLine("------------------------------");

            foreach (var item in inputFrames)
            {
                var res = predictor.Predict(item);

                if (res.Count > 0)
                {
                    foreach (var pred in res)
                    {
                        Debug.WriteLine($"{pred.PredictedInput} - {pred.Similarity}");
                    }

                    var tokens = res.First().PredictedInput.Split('_');
                    var tokens2 = res.First().PredictedInput.Split('-');
                    Debug.WriteLine($"Predicted Sequence: {tokens[0]}, predicted next element {tokens2[tokens.Length - 1]}");
                }
                else
                    Debug.WriteLine("Nothing predicted :(");
            }

            Debug.WriteLine("------------------------------");
        }
    }
}