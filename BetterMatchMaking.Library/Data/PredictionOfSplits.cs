using BetterMatchMaking.Library.Calc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Data
{
    class PredictionOfSplits
    {
        public Split CurrentSplit { get; set; }
        public Split NextSplit { get; set; }

        public List<ClassDiffInPrediction> Differences { get; private set; }

        public double Score { get; private set; }
        public bool IsPossible { get; private set; }


        public void CalcDiff(int classIndex, int classId)
        {
            
            if (Differences == null) Differences = new List<ClassDiffInPrediction>();

            ClassDiffInPrediction diff = new ClassDiffInPrediction();


            Calc.SofDifferenceEvaluator eval;

            if (CurrentSplit.CountClassCars(classIndex) > 1)
            {
                    diff.ClassId = classId;
                    eval = new Calc.SofDifferenceEvaluator(CurrentSplit, classIndex);
                    diff.Diff = eval.PercentDifference;
                    diff.InSplit = CurrentSplit.Number;
                    diff.InCurrentSplit = true;
            }
            else if (NextSplit.CountClassCars(classIndex) > 1)
            {
                    diff.ClassId = classId;
                    eval = new Calc.SofDifferenceEvaluator(NextSplit, classIndex);
                    diff.Diff = eval.PercentDifference;
                    diff.InSplit = NextSplit.Number;
                    diff.InCurrentSplit = false;
            }
            



            Differences.Add(diff);
        }


        #region Statistics which can helps for decision

        public int NumberOfClasses { get; set; }
        public bool NoMiddleClassesMissing { get; set; }
        public int DiffBetweenClassesPoints { get; set; }
        public double DiffBetweenClassesPercent { get; set; }
        public bool AllSofsHigherThanNextSplitMax { get; set; }
        public bool AllSofsLowerThanPrevSplitMax { get; set; }
        public Dictionary<int, int> RatingDiffPerClassPoints { get; set; }
        public Dictionary<int, double> RatingDiffPerClassPercent { get; set; }
        public double DiffBetweenMinCurrentSplitSofAndMaxNextSplitSof { get; set; }
        public bool MostPopulatedClassIsTheMaxSox { get; set; }


        #endregion

        public void CalcStats(int prevSplitMaxSof)
        {
            RatingDiffPerClassPoints = new Dictionary<int, int>();
            RatingDiffPerClassPercent = new Dictionary<int, double>();

            NumberOfClasses = CurrentSplit.GetClassesCount();

            var indexes = CurrentSplit.GetClassesIndex();
            int maxIndex = indexes.Max();
            int minIndex = indexes.Min();
            int c = maxIndex - minIndex + 1;
            NoMiddleClassesMissing = NumberOfClasses == c;

            DiffBetweenClassesPoints = CurrentSplit.GetMaxClassSof() - CurrentSplit.GetMinClassSof();
            DiffBetweenClassesPercent = Calc.SofDifferenceEvaluator.CalcDiff(CurrentSplit.GetMaxClassSof(), CurrentSplit.GetMinClassSof());

            AllSofsHigherThanNextSplitMax = CurrentSplit.GetMinClassSof() > NextSplit.GetMaxClassSof();
            AllSofsLowerThanPrevSplitMax = CurrentSplit.GetMaxClassSof() > prevSplitMaxSof;


            var classesIndex = CurrentSplit.GetClassesIndex();
            foreach (int classIndex in classesIndex)
            {
                int classId = CurrentSplit.GetClassId(classIndex);
                var cars = CurrentSplit.GetClassCars(classIndex);
                double min = (from r in cars select r.rating).Min();
                double max = (from r in cars select r.rating).Min();

                RatingDiffPerClassPoints.Add(classId, Convert.ToInt32(max - min));
                RatingDiffPerClassPercent.Add(classId, Calc.SofDifferenceEvaluator.CalcDiff(min, max));
            }

            DiffBetweenMinCurrentSplitSofAndMaxNextSplitSof = Calc.SofDifferenceEvaluator.CalcDiff(CurrentSplit.GetMinClassSof(), NextSplit.GetMaxClassSof(), false);
            if (DiffBetweenMinCurrentSplitSofAndMaxNextSplitSof < 0) DiffBetweenMinCurrentSplitSofAndMaxNextSplitSof = 999;

            int mostPopSof = CurrentSplit.GetClassSof(CurrentSplit.GetLastClassIndex());
            MostPopulatedClassIsTheMaxSox = mostPopSof == CurrentSplit.GetMaxClassSof();

        }


        
        
    }

    class ClassDiffInPrediction
    {
        public double Diff { get; set; }
        public int InSplit { get; set; }
        public bool InCurrentSplit { get; set; }
        public int ClassId { get; set; }
    }
}
