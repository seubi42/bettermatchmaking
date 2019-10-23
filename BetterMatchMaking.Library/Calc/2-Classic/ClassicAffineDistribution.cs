// Better Splits Project - https://board.ipitting.com/bettersplits
// Written by Sebastien Mallet (seubiracing@gmail.com - iRacer #281664)
// From an idea of Yannick Lapchin (iRacing #78137)
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
    /// The TakeClassCars method implement a idea of Yannick Lapchin to guaranty minimumcars
    /// More informations here :
    /// https://members.iracing.com/jforum/posts/list/75/3669811.page#11771352
    /// </summary>
    public class ClassicAffineDistribution : ClassicEqualitarian, ITakeCarsProportionCalculator
    {

        #region Active Parameters
        public override bool UseParameterMinCars
        {
            get { return true; }
        }
        #endregion

        // classes in the race
        List<int> carClassesIds;
        // the entry list
        List<Line> data;

        // proportions if %, of each cars
        Dictionary<int, double> classProportion;

        

        Dictionary<int, int> classDistributionForFullSplit;


        internal override void InitData(List<int> classesIds, List<Line> data)
        {
            this.carClassesIds = classesIds;
            if (this.carClassesIds.Count == 1)
            {
                // we do not need that calculation if only one class
                return;
            }
            this.data = data;

            if (ParameterMinCarsValue < 1) ParameterMinCarsValue = 10;
            if (ParameterMinCarsValue == 0) ParameterMinCarsValue = Math.Min(ParameterMinCarsValue, fieldSize / classesIds.Count);

            // calculate class proportion in %
            classProportion = new Dictionary<int, double>();
            var classesQueues = Tools.SplitCarsPerClass(data);
            foreach (var queue in classesQueues)
            {
                classProportion.Add(queue.CarClassId, Convert.ToDouble(queue.Cars.Count) / Convert.ToDouble(data.Count));
            }
            // -->




            classDistributionForFullSplit = new Dictionary<int, int>();
            int numberOfClasses = classProportion.Count;
            for (int i = 0; i < numberOfClasses - 1; i++)
            {

                int classId = carClassesIds[i];
                double percentage = classProportion[classId];

                // use Yannick Lapchin affine formula to get distribution of car in a complete field size
                double nb = ParameterMinCarsValue + (fieldSize - numberOfClasses * ParameterMinCarsValue) * percentage;
                nb = Math.Round(nb);

                classDistributionForFullSplit.Add(classId, Convert.ToInt32(nb));
            }
            if (classProportion.Count > classDistributionForFullSplit.Count)
            {
                int lastclassId = classesIds.Last();

                int remaining = fieldSize;
                foreach (var x in classDistributionForFullSplit.Values)
                {
                    remaining -= x;
                }

                classDistributionForFullSplit.Add(lastclassId, remaining);
            }



        }
        

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
            if (carClassesIds.Count == 1)
            {
                return fieldSizeOrLimit;
            }

            // rule of thirds on the classDistributionForFullSplit
            double carsToTake = 0;
            double forAFieldOf = 0;

            foreach (var c in classes)
            {
                forAFieldOf += classDistributionForFullSplit[c];
                if(c == classId) carsToTake = classDistributionForFullSplit[c]; ;
            }

            double percent = carsToTake / forAFieldOf;
            carsToTake = percent * fieldSizeOrLimit;
            return Convert.ToInt32(carsToTake);
        }


        public override int TakeClassCars(int fieldSize, int remCarClasses, Dictionary<int, int> classRemainingCars, int classid, List<ClassCarsQueue> carsListPerClass, int split)
        {
            //get classes with remaining cars
            var classesIdWithRemaningCars = (from r in classRemainingCars where r.Value > 0 select r.Key).ToList();
            return TakeCars(classid, classesIdWithRemaningCars, fieldSize);
        }

        

    }
}
