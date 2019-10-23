// Better Splits Project - https://board.ipitting.com/bettersplits
// Written by Sebastien Mallet (seubiracing@gmail.com - iRacer #281664)
// --------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Data
{
    /// <summary>
    /// Audit. To make quality tests on algorithm results.
    /// </summary>
    public class Audit
    {
        /// <summary>
        /// Success : if false, the algorithm does not works as intended.
        /// A critical error is in it.
        ///
        /// 
        /// For exemple: 
        /// - all the cars have not been set to a split.
        /// - a split have more cars than the fieldsize limit
        /// - ...
        /// 
        /// Usefull for debugging.
        /// Of course, after debugging, it should never happens :-)
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Splits which have more cars than the fieldsize limit error.
        /// Have to be empty.
        /// </summary>
        public List<int> SplitsExceedsFieldSize { get; set; }

        /// <summary>
        /// Cars Ids which are missing, and which are not in any split.
        /// Have to be empty.
        /// </summary>
        public List<int> CarsMissingInAnySplit { get; set; }

        /// <summary>
        /// Unknown and unexpected Cars Id are in a split and we don't know why.
        /// Have to be empty
        /// </summary>
        public List<int> NotExpectedCarsRegistred { get; set; }

        /// <summary>
        /// Time requires (in ms) to compute the algorithm
        /// </summary>
        public int ComputingTimeInMs { get; set; }

        /// <summary>
        /// Sum of the number of cars in every splits
        /// </summary>
        public int Cars { get; set; }

        /// <summary>
        /// How many splits have been created
        /// </summary>
        public int Splits { get; set; }

        /// <summary>
        /// Average value of ClassesSofDifference for every splits.
        /// It takes, for each split, 'min class SoF' and 'max class SoF'
        /// And make average value of all splits.
        /// </summary>
        public int AverageSplitClassesSofDifference { get; set; }

        /// <summary>
        /// If a split have less cars than the other.
        /// It tell the % car number between the split having less (min) and the split having more (max)
        /// </summary>
        public double MinSplitSizePercent { get; set; }

        /// <summary>
        /// If a higher iRacing is in lower split.
        /// Than it will record on this list the number of the starting split.
        /// 
        /// The exmple if Split 2 contains driver for [3000 - 2000],
        /// split 3 will only have driver less than 2000
        /// it not (if there is a 2500 driver in split 3 for no reason)
        /// than a '2' (for split 2) will be recorded in the list.
        /// 
        /// Have to always be empty.
        /// </summary>
        public List<int> IROrderInconsistencySplits { get; set; }


        /// <summary>
        /// Exports all errors and quality stats about that split.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (Success) sb.AppendLine("SUCCESS");
            else sb.AppendLine("ERROR");

            if(SplitsExceedsFieldSize.Count > 0)
            {
                sb.AppendLine("SplitsExceedsFieldSize : " + Contact(SplitsExceedsFieldSize));
            }
            if (CarsMissingInAnySplit.Count > 0)
            {
                sb.AppendLine("CarsMissingInAnySplit : " + Contact(CarsMissingInAnySplit));
            }
            if (NotExpectedCarsRegistred.Count > 0)
            {
                sb.AppendLine("NotExpectedCarsRegistred : " + Contact(NotExpectedCarsRegistred));
            }
            if (IROrderInconsistencySplits.Count > 0)
            {
                sb.AppendLine("IROrderInconsistencySplits : " + Contact(IROrderInconsistencySplits));
            }

            string ret = sb.ToString();
            ret = ret.Trim();
            return ret;
        }


        /// <summary>
        /// Little helper to concat int list like {1, 2, 3}
        /// to a string like "1, 2, 3".
        /// </summary>
        /// <param name="li">number list to concat, like {1, 2, 3}</param>
        /// <returns>concatenated string like "1, 2, 3".</returns>
        static string Contact(List<int> li)
        {
            string ret = "";
            for (int i = 0; i < li.Count; i++)
            {
                ret += li[i];
                if (i < li.Count - 1) ret += ", ";
            }
            return ret;
        }
    }
}
