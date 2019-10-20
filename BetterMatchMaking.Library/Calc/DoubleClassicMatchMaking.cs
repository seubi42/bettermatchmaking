using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Library.Data;

namespace BetterMatchMaking.Library.Calc
{
    public class DoubleClassicMatchMaking : IMatchMaking
    {
        // parameters
        public virtual bool UseParameterP
        {
            get { return false; }
        }
        public bool UseParameterIR
        {
            get { return true; }
        }

        public bool UseParameterMaxSofDiff
        {
            get { return false; }

        }
        public bool UseParameterTopSplitException
        {
            get { return false; }

        }
        public bool UseParameterEqualizeSplits
        {
            get { return false; }
        }
        public int ParameterEqualizeSplits { get; set; }
        public int ParameterPValue { get; set; }
        public int ParameterIRValue { get; set; }
        public int ParameterMaxSofDiff { get; set; }
        public int ParameterMaxSofFunctA { get; set; }
        public int ParameterMaxSofFunctB { get; set; }
        public int ParameterMaxSofFunctX { get; set; }
        public int ParameterTopSplitException { get; set; }
        // -->

        public List<Split> Splits { get; private set; }

        IMatchMaking c;


        internal virtual int GetiRatingLimit()
        {
            return ParameterIRValue;
        }


        internal virtual IMatchMaking GetGroupMatchMaker()
        {
            return new ClassicMatchMaking();
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
