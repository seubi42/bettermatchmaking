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
    /// This algorithm include inherits the RatingThresholdedProportionnalBalanced.
    /// Please see and tun the RatingThresholdedProportionnalBalanced algorithm first to understand the process.
    /// 
    /// Only the TakeClassCars proccess changes, for a different car class repartition 
    /// based on ClassicProportionnalBalanced algorithm. It cans add the
    ///  'RatingThresholdedProportionnalBalanced' parameter usage.
    public class RatingThresholdedProportionnalBalanced : RatingThresholdedEqualitarian
    {
        #region Active Parameters
        public override bool UseParameterClassPropMinPercent
        {
            get { return true; }
        }
        #endregion

        internal override IMatchMaking GetGroupMatchMaker()
        {
            // this algorithm will use the ClassicProportionnalBalanced algorithm
            // to use it TakeClassCars implementation
            return new ClassicProportionnalBalanced();
        }


    }
}
