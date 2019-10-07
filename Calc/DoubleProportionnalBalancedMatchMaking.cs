using BetterMatchMaking.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Calc
{
    class DoubleProportionnalBalancedMatchMaking : DoubleClassicMatchMaking
    {
        internal override IMatchMaking GetGroupMatchMaker()
        {
            return new ProportionnalBalancedMatchMaking();
        }


    }
}
