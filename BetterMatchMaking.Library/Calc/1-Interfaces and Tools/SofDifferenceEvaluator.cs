using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Calc
{
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
            int classSof = Split.GetClassSof(ClassIndex);
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


            //double referencesof = (s.GlobalSof + max + classSof) / 3;
            double referencesof = classSof;
            if (classSof == 0) referencesof = min;
            ClassSof = Convert.ToInt32(referencesof);
            // -->

            // difference in % between min and max
            int diff = Convert.ToInt32(100 * Convert.ToInt32(referencesof) / MaxSofInSplit);
            diff = 100 - diff;
            diff = Math.Abs(diff);
            PercentDifference = diff;
            // -->

        }

        public bool MoreThanLimit(int maxSofDiffValue,
            int maxSoffDiffValueFx, int maxSoffDiffValueFa, int maxSoffDiffValueFb)
        {
            if (!Evaluated) return false;


            // what is the allowed limit ?
            // read it from ParameterMaxSofDiffValue (constant value)
            MaxPercentDifferenceAllowed = maxSofDiffValue;
            //limit = moveDownPass; // it will be only the half on second pass



            // and it set, read it from the affine function
            // f(rating) = (rating / X) * A) + b
            if (!(maxSoffDiffValueFx == 0 || maxSoffDiffValueFa == 0 || maxSoffDiffValueFb == 0))
            {
                double newDiff = ((Convert.ToDouble(ClassSof) / maxSoffDiffValueFx) * maxSoffDiffValueFa) + maxSoffDiffValueFb;
                if (newDiff > MaxPercentDifferenceAllowed)
                {
                    PercentDifferenceUsesAffineFunction = true;
                    MaxPercentDifferenceAllowed = Math.Max(newDiff, MaxPercentDifferenceAllowed);
                }
            }

            return (PercentDifference >= MaxPercentDifferenceAllowed);
        }
    }
}
