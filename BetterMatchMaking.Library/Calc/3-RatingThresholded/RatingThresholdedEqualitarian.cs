using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Library.Data;

namespace BetterMatchMaking.Library.Calc
{
    public class RatingThresholdedEqualitarian : IMatchMaking
    {
        #region Active Parameters
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
        public bool UseParameterMaxSofDiff
        {
            get { return false; }

        }
        public bool UseParameterTopSplitException
        {
            get { return false; }

        }
        public bool UseParameterMostPopulatedClassInEverySplits
        {
            get { return false; }
        }
        public int ParameterMostPopulatedClassInEverySplitsValue { get; set; }
        public int ParameterClassPropMinPercentValue { get; set; }

        public int ParameterMaxSofDiffValue { get; set; }
        public int ParameterMaxSofFunctAValue { get; set; }
        public int ParameterMaxSofFunctBValue { get; set; }
        public int ParameterMaxSofFunctXValue { get; set; }
        public int ParameterTopSplitExceptionValue { get; set; }
        #endregion



        public List<Split> Splits { get; private set; }

        IMatchMaking c;


        internal virtual int GetiRatingLimit()
        {
            return ParameterRatingThresholdValue;
        }


        internal virtual IMatchMaking GetGroupMatchMaker()
        {
            return new ClassicEqualitarian();
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
            var moreThanLimit = (from r in data orderby r.rating descending select r).Take(moreThanLimitCars).ToList();
            
            var lessThanLimit = new List<Line>();
            foreach (var line in data)
            {
                if (!moreThanLimit.Contains(line)) lessThanLimit.Add(line);
            }


            // compute both list separatly

            // more than limit split calculation
            c = GetGroupMatchMaker();
            BetterMatchMaking.Library.BetterMatchMakingCalculator.CopyParameters(this, c);
            c.Compute(moreThanLimit, fieldSize);
            Splits = c.Splits;

            // less than limit split calculation
            double approxSplitsCount = Convert.ToDouble(lessThanLimit.Count) / fieldSize;
            double newFieldSize = Convert.ToDouble(lessThanLimit.Count) / Math.Ceiling(approxSplitsCount);
            fieldSize = Convert.ToInt32(Math.Ceiling(newFieldSize));

            c = GetGroupMatchMaker();
            BetterMatchMaking.Library.BetterMatchMakingCalculator.CopyParameters(this, c);
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
