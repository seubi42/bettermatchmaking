// Better Splits Project - https://board.ipitting.com/bettersplits
// Written by Sebastien Mallet (seubiracing@gmail.com - iRacer #281664)
// --------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Calc
{

    /// <summary>
    /// This class help to get SoF differences
    /// and helper to test if this difference is
    /// allowed or not.
    /// </summary>
    public class SofDifferenceEvaluator
    {
        public Data.Split Split { get; private set; }
        public int ClassIndex { get; private set; }
        public bool Evaluated { get; private set; }
        public double PercentDifference { get; private set; }

        public bool PercentDifferenceUsesAffineFunction { get; private set; }

        public int ClassSof { get; private set; }
        public double MaxPercentDifferenceAllowed { get; private set; }
        public int MaxSofInSplit { get; private set; }
        


        public SofDifferenceEvaluator(Data.Split split, int classIndex)
        {
            split.RefreshSofs();
            this.Split = split;
            this.ClassIndex = classIndex;
            EvalDifference();
        }

        private void EvalDifference()
        {
            Evaluated = true;

            // get each class SoFs in this split
            // and keep min and max
            int classSof = 0;
            if (ClassIndex == -1)
            {
                classSof = Split.GlobalSof;
            }
            else
            {
                classSof = Split.GetClassSof(ClassIndex);
            }
            int min = classSof;
            MaxSofInSplit = Split.GetMaxClassSof(ClassIndex); // max of other classes
            if (MaxSofInSplit == 0) MaxSofInSplit = classSof;
            // -->

            // exit if 0
            if (min == 0 && MaxSofInSplit == 0)
            {
                Evaluated = false; // we can not eval that
                return;
            }
            // -->


            double referencesof = classSof;
            if (classSof == 0) referencesof = min;
            ClassSof = Convert.ToInt32(referencesof);
            // -->

            // difference in % between min and max
            
            double a = referencesof;
            double b = MaxSofInSplit;
            if (a == 0 || b == 0)
            {
                PercentDifference = 0;
                return;
            }


            int diff = 100 - Convert.ToInt32(Math.Round(100 * a / b));
            diff = Math.Max(diff, 0);
            PercentDifference = diff;
            // -->

        }


        /// <summary>
        /// Calc SoF difference (in points)
        /// </summary>
        /// <param name="d1">min sof</param>
        /// <param name="d2">max sof</param>
        /// <param name="abs">absolute value ?</param>
        /// <returns></returns>
        public static double CalcDiff(double d1, double d2, bool abs=true)
        {
            double a = d1;
            double b = d2;
            if (abs)
            {
                a = Math.Min(d1, d2);
                b = Math.Max(d1, d2);
            }
            double diff = 100 - (100 * a / b);

            return diff;
        }

        public bool MoreThanLimit(int maxSofDiffValue,
            int functStartingIr, int functStartingThreshold, int functExtraThresholdPenK)
        {
            if (!Evaluated) return false;


            // what is the allowed limit ?
            // read it from ParameterMaxSofDiffValue (constant value)
            MaxPercentDifferenceAllowed = maxSofDiffValue;
            //limit = moveDownPass; // it will be only the half on second pass



            // and it set, read it from the affine function
            if (functExtraThresholdPenK > 0 && ClassSof > functStartingIr)
            {
                if(MoreThanFunction(functStartingIr, functStartingThreshold, functExtraThresholdPenK))
                {
                    return true;
                }
            }

            return (PercentDifference >= MaxPercentDifferenceAllowed);
        }

        public bool MoreThanFunction(int functStartingIr, int functStartingThreshold, int functExtraThresholdPenK)
        {
            if (!Evaluated) return false;
            if (functExtraThresholdPenK > 0)
            {
                double newDiff = EvalFormula(functStartingIr, functStartingThreshold, functExtraThresholdPenK, ClassSof);
                if (newDiff > MaxPercentDifferenceAllowed)
                {
                    PercentDifferenceUsesAffineFunction = true;
                    MaxPercentDifferenceAllowed = Math.Max(newDiff, MaxPercentDifferenceAllowed);
                }
            }
            return (PercentDifference >= MaxPercentDifferenceAllowed);
        }


        /// <summary>
        /// This method will compute the Max % SoF difference we can allow on a split
        /// using 3 parameters:
        /// 
        /// startingIr : on how much Rating this formula starts
        /// startingThreshold : what is the starting % SoF difference threshold we allow when current iRating = startingIr
        /// extraThresholdPerKilo : how much % SoF difference car we allow more for each 1000ir points
        /// 
        /// Formula iss (( iRating - startingIr ) / 100 * extraThresholdPerKilo) + startingThreshold
        /// </summary>
        /// <param name="startingIr">on how much Rating this formula starts</param>
        /// <param name="startingThreshold">what is the starting % SoF difference threshold we allow at this
        /// startung point (when current startingThreshold == currentIR)</param>
        /// <param name="extraThresholdPerKilo">how much % SoF difference car we allow more for each 1000ir points</param>
        /// <param name="currentIR">the input Rating/SoF</param>
        /// <returns></returns>
        public static double EvalFormula(double startingIr, double startingThreshold, double extraThresholdPerKilo, int currentIR)
        {
            return Math.Round(Math.Max(0, (Convert.ToDouble(currentIR) - startingIr) / 1000 * extraThresholdPerKilo) + startingThreshold);
        }
    }
}
