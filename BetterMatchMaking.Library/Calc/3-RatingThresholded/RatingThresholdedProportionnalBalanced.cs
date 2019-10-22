using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Library.Data;

namespace BetterMatchMaking.Library.Calc
{
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
