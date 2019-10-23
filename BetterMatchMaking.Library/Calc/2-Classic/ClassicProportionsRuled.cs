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
    /// The TakeClassCars method implement a fixed rule to get a nice distribution between classes.
    /// For three classes, the distribution is : 26% / 29% / 45%
    /// </summary>
    public class ClassicProportionsRuled : ClassicEqualitarian, ITakeCarsProportionCalculator
    {
        #region Initialization
        // classes in the race (for every splits)
        List<int> carClassesIds; 

        public void Init(List<int> classesid)
        {
            carClassesIds = classesid;
        }
        #endregion

        /// <summary>
        /// The Main idea of this algorithm.
        /// Return car number depedent to % rules witch gives a good repartition.
        /// 
        /// This rule is hardcoded, maybe one day it will be fine to describe it
        /// in parameters but anyway...
        /// </summary>
        /// <param name="classId">class id you need cars</param>
        /// <param name="classes">list of classes you have in your split (the Ids)</param>
        /// <param name="fieldSizeOrLimit">how many maximum avaible slot to fill (for all classes)</param>
        /// <returns></returns>
        private int TakeCars(int classId, List<int> classes, int fieldSizeOrLimit)
        {
            List<double> classesPercent = new List<double>();

            // carClassesIds contains classes ids which are in the race
            // globally (on all splits, not just this one)
            
            // the rules
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
            // -->


            // rule of three process
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
            double result = Convert.ToDouble(fieldSizeOrLimit) * coef;
            // -->

            return Convert.ToInt32(result);

        }


        public override int TakeClassCars(int fieldSize, int remCarClasses, Dictionary<int, int> classRemainingCars, int classid, List<ClassCarsQueue> carsListPerClass, int split)
        {
            // Automatic Initialization : init carClassesId if not
            if (carClassesIds == null)
            {
                var classesId = (from r in classRemainingCars select r.Key).ToList();
                Init(classesId);
            }
            // -->
            
            //get classes with remaining cars
            var classesIdWithRemaningCars = (from r in classRemainingCars where r.Value > 0 select r.Key).ToList();

            return TakeCars(classid, classesIdWithRemaningCars, fieldSize);
        }

        

    }
}
