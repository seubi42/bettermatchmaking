using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Library.Data;

namespace BetterMatchMaking.Library.Calc
{
    /// <summary>
    /// This algorithm extends the ClassicProportionnal
    /// which makes the % computing for distribution
    /// 
    /// Please see and tun the ClassicEqualitarian algorithm first to understand the process.
    /// 
    /// This one implements a method ConstraintProportions to ensure the difference
    /// of less populated class and most populated class in more than value of UseParameterClassPropMinPercent
    /// 
    /// </summary>
    public class ClassicProportionnalBalanced : ClassicProportionnal, ITakeCarsProportionCalculator
    {
        #region Active Parameters
        public override bool UseParameterClassPropMinPercent
        {
            get { return true; }
        }
        #endregion



        internal override void ConstraintProportions(Dictionary<int, double> classRatio,
            Dictionary<int, int> classRemainingCars ,
            List<ClassCarsQueue> carsListPerClass)
        {
            List<int> availableClasses = (from r in classRemainingCars where r.Value > 0 select r.Key).ToList();
            double limit = Convert.ToDouble(ParameterClassPropMinPercentValue) / 100d;

            
            double maxRatio = (from r in classRatio select r.Value).Max(); // ratio of less populated class
            double minRatio = (from r in classRatio select r.Value).Min(); // ratio of most populated class
            int maxClass = (from r in classRatio orderby r.Value descending select r.Key).FirstOrDefault(); // class id of most populated class


            foreach (var carclass in carsListPerClass)
            {
                if (classRemainingCars.ContainsKey(carclass.CarClassId)) // is class still available
                {

                    // is it under the limit set with UseParameterClassPropMinPercent ?
                    if (classRatio[carclass.CarClassId] < limit * maxRatio) 
                    {
                        double toAdd = limit * maxRatio - classRatio[carclass.CarClassId]; // amount of ratio missing

                        // rule of three
                        double multiplicator = 0;
                        foreach (var cr in classRatio)
                        {
                            if (availableClasses.Contains(cr.Key))
                            {
                                multiplicator += cr.Value;
                            }
                        }
                       
                        // increment the class ratio
                        classRatio[carclass.CarClassId] += toAdd;
                        // decrement the most populated class ratio
                        classRatio[maxClass] -= toAdd;
                    }
                }
            }
        }


    }
}


