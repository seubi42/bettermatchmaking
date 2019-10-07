using BetterMatchMaking.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Calc
{
    public class ProportionnalMatchMaking : ClassicMatchMaking
    {


        internal override int TakeClassCars(int fieldSize, int remCarClasses,
            Dictionary<int, int> classRemainingCars, int classid,
            List<CarsPerClass> carsListPerClass, int split)
        {


            double classTotalCars = (from r in carsListPerClass where r.CarClassId == classid select r.Cars.Count).Sum();
            double allTotalCars = (from r in carsListPerClass select r.Cars.Count).Sum();

            double ratio = classTotalCars / allTotalCars;

            double x = Convert.ToDouble(base.TakeClassCars(fieldSize, remCarClasses, classRemainingCars, classid, carsListPerClass, split));
            
            x *= ratio;
            
            x = Math.Ceiling(x);

            return Convert.ToInt32(x);
        }
    }
}
