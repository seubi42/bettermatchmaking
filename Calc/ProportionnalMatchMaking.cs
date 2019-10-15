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

            double allTotalCars = (from r in classRemainingCars select r.Value).Sum();

            Dictionary<int, double> classRatio = new Dictionary<int, double>();
            foreach (var carclass in carsListPerClass)
            {
                double classTotalCars = Convert.ToDouble(classRemainingCars[carclass.CarClassId]);


                double rat = classTotalCars / allTotalCars;
                classRatio.Add(carclass.CarClassId, rat);

            }

            double maxRatio = (from r in classRatio select r.Value).Max();
            double minRatio = (from r in classRatio select r.Value).Min();
            int maxClass = (from r in classRatio orderby r.Value descending select r.Key).FirstOrDefault();


            foreach (var carclass in carsListPerClass)
            {
                classRatio[carclass.CarClassId] *= fieldSize;
            }


            double sum = (from r in classRatio select Math.Floor(r.Value)).Sum();
            while (sum < fieldSize)
            {
                int classtoround = (from r in classRatio orderby r.Value - Math.Floor(r.Value) descending select r.Key).FirstOrDefault();
                classRatio[classtoround] = Math.Floor(classRatio[classtoround]) + 1;
                sum = (from r in classRatio select Math.Floor(r.Value)).Sum();
            }

            return Convert.ToInt32(Math.Floor(classRatio[classid]));
        }
    }
}
