using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Data;

namespace BetterMatchMaking.Calc
{
    public class BasicMatchMaking : IMatchMaking
    {
        public List<Split> Splits { get; private set; }

        public void Compute(List<Line> data, int fieldSize)
        {
            // get true field size
            int maxFieldSize = fieldSize;
            int splits = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(data.Count) / Convert.ToDouble(fieldSize)));
            int minFieldSize = data.Count / splits;
            int moreCarsOnTopSplits = data.Count - (splits * minFieldSize);
            fieldSize = minFieldSize;

            // -->


            


            Splits = new List<Split>();

            var carsListPerClass = Tools.SplitCarsPerClass(data);
            List<int> carClassesList = (from r in carsListPerClass select r.CarClassId).ToList();
            int avgFieldSizePerClass = maxFieldSize / carClassesList.Count;

            // calculer une moyenne de voiture par split et class
            // le truc a trouver c'est quand ca change de nombre de classes
            // <from split, to split>
            // classes count

            

            carsListPerClass = Tools.SplitCarsPerClass(data);


            int splitCounter = 1;
            while (Tools.CountRemainingCars(carsListPerClass) > 0)
            {

                Split split = new Split(splitCounter);


                carsListPerClass = (from r in carsListPerClass
                                    where r.Cars.Count > 0

                                    select r).ToList();

                double divisor = 0;
                foreach (var carclass in carsListPerClass)
                {
                    int avgClassFieldSize = fieldSize / carsListPerClass.Count;
                    double d = Convert.ToDouble(carclass.Cars.Count) / Convert.ToDouble(avgClassFieldSize);
                    if (d >= 1) d = 1;
                    else
                    {

                    }

                    divisor += d;
                }

                int carsCountPerSplit = Convert.ToInt32(Math.Floor(Convert.ToDouble(fieldSize) / divisor));



                foreach (var carclass in carsListPerClass)
                {
                    var carsInSplitClass = carclass.GetCars(carsCountPerSplit);


                    if (carsInSplitClass.Count > 0)
                    {
                        var classIndex = carClassesList.IndexOf(carsInSplitClass[0].car_class_id);
                        split.SetClass(classIndex, carsInSplitClass, carsInSplitClass[0].car_class_id);
                    }

                    

                    
                }

                

                


                Splits.Add(split);
                splitCounter++;
            }
        }

         
    }
}
