using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Library.Data;

namespace BetterMatchMaking.Library.Calc
{
    /// <summary>
    /// Very close to iRacing Results
    /// 
    /// This algorithm is a retro-engineering which try to give very very close
    /// results to iRacing Match Making.
    /// 
    /// Main part of the code is close from ClassicRaw but with some improvments
    /// and more process to round cars distribution between last split and others
    /// 
    /// Do to that I use a after process calculation. You can check the Optimize
    /// method to run it with step by step debugger.
    /// </summary>
    public class ClassicEqualitarian : IMatchMaking, ITakeCarsProportionCalculator
    {
        #region Disabled Parameters
        public virtual bool UseParameterClassPropMinPercent
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
        public int ParameterEqualizeSplits { get; set; }
        public int ParameterClassPropMinPercentValue { get; set; }
        public int ParameterRatingThresholdValue { get; set; }
        public int ParameterMaxSofDiffValue { get; set; }
        public int ParameterMaxSofFunctAValue { get; set; }
        public int ParameterMaxSofFunctBValue { get; set; }
        public int ParameterMaxSofFunctXValue { get; set; }
        public int ParameterTopSplitExceptionValue { get; set; }
        public int ParameterMostPopulatedClassInEverySplitsValue { get; set; }
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


                Dictionary<int, int> classRemainingCarsBeforeChange = new Dictionary<int, int>();
                foreach (var item in classRemainingCars)
                {
                    classRemainingCarsBeforeChange.Add(item.Key, item.Value);
                }


                foreach (var carClass in carsListPerClass)
                {
                    // count cars to take in this class 
                    int takeCars = fieldSize;
                    takeCars = TakeClassCars(fieldSize, remCarClasses, classRemainingCarsBeforeChange, carClass.CarClassId, carsListPerClass, splitCounter);

                    // if not enought remianing cars than wanted, take what is possible
                    int carClassSize = Math.Min(takeCars, classRemainingCars[carClass.CarClassId]);


                    classSplitsCount[carClass.CarClassId].Add(carClassSize); // save the number of car in the class for this split
                    classRemainingCars[carClass.CarClassId] -= carClassSize; // decrement reminaing cars in the class

                }

                


                for (int i = 0; i < carsListPerClass.Count; i++) // do a pass per class
                {

                    // sum cars in this split
                    int carsInThisSplit = 0;
                    foreach (var carClass in carsListPerClass)
                    {
                        carsInThisSplit += classSplitsCount[carClass.CarClassId].Last();
                    }
                    int availableSlots = fieldSize - carsInThisSplit;
                    // -->

                    var lastClass = carsListPerClass.LastOrDefault(); //get last class, which is the class with more cars then the other
                                                                     
                    foreach (var item in classRemainingCars)  // if there is a better class, containing less remaning cars than available slots ?
                    {
                        if (item.Value < availableSlots && item.Value > 0 && item.Key != lastClass.CarClassId)
                        {
                            int classid = item.Key;
                            lastClass = (from r in carsListPerClass where r.CarClassId == classid select r).FirstOrDefault();
                            if (lastClass == null) lastClass = carsListPerClass.LastOrDefault();
                            else
                            {
                                availableSlots = Math.Min(availableSlots, item.Value);
                                break;
                            }
                        }
                    }

                   

                    if (lastClass != null)
                    {
                        // available slots ?
                        if (carsInThisSplit < fieldSize)
                        {
                            // fill this availableSlots with last class cars to match the maximum field size..
                            var splitclasslist = classSplitsCount[lastClass.CarClassId];
                            splitclasslist[splitclasslist.Count - 1] += availableSlots;

                            // and decremement remaining cars if this last class
                            classRemainingCars[lastClass.CarClassId] -= availableSlots;
                        }
                    }
                }



                // iis there always the same number of car class than the previous split ?
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


            // for each MultiClassMode, for each change of simultaneous car classes if you prefer
            // ... (when 3 classes, when 2 classes, when 1 class)
            foreach (var field in modes)
            {
                // create a dictionnary where KEY is the car class id
                // and value is the number of cars
                field.ClassCarsTarget = new Dictionary<int, int>();

                foreach (var carClass in carsListPerClass)
                {
                    // sum all cars of this class in this "MultiClassMode" 
                    int sum = 0;
                    List<int> splits = classSplitsCount[carClass.CarClassId];
                    for (int i = field.FromSplit - 1; i < field.ToSplit; i++)
                    {
                        sum += splits[i];
                    }

                    // count how many splits are in this "MultiClassMode"
                    int splitsCount = field.ToSplit - field.FromSplit + 1;

                    // calculate the average value, because we want all split having the same number of cars
                    // it will be our Target
                    int targetClassAverage = sum / splitsCount;

                    // save in the table the Target the this MultiClassMode:
                    // car class id -> target (avergate value)
                    field.ClassCarsTarget.Add(carClass.CarClassId, targetClassAverage);
                }
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
                    int take = mode.ClassCarsTarget[carClass.CarClassId];

                    // save the class target cars count in this class
                    split.SetClassTarget(carsListPerClass.IndexOf(carClass), take);
                    // .. and decrement the remaninng cars of this class
                    classRemainingCars[carClass.CarClassId] -= take;
                }
            }

            // OPTIMIZATIONS
            // the important points of this algorithm
            Optimize(fieldSize, classRemainingCars);

            // AT THIS POINT :
            // on the Splits array, all Class{i}Target values are updated with
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

            // done
            // :-)


        }

        private void Optimize(int fieldSize, Dictionary<int, int> classRemainingCars)
        {
            // OPTIMISATION 1 : fill to the limit top splits

            // for each split
            foreach (var split in Splits)
            {
                // get available slots
                int availableSlots = fieldSize - split.TotalTarget;
                while (availableSlots > 0)
                {
                    // while empty slots are available
                    int classToAdd = -1;
                    foreach (var remClass in classRemainingCars)
                    {
                        if (remClass.Value > 0)
                        {
                            // find a class where cars are remaining
                            classToAdd = remClass.Key;
                            break;
                        }
                    }

                    if ((from r in classRemainingCars where r.Value > 0 select r).Count() == 0)
                    {
                        // there is no remaining cars, in any class now so close this process
                        break;
                    }

                    if (classToAdd > -1)
                    {
                        // there is a class containing remaining cars and we can fill the available slot with it

                        classRemainingCars[classToAdd]--; // decrement 1 car on this remaining class cars list

                        // add to the target cars numbers of this class
                        int classIndex = classRemainingCars.Keys.ToList().IndexOf(classToAdd);
                        split.SetClassTarget(classIndex, split.GetClassTarget(classIndex) + 1);
                    }

                    // refresh if available slots
                    availableSlots = fieldSize - split.TotalTarget;

                    // it still available slots, if will loop for another pick
                }
            }


            // OPTIMISATION 2 : spread missing cars in bottom splits

            // get a dictionnary containing :
            // KEY = car class
            // VALUES = missing cars on last split 
            var excesses = (from r in classRemainingCars where r.Value < 0 select r).ToList();
            foreach (var excess in excesses)
            {
                int classId = excess.Key;
                int classIndex = classRemainingCars.Keys.ToList().IndexOf(classId); // convert the class id to array index

                // count how many splits contains this car class
                int splitsCount = 0;
                foreach (var split in Splits)
                {
                    if (split.GetClassTarget(classIndex) > 0) splitsCount++;
                }

                // how many cars can only remove on each split to spread the hole
                var v = Convert.ToDouble(Math.Abs(excess.Value)) / Convert.ToDouble(splitsCount);
                int removeForEachBottomSplit = Convert.ToInt32(Math.Ceiling(v));

                // in how many split remove them fo match the count
                int splitsToRemove = Math.Abs(excess.Value) / removeForEachBottomSplit;

                int removedCars = 0;
                for (int i = Splits.Count - 1; i >= 0; i--) // for each split, start from bottom
                {
                    if (removedCars < splitsToRemove)
                    {
                        var x = Splits[i].GetClassTarget(classIndex); // get the count
                        x -= removeForEachBottomSplit; // decrement it
                        Splits[i].SetClassTarget(classIndex, x); // set the now count
                    }
                    removedCars++;
                }
            }

        }

        private int SumValues(Dictionary<int, int> kv)
        {
            return (from r in kv select r.Value).Sum();
        }


        public virtual int TakeClassCars(int fieldSize, int remCarClasses, Dictionary<int, int> classRemainingCars, int classid, List<ClassCarsQueue> carsListPerClass, int split)
        {
            /*
             *  The distribution of the cars is here very simple
             *  it just divide the field size by the number of classes which are in the split.
             */
            return fieldSize / remCarClasses;
            
        }
        
    }
}

