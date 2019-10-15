using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Calc
{
    public interface IMatchMaking
    {
        List<Data.Split> Splits {get;}

        bool UseParameterP { get;  }
        bool UseParameterIR { get;  }

        int ParameterPValue { get; set; }
        int ParameterIRValue { get; set; }



        void Compute(List<Data.Line> data, int fieldSize);
    }
}
