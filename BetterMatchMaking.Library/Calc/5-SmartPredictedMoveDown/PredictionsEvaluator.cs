// Better Splits Project - https://board.ipitting.com/bettersplits
// Written by Sebastien Mallet (seubiracing@gmail.com - iRacer #281664)
// --------------------------------------------------------------------
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
    /// <summary>
    /// This class implements the algorithm to choose the best option
    /// between all the possible predictions
    ///  
    /// It requires the all the possible predictions to analyze
    /// and all the Parameter settings
    /// </summary>
    partial class PredictionsEvaluator
    {

        /// <summary>
        /// Predictions to analize
        /// </summary>
        List<Data.PredictionOfSplits> _predictions;

        /// <summary>
        /// car classes queues, containing the available cars
        /// before the split creation
        /// </summary>
        List<Data.ClassCarsQueue> _classesQueues;


        /// <summary>
        /// standard Threshold of the maximum % difference allowed between MIN Sof and MAX Sof in a same split
        /// </summary>
        public int ParameterMaxSofDiffValue { get; private set; }

        /// <summary>
        /// Advanced Threshold of the maximum % difference allowed between MIN Sof and MAX Sof in a same split.
        /// This one is relative to the MIN SoF.
        /// Check the SofDifferenceEvaluator.EvalFormula() method, relatives to following parameters :
        /// ParameterMaxSofFunctStartingIRValue, 
        /// ParameterMaxSofFunctStartingThreshold, 
        /// ParameterMaxSofFunctExtraThresoldPerK
        /// </summary>
        public int ParameterMaxSofFunctStartingIRValue { get; private set; }
        public int ParameterMaxSofFunctStartingThreshold { get; private set; }
        public int ParameterMaxSofFunctExtraThresoldPerK { get; private set; }

        /// <summary>
        /// Do we want an exception for split 1 to force all classes in it
        /// no matters the % SoF differences ?
        /// </summary>
        public int ParameterTopSplitExceptionValue { get; private set; }

        /// <summary>
        /// Do we want to always have middle classes.
        /// For exemple, if classes are, from less populated to most populated :
        /// C7; GT3; GTE
        /// 
        /// if this parameter is 0(off), we allow every combinations :
        ///  C7 | GTE | GTE
        ///  X     X     X 
        ///  X           X      -> a middle class is missing here
        ///        X     X
        ///              X
        /// 
        /// It hits parameter is 1(on), we only allos these combinations :
        ///  X     X     X 
        ///        X     X
        ///              X
        /// </summary>
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

            if (ParameterDebugFileValue == 1) WriteSplitPredictionsFile();
        }



        public Data.PredictionOfSplits GetBest()
        {
            // we need two variables, to make rollback if one filter is not possible
            List<Data.PredictionOfSplits> choices = _predictions;
            List<Data.PredictionOfSplits> filter = null;
            // -->


            AppendDebugDecisionMessage(">> DECISIONS TO GET BEST");
            AppendDebugDecisionMessage(">> *********************");

            // get the split number and the remaining classes with cars
            int splitNumber = _predictions[0].CurrentSplit.Number;
            int remClassWithCars = (from r in _classesQueues where r.CarsCount > 0 select r).Count();

            AppendDebugDecisionMessage("");
            AppendDebugDecisionMessage("Available predictions: " + choices.Count);
            AppendDebugDecisionResults(choices, null);
            AppendDebugDecisionMessage("");
            AppendDebugDecisionMessage("");


            // if the Parameter TopSplitExceptionValue is  ON,
            // we only want the scenario with all the classes
            if (splitNumber == 1 && ParameterTopSplitExceptionValue == 1)
            {
                AppendDebugDecisionMessage("[?] TopSplitException is enabled, and it is the Top Split, get the prediction with all complete classes.");
                filter = (from r in choices
                          where r.NumberOfClasses == remClassWithCars
                          && r.ClassesCuttedAroundRatingThreshold.Count == 0
                          orderby r.DiffBetweenClassesPercent
                          select r).ToList();
                if (filter.Count > 0)
                {

                    choices = filter;
                    AppendDebugDecisionMessage(" - Found (" + choices.Count + ")");
                    AppendDebugDecisionResults(choices, 1);

                    // in that case we don't need any more tests
                    CommitDecisionsAppends(splitNumber);
                    return choices.First();
                }
                else
                {
                    AppendDebugDecisionMessage(" - Not found, continue with all predictions (" + choices.Count + ")");
                }
                AppendDebugDecisionMessage("");
            }


            // if NoMiddleClassesEmpty is enabled
            // we will filter combinations to exclude predictions
            // without middle classes
            bool noMiddleClassesFiltered = false;
            var predictionsBeforeMiddleClassesFiltere = choices;
            if (ParameterNoMiddleClassesEmptyValue >= 1)
            {
                AppendDebugDecisionMessage("[?] NoMiddleClassesEmpty is enabled, filter predictions with non empty classes betwen less and more populated of each split.");

                filter = (from r in choices where r.NoMiddleClassesMissing select r).ToList();
                if (filter.Count > 0)
                {
                    choices = filter;
                    noMiddleClassesFiltered = true;
                    AppendDebugDecisionMessage(" - Found (" + choices.Count + ")");
                    AppendDebugDecisionResults(choices, null);
                }
                else
                {
                    AppendDebugDecisionMessage(" - Not found, continue with previous predictions (" + choices.Count + ")");

                }
            }
            AppendDebugDecisionMessage("");



            // we will try to get predictions which are corresponding to a logical order.
            // it means we try to only keep predictions where the current split have
            // better SoFs and the next split
            // it is the most important filter
            if (splitNumber > 1)
            {
                AppendDebugDecisionMessage("[?] Try to get predictions where :");
                AppendDebugDecisionMessage("    Diff between Min SoF in current split and Max SoF of in next split is less than 2%.");
                AppendDebugDecisionMessage("    or");
                AppendDebugDecisionMessage("    Global SoF can be tester with MaxSofFunct (higher than MaxSofFunctStartingIRValue)");
                AppendDebugDecisionMessage("    or");
                AppendDebugDecisionMessage("    The Max SoF of the split is not the most Populated class");

                filter = (from r in choices
                          where
                          r.CurrentSplit.GlobalSof > ParameterMaxSofFunctStartingIRValue
                          ||
                          r.DiffBetweenMinCurrentSplitSofAndMaxNextSplitSof <= 2
                          ||
                          r.MostPopulatedClassIsTheMaxSox == false
                          select r).ToList();
                if (filter.Count > 0)
                {
                    choices = filter;
                    AppendDebugDecisionMessage(" - Found (" + choices.Count + ")");
                    AppendDebugDecisionResults(choices, null);
                }
                else
                {
                    AppendDebugDecisionMessage(" - Not found, continue with previous predictions (" + choices.Count + ")");

                }
                AppendDebugDecisionMessage("");
            }



            // we will try to filter predictions which have % SoF difference allowed
            // we will start from predictions containing all the classes
            // if no one match, we will have a look to predictions having 1 class less
            // etc
            bool matchDiff = FilterSofDiffLimit(ref choices, ref filter, remClassWithCars);


            /*
             * if set to 2, try first with 1, and than 0... but it is maybe not a good idea
            if (ParameterNoMiddleClassesEmptyValue == 2 && !matchDiff && noMiddleClassesFiltered)
            {
                // restart without 
                AppendDebugDecisionMessage(" - No predictions match the SoF Diff Limit with the NoMiddleClassesFilter, we will try without");
                AppendDebugDecisionMessage("");
                choices = predictionsBeforeMiddleClassesFiltere;
                FilterSofDiffLimit(ref choices, ref filter, remClassWithCars);
            }
            */



            // we just put the worst score to DiffBetweenClassesPercent for predictions
            // contaning only one class
            foreach (var choice in choices)
            {
                if (choice.NumberOfClasses == 1) choice.DiffBetweenClassesPercent = 999;
            }


            // we will order predictions per DiffBetweenClassesPercent asc
            AppendDebugDecisionMessage("[?] Sort remaining predictions with less Diff first");
            AppendDebugDecisionMessage("    First of the list will be the answer");
            AppendDebugDecisionMessage(" - Found (" + choices.Count + ")");
            choices = (from r in choices orderby r.DiffBetweenClassesPercent ascending select r).ToList();
            AppendDebugDecisionResults(choices, null);
            AppendDebugDecisionMessage("");

            // and we will keep the first one (with the lowest DiffBetweenClassesPercent)
            var bestpred = choices.First();

            // commit logs
            CommitDecisionsAppends(splitNumber);


            return bestpred;
        }

        private bool FilterSofDiffLimit(ref List<PredictionOfSplits> choices, ref List<PredictionOfSplits> filter, int remClassWithCars)
        {
            // we will try to filter predictions which have % SoF difference allowed
            // we will start from predictions containing all the classes
            // if no one match, we will have a look to predictions having 1 class less
            // etc

            bool matchDiff = false;
            for (int i = remClassWithCars; i >= 1; i--)
            {
                AppendDebugDecisionMessage("[?] Try to get predictions with " + i + " classes contains car");
                AppendDebugDecisionMessage("    and Diff is lower than MaxSof or MaxSofFunct");

                filter = (from r in choices where r.NumberOfClasses == i select r).ToList();
                if (filter.Count > 0)
                {

                    filter = (from r in filter where CheckLimit(r, r.DiffBetweenClassesPercent) select r).ToList();
                    if (filter.Count > 0)
                    {
                        choices = filter;
                        AppendDebugDecisionMessage(" - Found (" + choices.Count + ")");
                        AppendDebugDecisionResults(choices, null);
                        AppendDebugDecisionMessage("");
                        matchDiff = true;
                        break;
                    }
                    else
                    {
                        AppendDebugDecisionMessage(" - Not found (higher than allowed diff), continue with previous predictions (" + choices.Count + ")");
                        AppendDebugDecisionMessage("");

                    }
                }
                else
                {
                    AppendDebugDecisionMessage(" - Not found (not with " + i + " classes), continue with previous predictions (" + choices.Count + ")");
                    AppendDebugDecisionMessage("");
                }
            }

            return matchDiff;
        }


        /// <summary>
        /// This methods will check Max allows Sof %
        /// using the 2 possibilities :
        /// The ParameterMaxSofDiffValue
        /// or, if split SoF can be compared with the ParameterMaxSofFunct,
        /// it will use the relative value
        /// </summary>
        /// <param name="prediction"></param>
        /// <param name="diff"></param>
        /// <returns></returns>
        public bool CheckLimit(Data.PredictionOfSplits prediction, double diff)
        {

            //double splitSof = prediction.CurrentSplit.GlobalSof;
            int splitSof = prediction.CurrentSplit.GetMinClassSof();
            AppendDebugDecisionMessage("");
            AppendDebugDecisionMessage("       - Prediction " + prediction.Id + " has a Min SoF of " + splitSof);

            double maxDiff = ParameterMaxSofDiffValue;
            if (splitSof > ParameterMaxSofFunctStartingIRValue)
            {
                maxDiff = SofDifferenceEvaluator.EvalFormula(ParameterMaxSofFunctStartingIRValue,
                    ParameterMaxSofFunctStartingThreshold,
                    ParameterMaxSofFunctExtraThresoldPerK,
                    splitSof);
                AppendDebugDecisionMessage("       Max allowed Diff is " + maxDiff + " (corresponding to MaxSofDiffValueFunction)");
            }
            else
            {
                AppendDebugDecisionMessage("       Max allowed Diff is " + maxDiff + " (corresponding to MaxSofDiffValue)");
            }
            bool result = diff < maxDiff;
            if (result)
            {
                AppendDebugDecisionMessage("       => OK. (under the limit)");
            }
            else
            {
                AppendDebugDecisionMessage("       => Not Ok. (higher than the limit)");
            }
            AppendDebugDecisionMessage("");
            return result;
        }

    }
}
