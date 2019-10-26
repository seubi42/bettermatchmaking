using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Library.Data;

namespace BetterMatchMaking.Library.Calc
{ 
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

        public void Compute(List<Line> data, int fieldSize)
        {
            if (ParameterDebugFileValue == 1)
            {
                PredictionsEvaluator.CleanOldDebugFiles();
            }

            // reduce the fieldSize if possible
            numberOfSplits = Tools.DivideAndCeil(data.Count, fieldSize);
            int betterFieldSize = Tools.DivideAndCeil(data.Count, numberOfSplits);
            this.fieldSize = Math.Min(fieldSize, betterFieldSize);

            // class queues
            classesQueues = Tools.SplitCarsPerClass(data);
            classesIds = (from r in classesQueues select r.CarClassId).ToList();

            // we need a affine distribution of car algorithm
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

            // test
            Splits = new List<Split>();

            int prevMaxPopSof = -1;

            for (int i = 0; i < numberOfSplits; i++)
            {
                int remClasses = (from r in classesQueues where r.CarsCount > 0 select r).Count();
                var prediction = PredictSplit(i, prevMaxPopSof, remClasses);
                if (prediction != null)
                {
                    Implement(prediction);
                    prevMaxPopSof = prediction.CurrentSplit.GetMaxClassSof();
                }

            }


            // last split (bad)
            var lastsplit = Splits.Last();
            for (int i = 0; i < classesQueues.Count; i++)
            {
                var cars = classesQueues[i].PickCars(classesQueues[i].CarsCount);
                if (cars.Count > 0)
                {
                    if (lastsplit.GetClassId(i) == 0)
                    {
                        lastsplit.SetClass(i, classesIds[i]);
                    }
                    lastsplit.AppendClassCars(i, cars);
                }
            }

            // optimize 
            
            classesQueues = Tools.SplitCarsPerClass(data);
            SplitsRepartitionOptimizer optimizer = new SplitsRepartitionOptimizer(Splits,
                fieldSize,
                classesIds,
                classesQueues,
                affineDistributor);
            Splits = optimizer.OptimizeAndSolveDifferences(); // a third pass

        }

        

        private void Implement(Data.PredictionOfSplits prediction)
        {
            Data.Split split = new Split();
            split.Number = prediction.CurrentSplit.Number;
            Splits.Add(split);

            for (int i = 0; i < 4; i++)
            {
                var take = prediction.CurrentSplit.CountClassCars(i);
                if(take > 0)
                {
                    int classId = classesIds[i];
                    var cars = classesQueues[i].PickCars(take);
                    split.SetClass(i, cars, classId);
                }
            }
            
        }

        private Data.PredictionOfSplits PredictSplit(int splitIndex, int prevMaxPopSof, int remainingQueues)
        {
            var availableQueues = (from r in classesQueues where r.CarsCount > 0 select r).ToList();
            var availableClassesId = (from r in availableQueues select r.CarClassId).ToList();

            // combinations for split
            Calc.EveryCombinations combS = new EveryCombinations(availableClassesId);
            // combinations for next split
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

            // foreach class, except the most populated one.
            // starting from biggest to lowers
            for (int classIndex = classesIds.Count - 2; classIndex >= 0; classIndex--)
            {
                int classId = classesIds[classIndex];
                foreach (var prediction in predictions)
                {
                    // calc the differences between the class and the most populated one
                    prediction.CalcDiff(classIndex, classId);
                }
                
            }

            List<PredictionOfSplits> predictions2 = new List<PredictionOfSplits>();

            foreach (var prediction in predictions)
            {
                prediction.CalcStats(prevMaxPopSof);
                predictions2.Add(prediction);

                if (ParameterRatingThresholdValue > 0)
                {
                    var variation = prediction.CuttedVariation(ParameterRatingThresholdValue, prevMaxPopSof);
                    if (variation != null) predictions2.Add(variation);
                }
            }


            // Choose the best now
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

        private Split GenerateSplitFromCombination(int splitNumber, Calc.Combination c, Split previewsplit = null)
        {
            
            Split s = new Split(splitNumber);
            int splitClasses = c.ClassesId.Length;

            var enabledClassesId = c.EnabledClassesId;
            for (int i = 0; i < splitClasses; i++)
            {

                if (c.Enabled[i])
                {
                    int classId = c.ClassesId[i];
                    int classIndex = classesIds.IndexOf(classId);

                    
                    

                    int totake = affineDistributor.TakeCars(classId, enabledClassesId, fieldSize);

                    int toskip = 0;
                    if (previewsplit != null) toskip = previewsplit.CountClassCars(classIndex);
                    
                    var cars = classesQueues[classIndex].GetFirstCars(toskip, totake);
                    s.SetClass(classIndex, cars, classId);
                }
            }
            return s;
        }
    }
}
