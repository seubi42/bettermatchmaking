using BetterMatchMaking.Library.Calc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Data
{
    /// <summary>
    /// This class describe a Prediction (or a scenario) of two split :
    /// CurrentSplit : is the split we want to implement
    /// NextSplit : the following
    /// </summary>
    class PredictionOfSplits
    {


        public Split CurrentSplit { get; set; }
        public Split NextSplit { get; set; }
        


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

        public List<int> ClassesCuttedAroundRatingThreshold { get; set; }
        #endregion


        #region Id (to help debugging)
        string _id;
        public string Id
        {
            get
            {
                if (_id == null)
                {
                    _id = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                }
                return _id;
            }
        }

        #endregion


        /// <summary>
        /// Calc all the statistics needed to take the right decision
        /// </summary>
        /// <param name="prevSplitMaxSof"></param>
        public void CalcStats(int prevSplitMaxSof)
        {
            RatingDiffPerClassPoints = new Dictionary<int, int>();
            RatingDiffPerClassPercent = new Dictionary<int, double>();
            ClassesCuttedAroundRatingThreshold = new List<int>();

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
                double max = (from r in cars select r.rating).Max();

                RatingDiffPerClassPoints.Add(classId, Convert.ToInt32(max - min));
                RatingDiffPerClassPercent.Add(classId, Calc.SofDifferenceEvaluator.CalcDiff(min, max));
            }

            DiffBetweenMinCurrentSplitSofAndMaxNextSplitSof = Calc.SofDifferenceEvaluator.CalcDiff(CurrentSplit.GetMinClassSof(), NextSplit.GetMaxClassSof(), false);
            //if (DiffBetweenMinCurrentSplitSofAndMaxNextSplitSof < 0) DiffBetweenMinCurrentSplitSofAndMaxNextSplitSof = 999;

            int mostPopSof = CurrentSplit.GetClassSof(CurrentSplit.GetLastClassIndex());
            MostPopulatedClassIsTheMaxSox = mostPopSof == CurrentSplit.GetMaxClassSof();

        }



        /// <summary>
        /// Calc 
        /// </summary>
        /// <param name="ratingthreshold"></param>
        /// <param name="prevSplitMaxSof"></param>
        /// <returns></returns>
        public List<PredictionOfSplits> CuttedVariation(int ratingthreshold, int prevSplitMaxSof, int mincars)
        {
            List<PredictionOfSplits> ret = new List<PredictionOfSplits>();
            foreach (var classDif in RatingDiffPerClassPercent)
            {
                if(classDif.Value > 50)
                {
                    int classIndex = CurrentSplit.GetClassIndexOfId(classDif.Key);
                    int mostPopClassIndex = NextSplit.GetLastClassIndex();
                    if (classIndex != mostPopClassIndex)
                    {

                        var classcars = CurrentSplit.GetClassCars(classIndex);
                        var firstcarRating = classcars.First().rating;
                        var lastcarRating = classcars.Last().rating;



                        if (lastcarRating < ratingthreshold && firstcarRating > ratingthreshold * 0.6d)
                        {
                            PredictionOfSplits alternative = new PredictionOfSplits();
                            
                            alternative.CurrentSplit = Data.Tools.SplitCloner(CurrentSplit);
                            alternative.NextSplit = Data.Tools.SplitCloner(NextSplit);
                            int cars = alternative.CurrentSplit.CountClassCars(classIndex);

                            if (cars >= mincars*2)
                            {
                                int carsToMove = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(cars) / 2d));
                                if (carsToMove >= mincars)
                                {

                                    // cut this class and  move down half
                                    var pick = alternative.CurrentSplit.PickClassCars(classIndex, carsToMove, true);
                                    if (alternative.NextSplit.GetClassId(classIndex) == 0)
                                    {
                                        alternative.NextSplit.SetClass(classIndex, classDif.Key);
                                    }
                                    alternative.NextSplit.AppendClassCars(classIndex, pick);

                                    // fill available slots with most pop
                                    pick = alternative.NextSplit.PickClassCars(mostPopClassIndex, carsToMove, false);
                                    alternative.CurrentSplit.AppendClassCars(mostPopClassIndex, pick);

                                    alternative.CalcStats(prevSplitMaxSof);
                                    ClassesCuttedAroundRatingThreshold.Add(classDif.Key);
                                    ret.Add(alternative);
                                }
                            }
                        }
                    }
                }
            }
            return ret;
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
