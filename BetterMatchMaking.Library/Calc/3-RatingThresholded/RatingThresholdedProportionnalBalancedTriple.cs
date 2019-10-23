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

    /// This algorithm override the TakeClassCars cuts the entry list in three parts (1, 2 and 3) :
    ///  0- Cars with iR more than 'ParameterRatingThresholdValue' are re-splitted to
    ///         1- First half, more than the median point of 0
    ///         2- Second half, less than the median point of 0
    ///  3- Cars with iR less than 'ParameterRatingThresholdValue'
    ///  
    /// - Then the ClassicEqualitarian algorithm is used to split each
    /// list.
    /// 
    /// - Then both list are combined together to get a bit list
    /// in two parts.
    public class RatingThresholdedProportionnalBalancedTriple : IMatchMaking
    {
        #region Active Parameters
        public bool UseParameterClassPropMinPercent
        {
            get { return true; }
        }
        public int ParameterClassPropMinPercentValue { get; set; }
        public bool UseParameterRatingThreshold
        {
            get { return true; }
        }
        public int ParameterRatingThresholdValue { get; set; }
        #endregion

        #region Disabled Parameters
        public bool UseParameterTopSplitException
        {
            get { return false; }
        }
        public int ParameterTopSplitExceptionValue { get; set; }

        public bool UseParameterMaxSofDiff
        {
            get { return false; }
        }
        public int ParameterMaxSofDiffValue { get; set; }
        public int ParameterMaxSofFunctAValue { get; set; }
        public int ParameterMaxSofFunctBValue { get; set; }
        public int ParameterMaxSofFunctXValue { get; set; }



        public bool UseParameterMostPopulatedClassInEverySplits
        {
            get { return false; }
        }
        public int ParameterMostPopulatedClassInEverySplitsValue { get; set; }


        public virtual bool UseParameterMinCars
        {
            get { return false; }
        }

        public int ParameterMinCarsValue { get; set; }
        #endregion


        public List<Split> Splits { get; private set; }


        IMatchMaking c; // this algorithm will use other ones


        internal virtual int GetiRatingLimit()
        {
            return ParameterRatingThresholdValue;
        }


        internal virtual IMatchMaking GetGroupMatchMaker()
        {
            var c= new ClassicProportionnalBalanced();
            BetterMatchMaking.Library.BetterMatchMakingCalculator.CopyParameters(this, c);
            return c;
        }


        public void Compute(List<Line> data, int fieldSize)
        {
            int totalcount = data.Count;

            int limit = GetiRatingLimit();

            // count the cars registrated with an irating upper than the limit
            int moreThanLimitCars = (from r in data where r.rating > limit select r).Count();
            int moreThanLimitSplits = Convert.ToInt32(
                Math.Floor(
                    Convert.ToDouble(moreThanLimitCars) / Convert.ToDouble(fieldSize)
                    )
                );
            // and round it to be a multiple of field size
            moreThanLimitCars = moreThanLimitSplits * fieldSize;


            // create two lists : moreThanLimit and lessThanLimit
            var moreThanLimit2 = (from r in data orderby r.rating descending select r).Take(moreThanLimitCars).ToList();

            var lessThanLimit = new List<Line>();
            foreach (var line in data)
            {
                if (!moreThanLimit2.Contains(line)) lessThanLimit.Add(line);
            }


            // now we when to cut the moreThanLimit2 in 2 parts, by the middle
            int middlevalue = moreThanLimit2[moreThanLimit2.Count / 2].rating;
            moreThanLimitCars = (from r in data where r.rating >= middlevalue select r).Count();
            moreThanLimitSplits = Convert.ToInt32(
                Math.Floor(
                    Convert.ToDouble(moreThanLimitCars) / Convert.ToDouble(fieldSize)
                    )
                );
            moreThanLimitCars = moreThanLimitSplits * fieldSize;
            var moreThanLimit1 = (from r in data orderby r.rating descending select r).Take(moreThanLimitCars).ToList();


            moreThanLimit2.Clear();
            foreach (var line in data)
            {
                if (!moreThanLimit1.Contains(line) && !lessThanLimit.Contains(line)) moreThanLimit2.Add(line);
            }


            // compute both list separatly
            int originalFieldSize = fieldSize;

            // more than limit 1 split calculation
            c = GetGroupMatchMaker();
            c.Compute(moreThanLimit1, fieldSize);
            Splits = c.Splits;

            // less than limit 2 split calculation
            double approxSplitsCount = Convert.ToDouble(moreThanLimit2.Count) / originalFieldSize;
            double newFieldSize = Convert.ToDouble(moreThanLimit2.Count) / Math.Ceiling(approxSplitsCount);
            fieldSize = Convert.ToInt32(Math.Ceiling(newFieldSize));

            c = GetGroupMatchMaker();
            c.Compute(moreThanLimit2, fieldSize);
            Splits.AddRange(c.Splits); // merge the two lists

            // less than limit split calculation
            approxSplitsCount = Convert.ToDouble(lessThanLimit.Count) / originalFieldSize;
            newFieldSize = Convert.ToDouble(lessThanLimit.Count) / Math.Ceiling(approxSplitsCount);
            fieldSize = Convert.ToInt32(Math.Ceiling(newFieldSize));

            c = GetGroupMatchMaker();
            c.Compute(lessThanLimit, fieldSize);
            Splits.AddRange(c.Splits); // merge the two lists

            // re count splits
            int counter = 1;
            foreach (var s in Splits)
            {
                s.Number = counter;
                counter++;
            }

        }




    }
}
