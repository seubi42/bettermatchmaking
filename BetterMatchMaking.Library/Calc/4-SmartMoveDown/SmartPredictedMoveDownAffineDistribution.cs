﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Library.Data;

namespace BetterMatchMaking.Library.Calc
{ 
    public class SmartPredictedMoveDownAffineDistribution : SmartMoveDownAffineDistribution
    {
        #region Disabled Parameters
        public override bool UseParameterMaxSofDiff
        {
            get
            {
                return false;
            }
        }
        #endregion 


        internal override bool TestIfMoveDownNeeded(Split s, int classIndex, List<int> splitSofs)
        {
            if (s.Number == INTERRUPT_BEFORE_MOVEDOWN_SPLITNUMBER
                && classIndex == INTERRUPT_BEFORE_MOVEDOWN_CLASSINDEX)
            {
                // to help debugging, you can set breakpoint here
            }

            if (Tools.EnableDebugTraces) s.Info += "{";

            bool movedown = false;
            string debug; // to build debug information

            // eval the current situation (before any move)
            Calc.SofDifferenceEvaluator evaluator = new SofDifferenceEvaluator(s, classIndex);
            

            bool disableMoveDowns = false;

            // if the class SoF allows to start using the affine function to make an exception
            if(evaluator.ClassSof > ParameterMaxSofFunctStartingIRValue)
            {
                // eval the function
                bool moreThanLimit = evaluator.MoreThanFunction(ParameterMaxSofFunctStartingIRValue,
                    ParameterMaxSofFunctStartingThreshold,
                    ParameterMaxSofFunctExtraThresoldPerK);

                if (!moreThanLimit)
                {
                    // if the result its is lowest than the limit we are sure we
                    // don't want any move down
                    disableMoveDowns = true;
                }

                // debug informations
                if (Tools.EnableDebugTraces)
                {
                    debug = "(Δ:$REFSOF/$MAX=$DIFF,L:$LIMIT,$MOVEDOWN) ";
                    debug = debug.Replace("$REFSOF", evaluator.ClassSof.ToString());
                    debug = debug.Replace("$MAX", evaluator.MaxSofInSplit.ToString());
                    debug = debug.Replace("$DIFF", Convert.ToInt32(evaluator.PercentDifference).ToString());
                    debug = debug.Replace("$LIMIT", Convert.ToInt32(evaluator.MaxPercentDifferenceAllowed).ToString());
                    debug = debug.Replace("$MOVEDOWN", Convert.ToInt32(movedown).ToString());
                    s.Info += debug;
                }
                // -->

                
                
            }

            

            // stop the process
            if (disableMoveDowns)
            {
                if (Tools.EnableDebugTraces) s.Info += "}";
                return false;
            }



            // we will clone the splits list to make a fake move in it without commiting anything to the real "Splits" plist
            List<Data.Split> snapshotedSplits = Data.Tools.SplitsCloner(Splits, s.Number + 4);
            var snapshotedCurrentSplit = snapshotedSplits[s.Number - 1]; // our current split is here

            var exclusions = snapshotedCurrentSplit.GetEmptyClassesId();
            //ResetSplitWithAllClassesFilled(snapshotedSplits, snapshotedCurrentSplit);
            UpCarsToSplit(snapshotedSplits, snapshotedCurrentSplit, exclusions);
            evaluator = new SofDifferenceEvaluator(snapshotedCurrentSplit, classIndex);

            int classId = carClassesIds[classIndex]; // we need the classId
            // we call the MoveDownCarsSplits method to do the MoveDown
            MoveDownCarsSplits(snapshotedSplits, snapshotedCurrentSplit, snapshotedCurrentSplit.Number - 1, classIndex);

            // we now need to fill avaible slots on the snapshotedCurrentSplit 
            // but just one move instead of the whole recursive process
            var movedClass = new List<int>() { classId }; // the class we moved (we don't allow to fill it)

            // we get the next split (our car was moved into it)
            var snapshotedNextSplit = snapshotedSplits[s.Number];
            exclusions = snapshotedNextSplit.GetEmptyClassesId();
            // we will make a simple fill of available slots into it
            UpCarsToSplit(snapshotedSplits, snapshotedNextSplit, exclusions);
            //ResetSplitWithAllClassesFilled(snapshotedSplits, snapshotedNextSplit);
            

            // eval the new situation of the next split (after the fake move)
            Calc.SofDifferenceEvaluator evaluatorIfMovedDown = new SofDifferenceEvaluator(snapshotedNextSplit, classIndex);
            double predictedDiff = evaluatorIfMovedDown.PercentDifference;
            

            // compare both situation
            if (Math.Abs(predictedDiff) < Math.Abs(evaluator.PercentDifference))
            {
                // with the move, it is better so
                // we decide we have to do it
                movedown = true;
            }


            // debug informations
            if (Tools.EnableDebugTraces)
            {
                debug = "($BEFORE vs $AFTER;$MOVEDOWN) ";
                debug = debug.Replace("$BEFORE", Convert.ToInt32(evaluator.PercentDifference).ToString());
                debug = debug.Replace("$AFTER", Convert.ToInt32(predictedDiff).ToString());
                debug = debug.Replace("$MOVEDOWN", Convert.ToInt32(movedown).ToString());
                s.Info += debug;
            }
            // -->



            if (Tools.EnableDebugTraces) s.Info = s.Info.Trim() + "} ";
            return movedown;
        }
    }
}
