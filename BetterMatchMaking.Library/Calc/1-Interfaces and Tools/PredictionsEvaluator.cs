using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ConsoleTables;

namespace BetterMatchMaking.Library.Calc
{
    partial class PredictionsEvaluator
    {
        

        List<Data.PredictionOfSplits> _predictions;
        List<Data.ClassCarsQueue> _classesQueues;
        public int ParameterMaxSofDiffValue { get; private set; }
        public int ParameterMaxSofFunctStartingIRValue { get; private set; }
        public int ParameterMaxSofFunctStartingThreshold { get; private set; }
        public int ParameterMaxSofFunctExtraThresoldPerK { get; private set; }
        public int ParameterTopSplitExceptionValue { get; private set; }
        public int ParameterNoMiddleClassesEmptyValue { get; set; }


        public PredictionsEvaluator(List<Data.PredictionOfSplits> predictions, List<Data.ClassCarsQueue> queues,
            int maxSofDiffValue, int maxSofFunctStartingIRValue, int maxSofFunctStartingThreshold, int maxSofFunctExtraThresoldPerK,
            int topSplitExceptionValue, int fileDebugger, int noMiddleClassesEmptyValue)
        {
            _predictions = predictions;
            _classesQueues = queues;

            ParameterMaxSofDiffValue = maxSofDiffValue;
            ParameterMaxSofFunctStartingIRValue = maxSofFunctStartingIRValue;
            ParameterMaxSofFunctStartingThreshold = maxSofFunctStartingThreshold;
            ParameterMaxSofFunctExtraThresoldPerK = maxSofFunctExtraThresoldPerK;
            ParameterTopSplitExceptionValue = topSplitExceptionValue;
            ParameterDebugFileValue = fileDebugger;
            ParameterNoMiddleClassesEmptyValue = noMiddleClassesEmptyValue;

            if (ParameterDebugFileValue == 1) WriteDebugsFiles();
        }

        public Data.PredictionOfSplits GetBest()
        {
            // we need two variables, to make rollback if one filter is not possible
            List<Data.PredictionOfSplits> choices = _predictions;
            List<Data.PredictionOfSplits> filter = null;
            // -->


            // get the split number and the remaining classes with cars
            int splitNumber = _predictions[0].CurrentSplit.Number;
            int remClassWithCars = (from r in _classesQueues where r.CarsCount > 0 select r).Count();



            // if the Parameter TopSplitExceptionValue is  ON,
            // we only want the scenario with all the classes
            if (splitNumber == 1 && ParameterTopSplitExceptionValue == 1)
            {
                filter = (from r in choices where r.NumberOfClasses == remClassWithCars orderby r.DiffBetweenClassesPercent select r).ToList();
                if (filter.Count > 0)
                {
                    choices = filter;
                    // in that case we don't need any more tests
                    return choices.First();
                }
            }


            if (ParameterNoMiddleClassesEmptyValue == 1)
            {
                filter = (from r in choices where r.NoMiddleClassesMissing select r).ToList();
                if (filter.Count > 0) choices = filter;
            }



                


            filter = (from r in choices
                        where 
                        r.CurrentSplit.GlobalSof > ParameterMaxSofFunctStartingIRValue
                        || 
                        //r.AllSofsHigherThanNextSplitMax
                        //||
                        r.DiffBetweenMinCurrentSplitSofAndMaxNextSplitSof <= 2
                        ||
                        r.MostPopulatedClassIsTheMaxSox == false
                        select r).ToList();
            if (filter.Count > 0) choices = filter;
            

            
            
            for (int i = remClassWithCars; i >= 1; i--)
            {
                filter = (from r in choices where r.NumberOfClasses == i select r).ToList();
                if (filter.Count > 0)
                {
                    filter = (from r in filter where r.DiffBetweenClassesPercent < GetLimit(r) select r).ToList();
                    if (filter.Count > 0)
                    {
                        choices = filter;
                        break;
                    }
                }
            }


            foreach (var choice in choices)
            {
                if (choice.NumberOfClasses == 1) choice.DiffBetweenClassesPercent = 999;
            }



            choices = (from r in choices orderby r.DiffBetweenClassesPercent ascending select r).ToList();
            

            return choices.First();
        }


        public double GetLimit(Data.PredictionOfSplits prediction)
        {
            //double splitSof = prediction.CurrentSplit.GlobalSof;
            int splitSof = prediction.CurrentSplit.GetMinClassSof();

            double maxDiff = ParameterMaxSofDiffValue;
            if (splitSof > ParameterMaxSofFunctStartingIRValue)
            {
                maxDiff = SofDifferenceEvaluator.EvalFormula(ParameterMaxSofFunctStartingIRValue,
                    ParameterMaxSofFunctStartingThreshold,
                    ParameterMaxSofFunctExtraThresoldPerK,
                    splitSof);
            }
            return maxDiff;
        }

    }
}
