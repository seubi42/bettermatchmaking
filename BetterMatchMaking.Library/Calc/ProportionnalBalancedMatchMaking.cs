using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Library.Data;

namespace BetterMatchMaking.Library.Calc
{
    public class ProportionnalBalancedMatchMaking : ClassicMatchMaking, ITakeCarsProportionCalculator
    {
        // parameters
        public override bool UseParameterP
        {
            get { return true; }
        }
        // -->



        public override int TakeClassCars(int fieldSize, int remCarClasses,
            Dictionary<int, int> classRemainingCars, int classid,
            List<CarsPerClass> carsListPerClass, int split)
        {

            List<int> availableClasses = (from r in classRemainingCars where r.Value > 0 select r.Key).ToList();

            double limit = Convert.ToDouble(ParameterPValue) / 100d;


            double allTotalCars = (from r in classRemainingCars select r.Value).Sum();

            Dictionary<int, double> classRatio = new Dictionary<int, double>();
            foreach (var carclass in carsListPerClass)
            {
                if (classRemainingCars.ContainsKey(carclass.CarClassId))
                {
                    double classTotalCars = Convert.ToDouble(classRemainingCars[carclass.CarClassId]);


                    double rat = classTotalCars / allTotalCars;
                    classRatio.Add(carclass.CarClassId, rat);
                }
            }

            if (classRatio.Count == 0) return 0;
            double maxRatio = (from r in classRatio select r.Value).Max();
            double minRatio = (from r in classRatio select r.Value).Min();
            int maxClass = (from r in classRatio orderby r.Value descending select r.Key).FirstOrDefault();


            foreach (var carclass in carsListPerClass)
            {
                if (classRemainingCars.ContainsKey(carclass.CarClassId))
                {
                    if (classRatio[carclass.CarClassId] < limit * maxRatio)
                    {
                        double toAdd = limit * maxRatio - classRatio[carclass.CarClassId];

                        // MODIF
                        double multiplicator = 0;
                        foreach (var cr in classRatio)
                        {
                            if (availableClasses.Contains(cr.Key))
                            {
                                multiplicator += cr.Value;
                            }
                        }
                        //toAdd /= multiplicator;
                        // -->


                        classRatio[carclass.CarClassId] += toAdd;
                        classRatio[maxClass] -= toAdd;
                    }
                }
            }

            foreach (var carclass in carsListPerClass)
            {
                if (classRatio.ContainsKey(carclass.CarClassId))
                {
                    classRatio[carclass.CarClassId] *= fieldSize;
                }
            }


            double sum = (from r in classRatio select Math.Floor(r.Value)).Sum();
            while (sum < fieldSize)
            {
                int classtoround = (from r in classRatio orderby r.Value - Math.Floor(r.Value) descending select r.Key).FirstOrDefault();
                classRatio[classtoround] = Math.Floor(classRatio[classtoround]) + 1;
                sum = (from r in classRatio select Math.Floor(r.Value)).Sum();
            }

            if (!classRatio.ContainsKey(classid)) return 0;

            return Convert.ToInt32(Math.Floor(classRatio[classid]));




        }


    }
}


