using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Library.Data;

namespace BetterMatchMaking.Library.Calc
{
    public class RuledMatchMaking : ClassicMatchMaking, ITakeCarsProportionCalculator
    {
        List<int> carClassesIds;

        public void Init(List<int> classesid)
        {
            carClassesIds = classesid;
        }


        public override int TakeClassCars(int fieldSize, int remCarClasses, Dictionary<int, int> classRemainingCars, int classid, List<ClassCarsQueue> carsListPerClass, int split)
        {

            // init carClassesId if not
            if(carClassesIds == null)
            {
                var classesId = (from r in classRemainingCars select r.Key).ToList();
                Init(classesId);
            }
            // -->

            var classesIdWithRemaningCars = (from r in classRemainingCars where r.Value > 0 select r.Key).ToList();
            int take = TakeCars(classid, classesIdWithRemaningCars, fieldSize);



            return take;
        }

        private int TakeCars(int classId, List<int> classes, int fieldSizeOrLimit)
        {
            
            List<double> classesPercent = new List<double>();

            if (carClassesIds.Count == 4)
            {
                // configuration with 4 classes has to be tested
                classesPercent.Add(0.15d);
                classesPercent.Add(0.20d);
                classesPercent.Add(0.25d);
                classesPercent.Add(0.30d);
            }
            else if (carClassesIds.Count == 3)
            {
                classesPercent.Add(0.26d);
                classesPercent.Add(0.29d);
                classesPercent.Add(0.45d);                
            }
            else if (carClassesIds.Count == 2)
            {
                classesPercent.Add(0.40d);
                classesPercent.Add(0.60d);
                
            }
            else if (carClassesIds.Count == 1)
            {
                classesPercent.Add(1.0d);
            }

            double pToGet = 0;
            double pCoef = 0;


            foreach (var classToGetId in classes)
            {
                int classToGetIndex = carClassesIds.IndexOf(classToGetId);

                if (classId == classToGetId)
                {
                    pToGet += classesPercent[classToGetIndex];
                }

                pCoef += classesPercent[classToGetIndex];
            }

            double coef = (pToGet / pCoef);
            double result = Convert.ToDouble(fieldSizeOrLimit) * coef ;


            return Convert.ToInt32(result);

        }

    }
}
