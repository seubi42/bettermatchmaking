using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Data;

namespace BetterMatchMaking.Calc
{
    public class DoubleClassicMatchMaking : IMatchMaking
    {
        public List<Split> Splits { get; private set; }

        IMatchMaking c;


        internal virtual int GetiRatingLimit()
        {
            return 1750;
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
            c.Compute(moreThanLimit, fieldSize);
            Splits = c.Splits;

            // less than limit split calculation
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
