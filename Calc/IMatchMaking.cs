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
        bool UseParameterMaxSofDiff { get;  }
        bool UseParameterTopSplitException { get;  }

       

        int ParameterPValue { get; set; }
        int ParameterIRValue { get; set; }
        int ParameterMaxSofDiff { get; set; }
        int ParameterMaxSofFunctA { get; set; }
        int ParameterMaxSofFunctB { get; set; }
        int ParameterMaxSofFunctX { get; set; }
        int ParameterTopSplitException { get; set; }



        void Compute(List<Data.Line> data, int fieldSize);
    }
}
