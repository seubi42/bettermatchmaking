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
    /// This algorithm is a very simple one.
    /// Result are pretty close to iRacing one.
    /// But there is no correction to round car number on last split.
    /// 
    /// Is was usefull for me to start from zero, and to understand
    /// problems needed to be solved in a simple case.
    /// </summary>
    public class ClassicRaw : IMatchMaking, ITakeCarsProportionCalculator
    {
        #region Disabled Parameters
        public bool UseParameterClassPropMinPercent
        {
            get { return false; }
        }
        public bool UseParameterRatingThreshold
        {
            get { return false; }
        }
        public bool UseParameterMaxSofDiff
        {
            get { return false; }
        }
        public bool UseParameterTopSplitException
        {
            get { return false; }
        }
        public bool UseParameterMostPopulatedClassInEverySplits
        {
            get { return false; }
        }
        public int ParameterMostPopulatedClassInEverySplitsValue { get; set; }
        public int ParameterClassPropMinPercentValue { get; set; }
        public int ParameterRatingThresholdValue { get; set; }
        public int ParameterMaxSofDiffValue { get; set; }
        public int ParameterMaxSofFunctAValue { get; set; }
        public int ParameterMaxSofFunctBValue { get; set; }
        public int ParameterMaxSofFunctXValue { get; set; }
        public int ParameterTopSplitExceptionValue { get; set; }

        public virtual bool UseParameterMinCars
        {
            get { return false; }
        }

        public int ParameterMinCarsValue { get; set; }
        #endregion

        public List<Split> Splits { get; private set; }

        public List<int> CarClassesId { get; private set; }

        public void Compute(List<Line> data, int fieldSize)
        {
            // Split cars per class
            var carsListPerClass = Tools.SplitCarsPerClass(data);

            // Create two dictionnary (KEY for both is the CarClass Id)
            // classRemainingCars : VALUE is the number of remaining cars in the class
            // classSplitsCount : VALUE is a list containing the number of car split per split
            Dictionary<int, int> classRemainingCars = new Dictionary<int, int>();
            Dictionary<int, List<int>> classSplitsCount = new Dictionary<int, List<int>>();
            foreach (var carClass in carsListPerClass)
            {
                classRemainingCars.Add(carClass.CarClassId, carClass.Cars.Count);
                classSplitsCount.Add(carClass.CarClassId, new List<int>());
            }

            // export classes id
            CarClassesId = new List<int>();
            foreach (var carClass in carsListPerClass) CarClassesId.Add(carClass.CarClassId);

            // MultiClassMode records describes changes on split car classes compositions
            // - FromSplit and ToSplit describes the range of splits
            // - ClassesCount describes how many car classes can be part of the splits 
            //      (exemple: 3 first for LMP1/LMP2/GTE, then 2 when it become LMP1/GTE because not enought LMP2 are available, then 1 when single class)...
            List<MultiClassChanges> modes = new List<MultiClassChanges>();
            MultiClassChanges currentMode = new MultiClassChanges();
            currentMode.FromSplit = 1;
            currentMode.ToSplit = 1;
            currentMode.ClassesCount = classRemainingCars.Count;
            modes.Add(currentMode);

            int splitCounter = 1;


            while (SumValues(classRemainingCars) > 0) // when cars remaings
            {
                // count classes containing remaining cars
                int remCarClasses = (from r in classRemainingCars where r.Value > 0 select r).Count();
                
                foreach (var carClass in carsListPerClass)
                {
                    // count cars to take in this class 
                    int takeCars = fieldSize;
                    takeCars = TakeClassCars(fieldSize, remCarClasses, classRemainingCars, carClass.CarClassId, carsListPerClass, splitCounter);

                    // if not enought remianing cars than wanted, take what is possible
                    int carClassSize = Math.Min(takeCars, classRemainingCars[carClass.CarClassId]);


                    classSplitsCount[carClass.CarClassId].Add(carClassSize); // save the number of car in the class for this split
                    classRemainingCars[carClass.CarClassId] -= carClassSize; // decrement reminaing cars in the class

                }

                var lastClass = carsListPerClass.LastOrDefault(); //get last class, which is the class with more cars then the other
                if (lastClass != null)
                {
                    // sum cars in this split
                    int carsInThisSplit = 0;
                    foreach (var carClass in carsListPerClass)
                    {
                        carsInThisSplit += classSplitsCount[carClass.CarClassId].Last();
                    }
                    // -->

                    // available slots ?
                    if (carsInThisSplit < fieldSize)
                    {
                        int availableSlots = fieldSize - carsInThisSplit;


                        // fill this availableSlots with last class cars to match the maximum field size..
                        var splitclasslist = classSplitsCount[lastClass.CarClassId];
                        splitclasslist[splitclasslist.Count - 1] += availableSlots;

                        // and decremement remaining cars if this last class
                        classRemainingCars[lastClass.CarClassId] -= availableSlots;
                    }
                }

                // is there always the same number of car class than the previous split ?
                if (remCarClasses == currentMode.ClassesCount)
                {
                    // yes, just update the ToSplit number
                    currentMode.ToSplit = splitCounter;
                }
                else
                {
                    // no, save a change starting from this split
                    currentMode = new MultiClassChanges();
                    currentMode.FromSplit = splitCounter;
                    currentMode.ToSplit = splitCounter;
                    currentMode.ClassesCount = remCarClasses;
                    modes.Add(currentMode);
                }

                splitCounter++;
            }

            
            // create the array of splits
            Splits = new List<Split>();
            int maxsplit = (from r in modes select r.ToSplit).Max();
            for (int i = 1; i <= maxsplit; i++)
            {
                var split = new Split();
                split.Number = i;
                Splits.Add(split);
            }


            // reset the classRemainingCars counts
            classRemainingCars.Clear();
            foreach (var carClass in carsListPerClass)
            {
                classRemainingCars.Add(carClass.CarClassId, carClass.Cars.Count);
            }


            // for each car class
            foreach (var carClass in carsListPerClass)
            {
                // for each split
                for (int i = 1; i <= maxsplit; i++)
                {
                    var split = Splits[i - 1]; // get the split record in the array of splits

                    // get the MultiClassMode where this split is in, the target cars count for the classes
                    var mode = (from r in modes where i >= r.FromSplit orderby r.ToSplit descending select r).First();
                    int take = fieldSize / mode.ClassesCount;

                    // save the class target cars count in this class
                    split.SetClassTarget(carsListPerClass.IndexOf(carClass), take);
                    // .. and decrement the remaninng cars of this class
                    classRemainingCars[carClass.CarClassId] -= take;
                }
            }


            


            // AT THIS POINT :
            // on the Splits array, all Class{i}Target values are up to date with
            // number of cars we want
            // for each split, and each class.


            // Implement car lists
            foreach (var split in Splits) // foreach each split
            {
                for (int i = 0; i < 4; i++) // for each car class
                {
                    int carsToAddInClass = split.GetClassTarget(i); // get the cars count we want
                    if(carsListPerClass.Count > i)
                    {
                        var cars = carsListPerClass[i].PickCars(carsToAddInClass); // pick up the cars in the ordered list by iRating DESC
                        if(cars.Count > 0)
                        {
                            split.SetClass(i, cars, carsListPerClass[i].CarClassId); // set the class car list
                        }
                    }
                      
                }
            }


            // manage the rest badly, very raw method
            var lastSplit = new Split();
            lastSplit.Number = Splits.Last().Number + 1;
            bool includeLastSplit = false;
            for (int i = 0; i < 4; i++) // for each car class
            {
                int carsToAddInClass = Splits.Last().GetClassTarget(i); // get the cars count we want
                if (carsListPerClass.Count > i)
                {
                    var cars = carsListPerClass[i].PickCars(carsToAddInClass); // pick up the cars in the ordered list by iRating DESC
                    if (cars.Count > 0)
                    {
                        lastSplit.SetClass(i, cars, carsListPerClass[i].CarClassId); // set the class car list
                    }
                }
                if (lastSplit.TotalCarsCount > 0)
                {
                    includeLastSplit = true;
                    
                }
            }
            if(includeLastSplit) Splits.Add(lastSplit);
            // done
            // :-)


        }

        private int SumValues(Dictionary<int, int> kv)
        {
            return (from r in kv select r.Value).Sum();
        }


        public virtual int TakeClassCars(int fieldSize, int remCarClasses, Dictionary<int, int> classRemainingCars, int classid, List<ClassCarsQueue> carsListPerClass, int split)
        {
            int carsToTake = fieldSize / remCarClasses;
            
            return carsToTake;
        }
        
    }
}

