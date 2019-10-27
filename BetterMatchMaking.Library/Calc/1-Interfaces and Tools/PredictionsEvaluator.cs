using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ConsoleTables;
using BetterMatchMaking.Library.Data;

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


            OutputDebugDecisionMessage(">> DECISIONS TO GET BEST");
            OutputDebugDecisionMessage(">> *********************");

            // get the split number and the remaining classes with cars
            int splitNumber = _predictions[0].CurrentSplit.Number;
            int remClassWithCars = (from r in _classesQueues where r.CarsCount > 0 select r).Count();

            OutputDebugDecisionMessage("");
            OutputDebugDecisionMessage("Available predictions: " + choices.Count);
            OutputDebugDecisionResults(choices, null);
            OutputDebugDecisionMessage("");
            OutputDebugDecisionMessage("");


            // if the Parameter TopSplitExceptionValue is  ON,
            // we only want the scenario with all the classes

            if (splitNumber == 1 && ParameterTopSplitExceptionValue == 1)
            {
                OutputDebugDecisionMessage("[?] TopSplitException is enabled, and it is the Top Split, get the prediction with all complete classes.");
                filter = (from r in choices where r.NumberOfClasses == remClassWithCars
                          && r.ClassesCuttedAroundRatingThreshold.Count == 0
                          orderby r.DiffBetweenClassesPercent select r).ToList();
                if (filter.Count > 0)
                {
                    
                    choices = filter;
                    OutputDebugDecisionMessage(" - Found (" + choices.Count + ")");
                    OutputDebugDecisionResults(choices, 1);

                    // in that case we don't need any more tests
                    CommitDebugDecisions(splitNumber);
                    return choices.First();
                }
                else
                {
                    OutputDebugDecisionMessage(" - Not found, continue with all predictions ("+choices.Count+")");
                }
                OutputDebugDecisionMessage("");
            }


            if (ParameterNoMiddleClassesEmptyValue == 1)
            {
                OutputDebugDecisionMessage("[?] NoMiddleClassesEmpty is enabled, filter predictions with non empty classes betwen less and more populated of each split.");

                filter = (from r in choices where r.NoMiddleClassesMissing select r).ToList();
                if (filter.Count > 0)
                {
                    choices = filter;
                    OutputDebugDecisionMessage(" - Found (" + choices.Count + ")");
                    OutputDebugDecisionResults(choices, null);
                }
                else
                {
                    OutputDebugDecisionMessage(" - Not found, continue with previous predictions (" + choices.Count + ")");

                }
            }
            OutputDebugDecisionMessage("");





            OutputDebugDecisionMessage("[?] Try to get predictions where :");
            OutputDebugDecisionMessage("    Diff between Min SoF in current split and Max SoF of in next split is less than 2%.");
            OutputDebugDecisionMessage("    or");
            OutputDebugDecisionMessage("    Global SoF can be tester with MaxSofFunct (higher than MaxSofFunctStartingIRValue)");
            OutputDebugDecisionMessage("    or");
            OutputDebugDecisionMessage("    The Max SoF of the split is not the most Populated class");

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
            if (filter.Count > 0)
            {
                choices = filter;
                OutputDebugDecisionMessage(" - Found (" + choices.Count + ")");
                OutputDebugDecisionResults(choices, null);
            }
            else
            {
                OutputDebugDecisionMessage(" - Not found, continue with previous predictions (" + choices.Count + ")");

            }
            OutputDebugDecisionMessage("");



            for (int i = remClassWithCars; i >= 1; i--)
            {
                OutputDebugDecisionMessage("[?] Try to get predictions with " + i + " classes contains car");
                OutputDebugDecisionMessage("    and Diff is lower than MaxSof or MaxSofFunct");

                filter = (from r in choices where r.NumberOfClasses == i select r).ToList();
                if (filter.Count > 0)
                {
                    
                    filter = (from r in filter where r.DiffBetweenClassesPercent < GetLimit(r) select r).ToList();
                    if (filter.Count > 0)
                    {
                        choices = filter;
                        OutputDebugDecisionMessage(" - Found (" + choices.Count + ")");
                        OutputDebugDecisionResults(choices, null);
                        OutputDebugDecisionMessage("");

                        break;
                    }
                    else
                    {
                        OutputDebugDecisionMessage(" - Not found (higher than allowed diff), continue with previous predictions (" + choices.Count + ")");
                        OutputDebugDecisionMessage("");

                    }
                }
                else
                {
                    OutputDebugDecisionMessage(" - Not found (not with " + i + " classes), continue with previous predictions (" + choices.Count + ")");
                    OutputDebugDecisionMessage("");
                }
            }


            foreach (var choice in choices)
            {
                if (choice.NumberOfClasses == 1) choice.DiffBetweenClassesPercent = 999;
            }


            OutputDebugDecisionMessage("[?] Sort remaining predictions with less Diff first");
            OutputDebugDecisionMessage("    First of the list will be the answer");
            OutputDebugDecisionMessage(" - Found (" + choices.Count + ")");
            choices = (from r in choices orderby r.DiffBetweenClassesPercent ascending select r).ToList();
            OutputDebugDecisionResults(choices, null);
            OutputDebugDecisionMessage("");


            var bestpred = choices.First();


            CommitDebugDecisions(splitNumber);
            return bestpred;
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
