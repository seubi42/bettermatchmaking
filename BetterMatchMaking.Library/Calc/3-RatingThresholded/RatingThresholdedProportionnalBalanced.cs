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
        public override bool UseParameterClassPropMinPercent
        {
            get { return true; }
        }

        internal override IMatchMaking GetGroupMatchMaker()
        {
            return new ClassicProportionnalBalanced();
        }


    }
}
