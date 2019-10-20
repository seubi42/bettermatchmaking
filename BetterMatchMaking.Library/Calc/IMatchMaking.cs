using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Calc
{
    public interface IMatchMaking
    {
        List<Data.Split> Splits {get;}

        bool UseParameterP { get;  }
        bool UseParameterIR { get;  }
        bool UseParameterMaxSofDiff { get;  }
        bool UseParameterTopSplitException { get;  }
        bool UseParameterEqualizeSplits { get;  }

       

        int ParameterPValue { get; set; }
        int ParameterIRValue { get; set; }
        int ParameterMaxSofDiff { get; set; }
        int ParameterMaxSofFunctA { get; set; }
        int ParameterMaxSofFunctB { get; set; }
        int ParameterMaxSofFunctX { get; set; }
        int ParameterTopSplitException { get; set; }
        int ParameterEqualizeSplits { get; set; }



        void Compute(List<Data.Line> data, int fieldSize);
    }

    public interface ITakeCarsProportionCalculator
    {
        int TakeClassCars(int fieldSize, int remCarClasses,
            Dictionary<int, int> classRemainingCars, int classid,
            List<Data.CarsPerClass> carsListPerClass, int split);
    }
}
