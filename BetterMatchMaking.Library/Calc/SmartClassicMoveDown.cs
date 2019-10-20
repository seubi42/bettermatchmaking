using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Calc
{
    public class SmartClassicMoveDown : SmartProportionnalMoveDown
    {
        public override bool UseParameterP
        {
            get
            {
                return false;
            }
        }

        internal override ITakeCarsProportionCalculator GetFirstPassCalculator()
        {
            return new ClassicMatchMaking();
        }
    }
}
