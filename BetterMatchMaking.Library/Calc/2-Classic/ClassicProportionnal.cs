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
    /// <summary>
    /// This algorithm override the TakeClassCars of the ClassicEqualitarian.
    /// Please see and tun the ClassicEqualitarian algorithm first to understand the process.
    /// 
    /// The TakeClassCars method will calculate the number of cars needed to
    /// get something 100% proportionnal to the entrylist proportions.
    /// </summary>
    public class ClassicProportionnal : ClassicEqualitarian, ITakeCarsProportionCalculator
    {


        public override int TakeClassCars(int fieldSize, int remCarClasses,
            Dictionary<int, int> classRemainingCars, int classid,
            List<ClassCarsQueue> carsListPerClass, int split)
        {
            // check if possible, it not return 0
            var possible = (from r in carsListPerClass where r.CarClassId == classid && r.Cars.Count > 0 select r).FirstOrDefault();
            if (possible == null)
            {
                return 0;
            }

            // count all the cars remaining
            double allTotalCars = (from r in classRemainingCars select r.Value).Sum();


            // create an array of class ratio 
            // KEY is the class id
            // VALUE is the % of the entrylist
            Dictionary<int, double> classRatio = new Dictionary<int, double>();
            foreach (var carclass in carsListPerClass)
            {
                if (classRemainingCars.ContainsKey(carclass.CarClassId))
                {
                    double classTotalCars = Convert.ToDouble(classRemainingCars[carclass.CarClassId]);
                    double rat = classTotalCars / allTotalCars;
                    classRatio.Add(carclass.CarClassId, rat);
                }
                else
                {
                    classRatio.Add(carclass.CarClassId, 0);
                }
            }

            if (classRatio.Count == 0) return 0;


            ConstraintProportions(classRatio, classRemainingCars, carsListPerClass); //



            // multiply the % ratio by the fieldsize give the number of cars to take
            // for each class
            foreach (var carclass in carsListPerClass)
            {
                classRatio[carclass.CarClassId] *= fieldSize;
            }

            // in case there is less cars left than the fieldsize (because of rounded values)
            // boost the class which the clostest to the Math.Floored value
            double sum = (from r in classRatio select Math.Floor(r.Value)).Sum();
            while (sum < fieldSize)
            {
                int classtoround = (from r in classRatio orderby r.Value - Math.Floor(r.Value) descending select r.Key).FirstOrDefault();
                classRatio[classtoround] = Math.Floor(classRatio[classtoround]) + 1;
                sum = (from r in classRatio select Math.Floor(r.Value)).Sum();
            }

            return Convert.ToInt32(Math.Floor(classRatio[classid]));
        }


        internal virtual void ConstraintProportions(Dictionary<int, double> classRatio, 
            Dictionary<int, int> classRemainingCars, 
            List<ClassCarsQueue> carsListPerClass)
        {
            // do nothing
        }
    }


}
