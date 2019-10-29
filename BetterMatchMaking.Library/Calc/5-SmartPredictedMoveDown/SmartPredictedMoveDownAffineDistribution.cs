// Better Splits Project - https://board.ipitting.com/bettersplits
// Written by Sebastien Mallet (seubiracing@gmail.com - iRacer #281664)
// --------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Library.Data;

namespace BetterMatchMaking.Library.Calc
{
    /// <summary>
    /// This algorithm is the master piece of this project which try to put all good ideas together.
    /// This algorithm is called 'SmartPredictedMoveDown' because it evaluate any possibilities 
    /// for each split, before choosing the best one.
    /// 
    /// Because this algorithm does lot of this, be free to set UseParameterDebugFile=1
    /// for bettern understanding. It will create a 'predictlogs' directory containing
    /// a text file for each split process (ex: '01.txt' for the split 1)
    /// 
    /// </summary>
    public class SmartPredictedMoveDownAffineDistribution : IMatchMaking
    {

        public List<Split> Splits { get; private set; }

        #region Active Parameters
        public bool UseParameterNoMiddleClassesEmpty
        {
            get { return true; }
        }

        public bool UseParameterDebugFile
        {
            get { return true; }
        }
        public int ParameterNoMiddleClassesEmptyValue { get; set; }
        public int ParameterDebugFileValue { get; set; }

        public virtual bool UseParameterMaxSofDiff
        {
            get { return true; }

        }
        public int ParameterMaxSofDiffValue { get; set; }

        public virtual bool UseParameterMaxSofFunct
        {
            get { return true; }

        }
        public int ParameterMaxSofFunctStartingIRValue { get; set; }
        public int ParameterMaxSofFunctStartingThreshold { get; set; }
        public int ParameterMaxSofFunctExtraThresoldPerK { get; set; }

        public bool UseParameterTopSplitException
        {
            get { return true; }

        }
        public int ParameterTopSplitExceptionValue { get; set; }

        public virtual bool UseParameterMinCars
        {
            get { return true; }
        }

        public int ParameterMinCarsValue { get; set; }

        public bool UseParameterRatingThreshold
        {
            get { return true; }
        }
        public int ParameterRatingThresholdValue { get; set; }
        #endregion


        #region Disabled Parameters


        public virtual bool UseParameterClassPropMinPercent
        {
            get { return false; }
        }

        
        public int ParameterClassPropMinPercentValue { get; set; }
        


        #endregion

        ClassicAffineDistribution affineDistributor;
        List<Data.ClassCarsQueue> classesQueues;
        List<int> classesIds;
        int numberOfSplits = 0;
        int fieldSize = 0;
        int minCarsToHalfSplits;

        public void Compute(List<Line> data, int fieldSize)
        {
            if (ParameterDebugFileValue == 1)
            {
                PredictionsEvaluator.CleanOldDebugFiles();
            }

            minCarsToHalfSplits = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(ParameterMinCarsValue) / 2d));

            // Calc number of splits required
            // And try to reduce fieldSize, if possible, for the same number of splits
            numberOfSplits = Tools.DivideAndCeil(data.Count, fieldSize);
            int betterFieldSize = Tools.DivideAndCeil(data.Count, numberOfSplits);
            this.fieldSize = Math.Min(fieldSize, betterFieldSize);

            // create classes queues
            classesQueues = Tools.SplitCarsPerClass(data);
            classesIds = (from r in classesQueues select r.CarClassId).ToList();

            // instanciate an ClassicAffineDistributio algorithm to use its car distribution system
            affineDistributor = new ClassicAffineDistribution();
            affineDistributor.ParameterMinCarsValue = this.ParameterMinCarsValue;
            affineDistributor.SetFieldSize(fieldSize);
            affineDistributor.InitData(classesIds, data);

            if(classesQueues.Count == 1)
            {
                // if only one class, the affinedistributor will do the job
                affineDistributor.Compute(data, fieldSize);
                Splits = affineDistributor.Splits;
                return;
            }



            // Starting the process
            Splits = new List<Split>();

            // this variable will contain the SoF Max of the 'Most Populated Class' of the last split
            // it will be given to the next one to help evaluating possible situations
            int prevMaxPopSof = -1; 


            // for each split required
            for (int i = 0; i < numberOfSplits; i++)
            {
                // call the prediction algorithm, and get the best situation
                var prediction = ComputeFieldPredictionsAndKeepTheBest(i, prevMaxPopSof);

                // if something is possible
                if (prediction != null)
                {
                    // Create the split, for true.
                    Implement(prediction);

                    // remember the Max SoF of the 'Most Populated Class'
                    prevMaxPopSof = prediction.CurrentSplit.GetMaxClassSof();
                }

            }


            // create the last split ...
            var lastsplit = Splits.Last();
            // by checking any class queues
            for (int i = 0; i < classesQueues.Count; i++)
            {
                // if any cars are still in the queue
                var cars = classesQueues[i].PickCars(classesQueues[i].CarsCount);
                if (cars.Count > 0)
                {
                    // put them in this last split
                    if (lastsplit.GetClassId(i) == 0)
                    {
                        lastsplit.SetClass(i, classesIds[i]);
                    }
                    lastsplit.AppendClassCars(i, cars);
                }
            }

            // call the optimizer process (second pass algorithm) for
            // rounding cars number between splits, or fix some issue
            // if some splits exceed the fieldsize
            classesQueues = Tools.SplitCarsPerClass(data);
            SplitsRepartitionOptimizer optimizer = new SplitsRepartitionOptimizer(Splits,
                fieldSize,
                classesIds,
                classesQueues,
                affineDistributor);
            Splits = optimizer.OptimizeAndSolveDifferences(); // now we have the final result

        }

        
        
        /// <summary>
        /// This method will compute any possible possibilities for the next split (predictions),
        /// will make some stats on it, and then take a decision to keep the best one
        /// </summary>
        /// <param name="splitIndex">split index (starting from 0)</param>
        /// <param name="prevMostPopSof">Most Populated class SoF of the previous split</param>
        /// <returns></returns>
        private Data.PredictionOfSplits ComputeFieldPredictionsAndKeepTheBest(int splitIndex, int prevMostPopSof)
        {

            // remaining class car queues which contains cars in it
            var availableQueues = (from r in classesQueues where r.CarsCount > 0 select r).ToList();
            int remainingQueues = availableQueues.Count;

            // enum classes ids
            var availableClassesId = (from r in availableQueues select r.CarClassId).ToList();

            // Get the Truth Table of any possibile combinations for the current split (splitIndex)
            Calc.EveryCombinations combS = new EveryCombinations(availableClassesId);
            // Get anothe Truth Table of any possibile combinations for the next split (splitIndex+1)
            Calc.EveryCombinations combNS = new EveryCombinations(availableClassesId);


            // generate all possible predictions
            List<PredictionOfSplits> predictions = new List<PredictionOfSplits>();
            foreach (var cS in combS.Combinations)
            {
                foreach (var cNS in combNS.Combinations)
                {
                    if (cNS.NumberOfTrue >= remainingQueues)
                    {
                        PredictionOfSplits prediction = new PredictionOfSplits();
                        prediction.CurrentSplit = GenerateSplitFromCombination(splitIndex + 1, cS);
                        prediction.NextSplit = GenerateSplitFromCombination(splitIndex + 2, cNS, prediction.CurrentSplit);
                        if (prediction.CurrentSplit.TotalCarsCount > 0 && prediction.NextSplit.TotalCarsCount > 0)
                        {
                            predictions.Add(prediction);
                        }
                    }
                }
                
            }

            // calc stats of all predictions
            List<PredictionOfSplits> predictions2 = new List<PredictionOfSplits>(); // a new list
            foreach (var prediction in predictions) // for each existing prediction
            {
                prediction.CalcStats(prevMostPopSof); // calc stats
                predictions2.Add(prediction); // add it to a new list

                if (ParameterRatingThresholdValue > 0) // it the iRating split option is enabled
                {
                    // compute another prediction with cutting the split
                    var variations = prediction.CuttedVariation(ParameterRatingThresholdValue, prevMostPopSof, minCarsToHalfSplits);
                    if (variations != null && variations.Count > 0) predictions2.AddRange(variations); // if available, add this new predictions to the list
                }
            }


            if (predictions2.Count == 0) return null;

            // Choose the best now prediction
            PredictionsEvaluator eval = new PredictionsEvaluator(predictions2, classesQueues,
                ParameterMaxSofDiffValue, 
                ParameterMaxSofFunctStartingIRValue,
                ParameterMaxSofFunctStartingThreshold,
                ParameterMaxSofFunctExtraThresoldPerK,
                ParameterTopSplitExceptionValue,
                ParameterDebugFileValue,
                ParameterNoMiddleClassesEmptyValue);
            PredictionOfSplits bestScenario = eval.GetBest();
            return bestScenario;
        }


        /// <summary>
        /// This method generate a split from combination (Truth Tabl)
        /// It will convert fill corresponding classes described in a combination (with true/false)
        /// with concrete cars list, taking the right number
        /// </summary>
        /// <param name="splitNumber"></param>
        /// <param name="c"></param>
        /// <param name="previousSplit"></param>
        /// <returns></returns>
        private Split GenerateSplitFromCombination(int splitNumber, Calc.Combination c, Split previousSplit = null)
        {
            // create a new split
            Split s = new Split(splitNumber);

            // how may classes in this combination
            int splitClasses = c.ClassesId.Length;

            // foreach enabled classes
            var enabledClassesId = c.EnabledClassesId;
            for (int i = 0; i < splitClasses; i++)
            {
                if (c.Enabled[i])
                {
                    // get class info
                    int classId = c.ClassesId[i];
                    int classIndex = classesIds.IndexOf(classId);

                    // call the affineDistributor to calc the right number of cars
                    int totake = affineDistributor.TakeCars(classId, enabledClassesId, fieldSize);

                    int toskip = 0;
                    if (previousSplit != null)
                    {
                        // if this split is the second, and have a previous split
                        // we need to skip cars already in that previous split
                        toskip = previousSplit.CountClassCars(classIndex);
                    }
                    
                    // get cars
                    var cars = classesQueues[classIndex].GetFirstCars(toskip, totake);

                    // put them in the split
                    s.SetClass(classIndex, cars, classId);
                }
            }
            return s;
        }


        /// <summary>
        /// This method will transform a prediction to a concrete split
        /// </summary>
        /// <param name="prediction"></param>
        private void Implement(Data.PredictionOfSplits prediction)
        {
            // create a split with the right number
            Data.Split split = new Split();
            split.Number = prediction.CurrentSplit.Number;
            // add the split to the Splits List
            Splits.Add(split);

            // for each available queues
            for (int i = 0; i < classesQueues.Count; i++)
            {
                // get the number of car suggested in the prediction
                var take = prediction.CurrentSplit.CountClassCars(i);
                if (take > 0)
                {
                    // dequeue the cars from the waiting queue
                    // (it will takes car from the highest rating)
                    // (it will remove them from the queue)
                    int classId = classesIds[i];
                    var cars = classesQueues[i].PickCars(take);

                    // move theses car the the split
                    split.SetClass(i, cars, classId);


                }
            }

        }
    }
}
