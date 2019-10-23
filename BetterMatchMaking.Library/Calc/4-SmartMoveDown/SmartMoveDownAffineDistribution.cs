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
    /// This algorithm first use a ClassicAffineDistribution to make splits
    /// which allows the use of the ParameterMinCars.
    /// 
    /// Then a second process try to move less populated class to lower splits if
    /// the % difference between the lowest and the highest class SoF is greater
    /// than the UseParameterMaxSofDiff parameter.
    /// 
    /// To fit the right cars number per split class, cars are moved from one split
    /// to another, respecting the iRating order. It can occurs in two case
    ///  - When a class (ex: C7) are moved down to the next split, cars are now missing
    ///  on the other class (GTE and GT3). The right proportion of it have to been
    ///  moved to this first split.
    /// - When a classe are moved down to the next split, there is now too much cars
    /// on this second split. Cars in excess have to been moved down lower, which can
    /// trigger recursive moves.
    /// 
    /// At the end, some optimisation are made to equalize last split fireld size with
    /// the others.
    /// 
    /// This algorithm is more complex but gives very interresting results.
    /// </summary>
    public class SmartMoveDownAffineDistribution : IMatchMaking
    {
        public List<Split> Splits { get; private set; }

        #region Active Parameters
        public bool UseParameterMaxSofDiff
        {
            get { return true; }

        }
        public int ParameterMaxSofDiffValue { get; set; }
        public int ParameterMaxSofFunctAValue { get; set; }
        public int ParameterMaxSofFunctBValue { get; set; }
        public int ParameterMaxSofFunctXValue { get; set; }

        public bool UseParameterTopSplitException
        {
            get { return true; }

        }
        public int ParameterTopSplitExceptionValue { get; set; }
        public bool UseParameterMostPopulatedClassInEverySplits
        {
            get { return true; }
        }
        public int ParameterMostPopulatedClassInEverySplitsValue { get; set; }

        public virtual bool UseParameterMinCars
        {
            get { return true; }
        }

        public int ParameterMinCarsValue { get; set; }
        #endregion


        #region Disabled Parameters
        public bool UseParameterRatingThreshold
        {
            get { return false; }

        }
        public int ParameterRatingThresholdValue { get; set; }

        public virtual bool UseParameterClassPropMinPercent
        {
            get { return false; }
        }
        public int ParameterClassPropMinPercentValue { get; set; }
        #endregion



        // internal variables
        internal List<ClassCarsQueue> carclasses;
        internal List<int> carClassesIds;
        internal IMatchMaking baseAlgorithm;
        internal int fieldSize;
        private List<Line> data;
        private int moveDownPass = 1;
        // -->

        

        public void Compute(List<Line> data, int fieldSize)
        {
            this.data = data;

            // Split cars per class, and enum class Ids
            carclasses = Tools.SplitCarsPerClass(data);
            carClassesIds = (from r in carclasses select r.CarClassId).ToList();

            // initialize the base algorithm
            baseAlgorithm = GetBaseAlgorithm();
            BetterMatchMakingCalculator.CopyParameters(this, baseAlgorithm as IMatchMaking);

            // Compute with be base Algorithm and get results
            (baseAlgorithm as IMatchMaking).Compute(data, fieldSize);
            Splits = (baseAlgorithm as IMatchMaking).Splits;

            // Before the move down process, the the true field size targetted
            this.fieldSize = Convert.ToInt32(Math.Ceiling((from r in Splits select r.TotalCarsCount).Average()));

            // at this point, results it the same than the base algorithm
            

            // now, enter to the Smart Move Down process
            SmartMoveDownProcess();

        }

        /// <summary>
        /// The car class distribution is made by the ClassicProportionsRuled algorithm
        /// </summary>
        /// <returns></returns>
        internal virtual IMatchMaking GetBaseAlgorithm()
        {
            return new ClassicAffineDistribution();
        }



        /// <summary>
        /// Helper to calc Cars to get
        /// </summary>
        /// <param name="split">the split</param>
        /// <param name="classId">the class id you want to fill</param>
        /// <param name="exceptionClassId">the class ids which are not (or not allowed) in this split</param>
        /// <param name="fieldSizeOrLimit">the available slots count to fill (with every splits)</param>
        /// <returns>the number of cars to get, which is the part of 'fieldSizeOrLimit' corresponding the the classId</returns>
        internal virtual int TakeCars(Split split, int classId, List<int> exceptionClassId, int fieldSizeOrLimit)
        {
            if (exceptionClassId == null) exceptionClassId = new List<int>();

            List<int> classesToSelect = new List<int>();
            Dictionary<int, int> classesRemaining = new Dictionary<int, int>();
            foreach (int id in carClassesIds)
            {
                if (!exceptionClassId.Contains(id))
                {
                    classesToSelect.Add(id);
                    classesRemaining.Add(id, 1);
                }
            }
            ClassicAffineDistribution algo = baseAlgorithm as ClassicAffineDistribution;
            return algo.TakeClassCars(fieldSizeOrLimit, classesToSelect.Count, classesRemaining, classId, null, split.Number);
        }



        /// <summary>
        /// This Process is the main idea of this algorithm.
        /// </summary>
        private void SmartMoveDownProcess()
        {
  

            // For every split
            for (int i = 0; i < Splits.Count; i++)
            {
                // mode down process. the most important thing on this algorithm
                MoveDownCarsSplits(Splits[i], i);
            }

 

            CleanEmptySplits(); // just to be sure


            MergeTheTwoLastSplits(); // because it can happens when differences are quite large on very last split
            CleanEmptySplits(); // just to be sure


            OptimizeAndSolveDifferences(); // a third pass
            CleanEmptySplits(); // just to be sure

            carclasses = Tools.SplitCarsPerClass(data);

        }

        




       
        /// <summary>
        /// Move down cars from a split to the lower one
        /// </summary>
        /// <param name="s"></param>
        /// <param name="splitIndex"></param>
        public void MoveDownCarsSplits(Split s, int splitIndex)
        {
            List<int> classesSof = CalcSplitSofs(s);


            // classes we move down in this split
            List<int> movedCategories = new List<int>();
            // -->


            AddMostPopulatedClassInTheSplitIfMissing(s);


            
            

            // move cars down
            for (int i = 0; i < classesSof.Count - 1; i++)
            {
                // if we have to move the class to the next split
                if (HaveToMoveDown(s, i, classesSof)) 
                {
                    // pick all the class cars
                    var cars = s.PickClassCars(i);
                    s.SetClassTarget(i, 0);

                    // add them to the next split
                    Split nextSplit = GetSplit(splitIndex + 1);
                    AppendCarsToSplit(nextSplit, i, cars);

                    if (cars.Count > 0)
                    {
                        movedCategories.Add(carClassesIds[i]);
                    }
                }
                
            }

            // -->



            // up cars to fill leaved slots
            UpCarsToSplit(s, movedCategories);
            // -->

            // to much cars in the split, move them down
            // reducedClasses is the ids of the classes recudes
            // keep then in a variable because we don't want
            // to update them again after that
            var reducedClasses = MoveDownExcessCarsInTheSplit(s);
            for (int e = s.Number; e < Splits.Count-1; e++)
            {
                MoveDownExcessCarsInTheSplit(Splits[e]);
            }
            // -->


            // build the exception list to classes to lock on this split
            // movedCategories = union of [movedCategories + reducedClasses + empty classes of 0 cars]
            foreach (var c in reducedClasses)
            {
                if (!movedCategories.Contains(c)) movedCategories.Add(c);
            }
            foreach (var c in carClassesIds)
            {
                int classIndex = carClassesIds.IndexOf(c);
                if(s.CountClassCars(classIndex) == 0)
                {
                    if (!movedCategories.Contains(c)) movedCategories.Add(c);
                }
            }
            // -->

            // up cars to fill leaved slot again
            // for classe not in the exception list 'movedCategories'
            UpCarsToSplit(s, movedCategories);
            // -->
        }

        /// <summary>
        /// Get all the category sofs in the split
        /// </summary>
        /// <param name="s">the split</param>
        /// <returns>SoFs list</returns>
        private List<int> CalcSplitSofs(Split s)
        {
            s.RefreshSofs();

            List<int> classesSof = new List<int>();

            foreach (var carclass in carclasses)
            {
                int classid = carclass.CarClassId;
                int classindex = carClassesIds.IndexOf(classid);

                classesSof.Add(s.GetClassSof(classindex));
            }

            return classesSof;
        }


        /// <summary>
        /// Test if the class have to be moved down to the next split
        /// </summary>
        /// <param name="s">The split to check</param>
        /// <param name="classIndex">The class index to check in the 's' split</param>
        /// <param name="splitSofs">The SoFs corresponding to classes</param>
        /// <returns></returns>
        public bool HaveToMoveDown(Split s, int classIndex, List<int> splitSofs)
        {
            

            // some exceptions:

            if (ParameterTopSplitExceptionValue == 1 && s.Number == 1)
            {
                // if the ParameterTopSplitExceptionValue is set to 1
                // and it is the first split.
                // -> never move down classes on it
                return false;
            }

            if (s.Number == Splits.Count)
            {
                // if the split is the last one
 
                // -> never move down classes on it
                return false;
            }


            if (!s.GetClassesIndex().Contains(classIndex))
            {
                // the split doest not contains the class

                return false;
            }
            // -->


            // get each class SoFs in this split
            // and keep min and max
            s.RefreshSofs();
            int classSof = s.GetClassSof(classIndex);
            int min = classSof;
            int max = s.GlobalSof;
            max = Math.Max(max, s.Class1Sof);
            max = Math.Max(max, s.Class2Sof);
            max = Math.Max(max, s.Class3Sof);
            max = Math.Max(max, s.Class4Sof);
            // -->

            // exit if 0
            if (min == 0 && max == 0) return false;
            // -->


            //double referencesof = (s.GlobalSof + max + classSof) / 3;
            double referencesof = classSof;
            //double referencesof = s.GlobalSof;
            //double referencesof = classSof;
            if(referencesof == 0)
            {
                referencesof = min;
            }
            // -->

            // difference in % between min and max
            int diff = Convert.ToInt32(100 * Convert.ToInt32(referencesof) / max);
            diff = 100 - diff;
            if (diff < 0)
            {
                diff = Math.Abs(diff);
            }
            // -->

            // what is the allowed limit ?
            // read it from ParameterMaxSofDiffValue (constant value)
            double limit = ParameterMaxSofDiffValue;
            //limit = moveDownPass; // it will be only the half on second pass

            

            // and it set, read it from the affine function
            // f(rating) = (rating / X) * A) + b
            double fx = ParameterMaxSofFunctXValue;
            double fa = ParameterMaxSofFunctAValue;
            double fb = ParameterMaxSofFunctBValue;
            if (!(fx == 0 || fa == 0 || fb == 0))
            {
                

                limit = ((Convert.ToDouble(referencesof) / fx) * fa) + fb;
                limit = Math.Max(limit, ParameterMaxSofDiffValue);

                
            }


            bool movedown = (diff >= limit);


            s.Info += "(Δ:" + referencesof + "/" + max + "=" + diff + ",L:" + Convert.ToInt32(limit) + "," + Convert.ToInt32(movedown) + ") ";


            return movedown;
        }



        /// <summary>
        /// If there is too much cars in the split (more than fieldSize)
        /// Than this method will move down the excess 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private List<int> MoveDownExcessCarsInTheSplit(Split s)
        {
            List<int> classesToReducted = new List<int>();

            // list classes in this split and class not in this splits
            List<int> classesInTheSplit = new List<int>();
            List<int> classesNotInTheSplit = new List<int>();
            for (int i = 0; i < carClassesIds.Count; i++)
            {
                if (s.CountClassCars(i) > 0)
                {
                    classesInTheSplit.Add(carClassesIds[i]);
                }
                else
                {
                    classesNotInTheSplit.Add(carClassesIds[i]);
                }
            }
            // -->




            // get limits list by querying nominal cars count for a 
            // complete field size with same car classes
            // KEY = classid
            // VALUE = targetted cars number
            Dictionary<int, int> classLimits = new Dictionary<int, int>();
            foreach (var c in classesInTheSplit)
            {

                int limit = TakeCars(s, c, classesNotInTheSplit, fieldSize);
                classLimits.Add(c, limit);
            }
            // -->

            // for each limit
            foreach (var limit in classLimits)
            {
                // get class and max cars count wanted
                int classId = limit.Key;
                int classIndex = carClassesIds.IndexOf(classId);
                int max = limit.Value;

                // count if to much cars
                int carsToMove = s.CountClassCars(classIndex) - max;

                // yes there are cars in excess for that class
                // while there is
                for (int i = 0; i < carsToMove; i++)
                {

                    // get the next split containng the same class
                    var nextSplitContainingSameClassCars = (from r in Splits
                                                            where r.Number > s.Number
                                                            && r.CountClassCars(classIndex) > 0
                                                            select r).FirstOrDefault();

                    // it the parameter to force most populated class in every
                    // split is set to yes, than simply get the next split
                    // nevermind if it contains the same class or not we will add it
                    if (ParameterMostPopulatedClassInEverySplitsValue == 1)
                    {
                        if (classIndex == carClassesIds.Count - 1)
                        {
                            // so get the next split
                            nextSplitContainingSameClassCars = (from r in Splits
                                                                where r.Number > s.Number
                                                                select r).FirstOrDefault();

                            if (nextSplitContainingSameClassCars != null)
                            {
                                // if this split does not contains the class yet, create it
                                if (nextSplitContainingSameClassCars.GetClassId(classIndex) != classId)
                                    nextSplitContainingSameClassCars.SetClass(classIndex, classId);
                                // -->
                            }
                        }
                    }
                    // end of the ParameterMostPopulatedClassInEverySplitsValue management
                    // -->


                    // if the next split to put cars on is not found
                    if (nextSplitContainingSameClassCars == null)
                    {
                        // just get the next one neverless contains the class yet
                        nextSplitContainingSameClassCars = (from r in Splits
                                                            where r.Number > s.Number
                                                            select r).FirstOrDefault();
                        if (nextSplitContainingSameClassCars != null)
                        {
                            // if this split does not contains the class yet, create it
                            if (nextSplitContainingSameClassCars.GetClassId(classIndex) != classId)
                                nextSplitContainingSameClassCars.SetClass(classIndex, classId);
                            // -->
                        }
                    }

                    // is the target split is here ?
                    if (nextSplitContainingSameClassCars != null)
                    {
                        // pick one car to the current split 's' (in excess)
                        var pick = s.PickClassCars(classIndex, 1, true);
                        // and append them to the targeted one
                        nextSplitContainingSameClassCars.AppendClassCars(classIndex, pick);

                    }
                    else
                    {
                        // no... hm
                        // no move possible
                        // keeps car in it for the moment
                    }

                    // add to the return on that method the classId we reduced
                    if (!classesToReducted.Contains(classId)) classesToReducted.Add(classId);
                }
                // end ot the for loop 'carsToMove'


            } // end of the foreach loop class limit

            // return the list of classesId reduced
            return classesToReducted;

        }



        /// <summary>
        /// If not enough car in the split (after move down)
        /// This method will up cars from the next(s) split(s) to fill it
        /// but not of course 'movedCategories' (cars we have just move down) 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="movedCategories"></param>
        private void UpCarsToSplit(Split s, List<int> movedCategories)
        {
            // calc the available slots
            int availableSlots = fieldSize - s.TotalCarsCount;

            
            // build a dictionnary containing cars moves we want
            // KEY : class ID
            // VALUE : number of car to add in this split
            Dictionary<int, int> carsToUp = new Dictionary<int, int>();
            for (int i = 0; i < carClassesIds.Count; i++)
            {
                int classId = carClassesIds[i];
                if (!movedCategories.Contains(classId)) // not moved class
                {
                    int take = TakeCars(s, classId, movedCategories, availableSlots);
                    carsToUp.Add(classId, take);
                }
            }
            // -->

            // because of % rounds, this check will lower the dictionnary values
            // before the split will excess the fieldSize because it will contains an approximation error
            while ((from r in carsToUp select r.Value).Sum() > Math.Max(0, availableSlots))
            {
                var ck = (from r in carsToUp orderby r.Value descending select r.Key).FirstOrDefault();
                carsToUp[ck]--;
            }
            // -->

            // call the method to do the moves
            UpCarsToSplit(s, carsToUp);
        }


        /// <summary>
        /// Do the move of cars from next(s) split(s) to the 's' split.
        /// regads to the 'carsToUp' dictionnary
        /// </summary>
        /// <param name="s">the target split we want to fill</param>
        /// <param name="carsToUp">Cars we need to move up. 
        /// KEY is classId
        /// VALUE is number of cars
        /// </param>
        private void UpCarsToSplit(Split s, Dictionary<int, int> carsToUp)
        {
            // for each class
            foreach (int classId in carsToUp.Keys)
            {
                // get missing cars count and class Id
                int classMissing = carsToUp[classId];
                int classIndex = carClassesIds.IndexOf(classId);
                // -->

                // foreach missing car
                for (int i = 0; i < classMissing; i++)
                {

                    // find a lower split containig the same class
                    var nextSplitContainingSameClassCars = (from r in Splits
                                                            where r.Number > s.Number
                                                            && r.CountClassCars(classIndex) > 0
                                                            select r).FirstOrDefault();

                    // doest we got one ?
                    if (nextSplitContainingSameClassCars != null)
                    {
                        // pick the top car of the next split (the highest IR of it)
                        var pick = nextSplitContainingSameClassCars.PickClassCars(classIndex, 1, false);


                        // doest the the target split 's' already contains this class ?
                        if(s.GetClassId(classIndex) != classIndex)
                        {
                            // no, we will create id
                            s.SetClass(classIndex, classId);
                        }

                        // append the car in the target split 's'
                        s.AppendClassCars(classIndex, pick);

                    }
                    else
                    {
                        // no...
                        // we can not to the move
                        // never mind we will fix that later
                        // :-D
                    }
                }
                // end of foreach missing car
            } 
            // end of foreach class
        }


        #region Basic Helpers

        /// <summary>
        /// Add cars to split
        /// </summary>
        /// <param name="split">split to fill</param>
        /// <param name="carClassIndex">class index to fill</param>
        /// <param name="cars">cars to append</param>
        private void AppendCarsToSplit(Split split, int carClassIndex, List<Line> cars)
        {
            split.SetClass(carClassIndex, carClassesIds[carClassIndex]);
            split.AppendClassCars(carClassIndex, cars);
        }

        /// <summary>
        /// Get the split by its index.
        /// And if it does not exists, create it.
        /// </summary>
        /// <param name="splitIndex">split index</param>
        /// <returns></returns>
        private Split GetSplit(int splitIndex)
        {
            Split nextSplit = null;
            if (splitIndex < Splits.Count)
            {
                nextSplit = Splits[splitIndex];
            }
            else
            {
                nextSplit = new Split();
                nextSplit.Number = splitIndex + 1;
                Splits.Add(nextSplit);
            }
            return nextSplit;
        }
        #endregion


        #region Basic Optimization

        /// <summary>
        /// If the two last splits can fill only one, group them.
        /// No matter the SoF differences
        /// /// </summary>
        private void MergeTheTwoLastSplits()
        {
            // doest the race have more than 1 split ?
            if (Splits.Count > 1)
            {
                var lastSplit1 = Splits[Splits.Count - 1];
                var lastSplit2 = Splits[Splits.Count - 2];

                // if totalcars of 2 lowest splits is less tha field size
                if (lastSplit1.TotalCarsCount + lastSplit2.TotalCarsCount < fieldSize)
                {
                    // merge them
                    for (int i = 0; i < 4; i++)
                    {
                        var pick = lastSplit1.PickClassCars(i);
                        if (pick.Count > 0)
                        {
                            lastSplit2.AppendClassCars(i, pick);
                        }
                    }
                    // -->
                }

            }
        }



        /// <summary>
        /// Clean Emtpy splits and empty ghost classes on splits
        /// </summary>
        private void CleanEmptySplits()
        {
            // remove splits with 0 cars
            var splits = (from r in Splits where r.TotalCarsCount > 0 select r).ToList();

            // reset numbers from 1 to end
            for (int i = 0; i < splits.Count; i++)
            {
                splits[i].Number = i + 1;
            }
            Splits = splits;
            // -->

            // if empty classes on every split
            foreach (var s in Splits)
            {
                // clean them
                s.CleanEmptyClasses();
            }
            // -->
        }

        #endregion

        #region Smart Optimizations

        /// <summary>
        /// To force a split having the most populated class.
        /// If ParameterMostPopulatedClassInEverySplitsValue = 1
        /// else it will be skipped
        /// </summary>
        /// <param name="s"></param>
        private void AddMostPopulatedClassInTheSplitIfMissing(Split s)
        {
            // if we want  the most populated class on each split
            if (ParameterMostPopulatedClassInEverySplitsValue == 1)
            {
                // add most populated class in the split :

                // first, the the list not classes not in this split
                List<int> classesNotInThisSplit = new List<int>();
                for (int i = 0; i < carClassesIds.Count - 2; i++)
                {
                    if (s.CountClassCars(i) == 0) classesNotInThisSplit.Add(carClassesIds[i]);
                }

                // get the most populated class id we want to have
                int mostPopClassId = carClassesIds.Last();

                // create a move dictionnary
                // KEY : populated class id
                // VALUES : missing cars to fit the good number of cars in this populated class 
                Dictionary<int, int> mostPopAdd = new Dictionary<int, int>();
                int mostPopTarget = TakeCars(s, mostPopClassId, null, fieldSize);
                mostPopTarget -= s.CountClassCars(carClassesIds.Count - 1);
                // -->

                // there is a move up to do
                if (mostPopTarget > 0)
                {
                    // so do it
                    mostPopAdd.Add(mostPopClassId, mostPopTarget);
                    UpCarsToSplit(s, mostPopAdd);
                    // -->
                }
            }
        }

        // Third pass of the algorithm,
        // it will fix every little difference
        // for exemple, after all the car moves
        // the C7 splits can have : 12 / 12 / 12 / 7 cars
        // we want to have : 11 / 11 / 11 / 10 instead
        private void OptimizeAndSolveDifferences()
        {
            // after all the car moves we will get the descriptions of
            // the splits, with ranges description like follwing
            // we just want numbers, not the list of cars
            // because it is easier to make average on it
            //  {Split: 1, C7: 11 cars}
            //  {Split: 2, C7: 11 cars}
            //  {Split: 5, C7: 11 cars}
            //  {Split: 8, C7: 8 cars}
            List<MultiClassChanges> splitsDescriptions = ConvertCurrentSplitsToSplitDescriptions();
            // -->

            // Equalize last split cars count with the minimum of other splits
            // Ex, after that it will be :
            //  {Split: 1, C7: 11 cars}
            //  {Split: 2, C7: 11 cars}
            //  {Split: 5, C7: 11 cars}
            //  {Split: 8, C7: 10 cars}
            EqualizeLastSplitsClassCarsWithOthers(splitsDescriptions);

            // Do Average value on identical split descrption
            // for exemple if two lines are identical
            //  -Same class count
            // - Same classes id
            // but cars count of each is not exactly the same
            // try to set the average value
            AverageSameLinesClassCarCounts(splitsDescriptions);

            // If there is split with more cars than field Size, fix it
            SolveSplitsExceedFieldSize(splitsDescriptions);


            // Not, finaly re-implements split from our description
            // we just have to follow the splitsDescriptions List
            // like if it was a todo list to get the right number
            // of cars in the right classes
            Splits = InstanciateSplitsFromNumberedDescription(splitsDescriptions);
        }

        /// <summary>
        /// This method will convert the current Splits
        /// to simple numered description.
        /// we just want numbers, not the list of cars
        /// because it is easier to make average on it.
        /// ---
        /// Exemple :
        //  {Split: 1, C7: 11 cars}
        //  {Split: 2, C7: 11 cars}
        //  {Split: 5, C7: 11 cars}
        //  {Split: 8, C7: 8 cars}
        /// </summary>
        /// <returns></returns>
        private List<MultiClassChanges> ConvertCurrentSplitsToSplitDescriptions()
        {
            List<MultiClassChanges> modes = new List<MultiClassChanges>();
            // foreach splits
            foreach (var item in Splits)
            {
                // split descrpition using the 'MultiClassChanges' class
                MultiClassChanges m = new MultiClassChanges();

                // set the split number.
                // (we will not use range here)
                // (or if you prefer, it will be ranges of 1) :D
                m.FromSplit = item.Number;
                m.ToSplit = item.Number;

                // number of classes in the split
                m.ClassesCount = item.GetClassesCount();
                m.ClassCarsTarget = new Dictionary<int, int>();

                // numbers of cars in each class
                // dictionnary ClassCarsTarget
                // KEY : class id
                // VALUE : cars count
                foreach (var classid in carClassesIds)
                {
                    int classIndex = carClassesIds.IndexOf(classid);
                    int classCarCount = item.CountClassCars(classIndex);
                    if (classCarCount > 0)
                    {
                        m.ClassCarsTarget.Add(classid, classCarCount);
                    }
                }

                
                // add that description to the return list of that method
                modes.Add(m);
            }
            // foreach splits end

            // return the list
            return modes;
        }



        /// <summary>
        /// This method will balance last split cars count (with less cars)
        /// with the minimums of others
        /// </summary>
        /// <param name="splitsDescriptions"></param>
        private void EqualizeLastSplitsClassCarsWithOthers(List<MultiClassChanges> splitsDescriptions)
        {
            // for each class
            var classesToEqualize = carClassesIds.ToArray().Reverse();
            foreach (var classid in classesToEqualize)
            {
                int classIndex = carClassesIds.IndexOf(classid);

                // get the description of the lastsplit
                var lastSplitDescription = (from r in splitsDescriptions where r.ClassCarsTarget.ContainsKey(classid) select r).LastOrDefault();
                if (lastSplitDescription != null)
                {

                    // max allow difference between last split and minimum of others
                    // we allow only 1 cars difference.
                    int maxAllowedDifference = 1; 

                    // min is the lastsplit cars count in the class
                    double lastSplitCars = lastSplitDescription.ClassCarsTarget[classid];

                    // get other splits upper
                    var otherSplits = (from r in splitsDescriptions where r.ClassCarsTarget.ContainsKey(classid) where r.ToSplit < lastSplitDescription.ToSplit select r).ToList();

                    // if there is other splits
                    if (otherSplits.Count > 0)
                    {
                        double minOfOtherSplits = 0;
                        do
                        {
                            // get the minimum car counts in the same class but on other splits than our last one
                            minOfOtherSplits = (from r in otherSplits select r.ClassCarsTarget[classid]).Min();
                            // -->

                            // difference exits
                            if (minOfOtherSplits - lastSplitCars >= maxAllowedDifference)
                            {
                                // get the split with the more cars in that class,
                                // lowest split first
                                var splitToReduce = (from r in splitsDescriptions
                                                     where r.ClassCarsTarget.ContainsKey(classid)
                                                     && r.ToSplit < lastSplitDescription.FromSplit
                                                     orderby r.ClassCarsTarget[classid] descending,
                                                     r.FromSplit descending
                                                     select r).FirstOrDefault();
                                // -->

                                // -1 for that split and +1 for last split
                                splitToReduce.ClassCarsTarget[classid]--;
                                lastSplitDescription.ClassCarsTarget[classid]++;


                                lastSplitCars = lastSplitDescription.ClassCarsTarget[classid];
                                minOfOtherSplits = (from r in otherSplits select r.ClassCarsTarget[classid]).Max();
                            }
                            //end of difference exits


                        } while (minOfOtherSplits - lastSplitCars >= maxAllowedDifference);
                        // loop if differences still exits for that class
                    }
                    // end of if there is other splits

                } // end of lastSplitDescription != null
            } 
            // end of foreach class
        }

        /// <summary>
        /// If lines are identical
        ///  - Same class count
        ///  - Same classes id
        /// try to set the average value for cars count of each class to reduce differences
        /// </summary>
        /// <param name="splitDescriptions"></param>
        private void AverageSameLinesClassCarCounts(List<MultiClassChanges> splitsDescritions)
        {
            // for each splits
            foreach (var splitDescription in splitsDescritions)
            {
                // get all the splits with same classes id and same classes number
                var allSameSplits = (from r in splitsDescritions where r.ClassesKey == splitDescription.ClassesKey select r).ToList();

                // if not alone
                if (allSameSplits.Count > 1)
                {
                    // foreach class
                    foreach (var classid in carClassesIds)
                    {
                        // check class exits in the split
                        if (splitDescription.ClassCarsTarget.ContainsKey(classid))
                        {
                            // create a list of cars count
                            List<int> classTarget = new List<int>();
                            foreach (var samemode in allSameSplits)
                            {
                                classTarget.Add(samemode.ClassCarsTarget[classid]);
                            }

                            // calc the average and the sum
                            int classTargetAvg = Convert.ToInt32(classTarget.Average());
                            int classTargetSum = Convert.ToInt32(classTarget.Sum());

                            // set the average for every splits
                            foreach (var samemode in allSameSplits)
                            {
                                samemode.ClassCarsTarget[classid] = classTargetAvg;
                            }

                            // after settings the change.
                            // calc the new sum
                            // and the difference between oldsum and new sum
                            // it will gives if cars are now missing
                            int newSum = classTargetAvg * allSameSplits.Count;
                            int balance = classTargetSum - newSum;

                            // while cars are in excess
                            while (balance < 0)
                            {
                                // get a same split to remove 1 car
                                // get the split with more total cars possible
                                // and / or the lowest possible
                                var excesstarget = (from r in allSameSplits
                                                     orderby
                                                     r.CountTotalTargets() descending,
                                                     r.ToSplit descending,
                                                     r.FromSplit descending
                                                     select r).FirstOrDefault();

                                if(excesstarget == null)
                                {
                                    // not found, open the query to any splits containig this class
                                    excesstarget = (from r in splitsDescritions
                                                     where r.ClassCarsTarget.ContainsKey(classid)
                                                     orderby
                                                     r.CountTotalTargets() descending,
                                                     r.ToSplit descending
                                                     select r).FirstOrDefault();
                                }

                                excesstarget.ClassCarsTarget[classid]--;
                                balance++; // update the balance
                            }
                            // end of while cars are in excess

                            // while cars are missing
                            while (balance > 0)
                            {
                                // get a split same split to remove add
                                // 1 missing car in it
                                // where fieldSize has not been reached
                                // the uppest split possible
                                var missingtarget = (from r in allSameSplits
                                                     where r.CountTotalTargets() < fieldSize
                                                     orderby r.CountTotalTargets() ascending,
                                                     r.FromSplit ascending
                                                     select r).FirstOrDefault();

                                if (missingtarget == null)
                                {
                                    // not found, open the query to any splits containig this class
                                    missingtarget = (from r in splitsDescritions
                                                     where r.ClassCarsTarget.ContainsKey(classid)
                                                     && r.CountTotalTargets() < fieldSize
                                                     orderby r.CountTotalTargets(),
                                                     r.FromSplit ascending
                                                     select r).FirstOrDefault();
                                }

                                if (missingtarget == null)
                                {
                                    // impossible, all fieldSize has been reached

                                    // so try to get any splits of other sort
                                    // containing this class
                                    // but with different other classes combinaition
                                    // where fieldSize has not been reached
                                    // the uppest split possible
                                    int mostpopulatedclass = carClassesIds.LastOrDefault();

                                    var othersplit = (from r in splitsDescritions
                                                      where r.ClassCarsTarget.ContainsKey(classid)
                                                      && r.ClassCarsTarget.ContainsKey(mostpopulatedclass)
                                                      orderby r.CountTotalTargets() ascending,
                                                      r.FromSplit ascending
                                                      select r).FirstOrDefault();
                                    // -->

                                    // add 1 missing car to it
                                    othersplit.ClassCarsTarget[classid]++;

                                    // remove 1 mostpopulated car to id
                                    othersplit.ClassCarsTarget[mostpopulatedclass]--;

                                    // now find the split containint our class
                                    // with the lowest total cars in it
                                    othersplit = (from r in splitsDescritions
                                                  where r.ClassCarsTarget.ContainsKey(mostpopulatedclass)
                                                  orderby r.CountTotalTargets()
                                                  select r).FirstOrDefault();

                                    // and add our missing car to id
                                    othersplit.ClassCarsTarget[mostpopulatedclass]++;
                                }
                                else
                                {
                                    // target split was found

                                    // add our missing car to id
                                    missingtarget.ClassCarsTarget[classid]++;
                                }
                                balance--; // update the balance
                            }

                        }

                    }

                }


            }
        }

        /// <summary>
        /// If there is split with more cars than field Size, fix it
        /// </summary>
        /// <param name="splitsDescriptions"></param>
        private void SolveSplitsExceedFieldSize(List<MultiClassChanges> splitsDescriptions)
        {
            // foreach split
            foreach (var splitDescription in splitsDescriptions)
            {
                // while the split is in excess
                while (splitDescription.CountTotalTargets() > fieldSize)
                {
                    // get the class with more cars
                    var excess = (from r in splitDescription.ClassCarsTarget orderby r.Value descending select r).FirstOrDefault();
                    int classid = excess.Key;

                    // find the uppest split possible containing the same class
                    // and a free avaiable slot
                    var splitWithSameClassAndSlotAvailable = (from r in splitsDescriptions
                                                              where
                                                              r.ClassCarsTarget.ContainsKey(excess.Key)
                                                              && r.CountTotalTargets() < fieldSize
                                                              orderby r.ClassCarsTarget[excess.Key] ascending
                                                              select r).FirstOrDefault();

                    // yes, found it
                    if (splitWithSameClassAndSlotAvailable != null)
                    {
                        // move the car to it
                        splitWithSameClassAndSlotAvailable.ClassCarsTarget[classid]++;
                        splitDescription.ClassCarsTarget[classid]--;
                    }

                    // no, not any possible
                    else
                    {
                        // get the classes id, from most populated to less
                        List<int> mostpopulatedclassed = carClassesIds.ToList();
                        mostpopulatedclassed.Reverse();
                        mostpopulatedclassed.Remove(classid); // but remove the current class
                        // -->

                        // we will do a pool shot in two moves

                        // foreach most populated class
                        foreach (int mostpopulatedclass in mostpopulatedclassed)
                        {

                            // find the split having the less cars possible, the lowest possible
                            // containing the mostpopulatedclass
                            // and thecontaining  current class 'classid'
                            var othersplit1 = (from r in splitsDescriptions
                                               where
                                               r.ClassCarsTarget.ContainsKey(classid)
                                               && r.ClassCarsTarget.ContainsKey(mostpopulatedclass)
                                               && r.ClassCarsTarget[mostpopulatedclass] > 0
                                               orderby r.CountTotalTargets() ascending, r.ToSplit descending
                                               select r).FirstOrDefault();

                            // find split containing the mostpopulatedclass, the uppest possible
                            // with the lowest car possible
                            var othersplit2 = (from r in splitsDescriptions
                                               where
                                               r.ClassCarsTarget.ContainsKey(mostpopulatedclass)
                                               && r.CountTotalTargets() < fieldSize
                                               && r.ClassCarsTarget[mostpopulatedclass] > 0
                                               orderby r.CountTotalTargets() ascending, r.ToSplit descending
                                               select r).FirstOrDefault();

                            // it these 2 splits are found
                            if (othersplit1 != null && othersplit2 != null)
                            {
                                // in otherslit1 : change a mostpopulatedclass slot with a classid slot
                                othersplit1.ClassCarsTarget[mostpopulatedclass]--;
                                othersplit1.ClassCarsTarget[classid]++;

                                // in otherslit1 : finaly add the missing car
                                othersplit2.ClassCarsTarget[mostpopulatedclass]++;

                                // to end solving the problem, we can now remove the car in excess to our current split
                                splitDescription.ClassCarsTarget[classid]--;

                                // break the 'foreach most populated class'
                                // we don't need to try with another class
                                // we solved the problem
                                break;
                            }
                            else
                            {
                                // not found too...
                                // we will try with the next mostpopulatedclass
                            }
                        }
                    }

                }
            } // end of foreach split
        }


        /// <summary>
        /// Instanciate final splits by following 
        /// splitsDescriptions like the instructions of a todo list.
        /// Car number we will correspond to queue cars pick up.
        /// </summary>
        /// <param name="splitsDescriptions"></param>
        /// <returns></returns>
        private List<Data.Split> InstanciateSplitsFromNumberedDescription(List<MultiClassChanges> splitsDescriptions)
        {
            // create a new list of splits
            List<Data.Split> newSplits = new List<Split>();


            int number = 1;
            foreach (var mode in splitsDescriptions)
            {
                // new split with number
                Data.Split split = new Split();
                split.Number = number;

                // foreach class
                foreach (var classid in mode.ClassCarsTarget.Keys)
                {
                    // get instuctions : class and number of cars to take
                    int take = mode.ClassCarsTarget[classid];
                    int classIndex = carClassesIds.IndexOf(classid);

                    // pick cars from que right class queue
                    var cars = carclasses[classIndex].PickCars(take);

                    // set cars in the right split class
                    split.SetClass(classIndex, cars, classid);
                }
                // end of foreach class

                number++; // increment the number
                newSplits.Add(split); // add the new split to the returned list
            }

            // to help debugging :
            // if preview split Info was filled, copy the value
            for (int i = 0; i < newSplits.Count; i++)
            {
                newSplits[i].Info = Splits[i].Info;
            }
            // -->

            return newSplits;
        }

        #endregion
    }

}
