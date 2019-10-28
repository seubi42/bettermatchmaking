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
        public virtual bool UseParameterMaxSofDiff
        {
            get { return true; }

        }
        public int ParameterMaxSofDiffValue { get; set; }
        public virtual bool UseParameterMaxSofFunct
        {
            get { return true; }

        }
        public int ParameterMaxSofFunctStartingIRValue { get; set; }
        public int ParameterMaxSofFunctStartingThreshold { get; set; }
        public int ParameterMaxSofFunctExtraThresoldPerK { get; set; }

        public bool UseParameterTopSplitException
        {
            get { return true; }

        }
        public int ParameterTopSplitExceptionValue { get; set; }
        
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


        public bool UseParameterNoMiddleClassesEmpty
        {
            get { return false; }
        }

        public bool UseParameterDebugFile
        {
            get { return false; }
        }
        public int ParameterNoMiddleClassesEmptyValue { get; set; }
        public int ParameterDebugFileValue { get; set; }


        #endregion


        #region For Debugging
        internal int INTERRUPT_BEFORE_MOVEDOWN_SPLITNUMBER = -6;
        internal int INTERRUPT_BEFORE_MOVEDOWN_CLASSINDEX = 0;
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
        internal virtual int TakeCars(List<Split> splits, Split split, int classId, List<int> exceptionClassId, int fieldSizeOrLimit)
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
                if (i > 0)
                {
                    ResetSplitWithAllClassesFilled(Splits, Splits[i]);
                }
                // mode down process. the most important thing on this algorithm
                MoveDownCarsSplits(Splits, Splits[i], i);

                if (Splits[i].Number == INTERRUPT_BEFORE_MOVEDOWN_SPLITNUMBER) return;
            }

            
            
            
 



            SplitsRepartitionOptimizer optimizer = new SplitsRepartitionOptimizer(Splits, 
                fieldSize, 
                carClassesIds, 
                carclasses, 
                baseAlgorithm as ClassicAffineDistribution);
            Splits = optimizer.OptimizeAndSolveDifferences(); // a third pass
            

            carclasses = Tools.SplitCarsPerClass(data);

        }


        /// <summary>
        /// Move down all the cars of a split, in the next one to empty it.
        /// Than, right cars number for each class is picked back.
        /// </summary>
        /// <param name="splits"></param>
        /// <param name="s"></param>
        internal void ResetSplitWithAllClassesFilled(List<Split> splits, Split s)
        {
            if (s.Number < splits.Count)
            {
                var nextSplits = splits[s.Number];
                for (int i = 0; i < carClassesIds.Count; i++)
                {
                    int classIndex = i;
                    var cars = s.PickClassCars(classIndex);
                    nextSplits.AppendClassCars(classIndex, cars);
                }
                UpCarsToSplit(splits, s, new List<int>());
            }
        }



        /// <summary>
        /// Move down cars from a split to the lower one
        /// </summary>
        /// <param name="s"></param>
        /// <param name="splitIndex"></param>
        public void MoveDownCarsSplits(List<Split> splits, Split s, int splitIndex, int? forceMoveDownOfClassIndex = null)
        {
            List<int> classesSof = CalcSplitSofs(s);


            // classes we move down in this split
            List<int> movedCategories = new List<int>();
            // -->

            // ensure most populated category is filled it option set
            bool mostPopCatAsBeenFilled = AddMostPopulatedClassInTheSplitIfMissing(splits, s);
            // -->

            // move cars down
            int classesToMoveDown = classesSof.Count - 1;
            if (false /*ParameterMostPopulatedClassInEverySplits*/)
            {
                // if the parameter MostPopulatedClassInEverySplits is disabled
                // then we will also check the last class
                classesToMoveDown++;
            }

            for (int i = classesToMoveDown - 1; i >= 0; i--)
            //for (int i = 0; i < classesToMoveDown; i++)
            {

                

                // test: do we need to move down ?
                bool doTheMoveDown = false;
                if (forceMoveDownOfClassIndex != null)
                {
                    doTheMoveDown = (i == forceMoveDownOfClassIndex.Value);
                }
                else
                {
                    doTheMoveDown = HaveToMoveDown(s, i, classesSof);
                }

                if (forceMoveDownOfClassIndex == null
                    && INTERRUPT_BEFORE_MOVEDOWN_SPLITNUMBER == s.Number 
                    && INTERRUPT_BEFORE_MOVEDOWN_CLASSINDEX == i)
                {
                    // to help debugging
                    return;
                }

                // if we have to move the class to the next split
                if (doTheMoveDown) 
                {
                    // pick all the class cars
                    var cars = s.PickClassCars(i);
                    s.SetClassTarget(i, 0);

                    // add them to the next split
                    Split nextSplit = GetSplit(splits, splitIndex + 1);
                    AppendCarsToSplit(nextSplit, i, cars);

                    if (cars.Count > 0)
                    {
                        movedCategories.Add(carClassesIds[i]);
                    }
                }
                
            }

            // -->

            // reset next split ?
            //if (s.Number + 1 < splits.Count)
            //{
            //    var nextSplit2 = splits[s.Number + 1];
            //    ResetSplitWithAllClassesFilled(splits, nextSplit2);
            //}
            // -->

            // up cars to fill leaved slots
            List<int> doNotUpCategories = new List<int>();
            doNotUpCategories.AddRange(movedCategories);
            if (mostPopCatAsBeenFilled)
            {
                int mostPopupClassId = carClassesIds[carClassesIds.Count - 1];
                if (!doNotUpCategories.Contains(mostPopupClassId)) doNotUpCategories.Add(mostPopupClassId);
            }
            UpCarsToSplit(splits, s, doNotUpCategories);
            // -->

            // to much cars in the split, move them down
            // reducedClasses is the ids of the classes recudes
            // keep then in a variable because we don't want
            // to update them again after that
            var reducedClasses = MoveDownExcessCarsInTheSplit(splits, s);
            for (int e = s.Number; e < splits.Count-1; e++)
            {
                MoveDownExcessCarsInTheSplit(splits, splits[e]);
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
            UpCarsToSplit(splits, s, movedCategories);
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
        /// Check if the class have to pass the moved down test 
        /// </summary>
        /// <param name="s">The split to check</param>
        /// <param name="classIndex">The class index to check in the 's' split</param>
        /// <param name="splitSofs">The SoFs corresponding to classes</param>
        /// <returns></returns>
        public virtual bool HaveToMoveDown(Split s, int classIndex, List<int> splitSofs)
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

            return TestIfMoveDownNeeded(s, classIndex, splitSofs);
        }

        /// <summary>
        /// Test if the class have to be moved down to the next split
        /// </summary>
        /// <param name="s">The split to check</param>
        /// <param name="classIndex">The class index to check in the 's' split</param>
        /// <param name="splitSofs">The SoFs corresponding to classes</param>
        /// <returns></returns>
        internal virtual bool TestIfMoveDownNeeded(Split s, int classIndex, List<int> splitSofs)
        {
            if(s.Number == INTERRUPT_BEFORE_MOVEDOWN_SPLITNUMBER
                && classIndex == INTERRUPT_BEFORE_MOVEDOWN_CLASSINDEX)
            {
                // to help debugging, you can set breakpoint here
            }

            bool movedown = false;

            Calc.SofDifferenceEvaluator evaluator = new SofDifferenceEvaluator(s, classIndex);
            movedown = evaluator.MoreThanLimit(ParameterMaxSofDiffValue,
                ParameterMaxSofFunctStartingIRValue,
                ParameterMaxSofFunctStartingThreshold,
                ParameterMaxSofFunctStartingThreshold);
            // debug informations
            if (Tools.EnableDebugTraces)
            {
                string debug = "(Δ:$REFSOF/$MAX=$DIFF,L:$LIMIT,$MOVEDOWN) ";
                debug = debug.Replace("$REFSOF", evaluator.ClassSof.ToString());
                debug = debug.Replace("$MAX", evaluator.MaxSofInSplit.ToString());
                debug = debug.Replace("$DIFF", Convert.ToInt32(evaluator.PercentDifference).ToString());
                debug = debug.Replace("$LIMIT", Convert.ToInt32(evaluator.MaxPercentDifferenceAllowed).ToString());
                debug = debug.Replace("$MOVEDOWN", Convert.ToInt32(movedown).ToString());
                s.Info += debug;
            }
            // -->

            return movedown;
        }


        


        /// <summary>
        /// If there is too much cars in the split (more than fieldSize)
        /// Than this method will move down the excess 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private List<int> MoveDownExcessCarsInTheSplit(List<Split> splits, Split s)
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

                int limit = TakeCars(splits, s, c, classesNotInTheSplit, fieldSize);
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
                    var nextSplitContainingSameClassCars = (from r in splits
                                                            where r.Number > s.Number
                                                            && r.CountClassCars(classIndex) > 0
                                                            select r).FirstOrDefault();

                    // it the parameter to force most populated class in every
                    // split is set to yes, than simply get the next split
                    // nevermind if it contains the same class or not we will add it
                    if (true /*ParameterMostPopulatedClassInEverySplits*/)
                    {
                        if (classIndex == carClassesIds.Count - 1)
                        {
                            // so get the next split
                            nextSplitContainingSameClassCars = (from r in splits
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
                    
                    // -->


                    // if the next split to put cars on is not found
                    if (nextSplitContainingSameClassCars == null)
                    {
                        // just get the next one neverless contains the class yet
                        nextSplitContainingSameClassCars = (from r in splits
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
        internal void UpCarsToSplit(List<Split> splits, Split s, List<int> movedCategories)
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
                    int take = TakeCars(splits, s, classId, movedCategories, availableSlots);
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
            Calc.SplitsRepartitionOptimizer.UpCarsToSplit(carClassesIds,splits, s, carsToUp);
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
        private Split GetSplit(List<Split> splits, int splitIndex)
        {
            Split nextSplit = null;
            if (splitIndex < splits.Count)
            {
                nextSplit = splits[splitIndex];
            }
            else
            {
                nextSplit = new Split();
                nextSplit.Number = splitIndex + 1;
                splits.Add(nextSplit);
            }
            return nextSplit;
        }
        #endregion


       


        /// <summary>
        /// To force a split having the most populated class.
        /// If ParameterMostPopulatedClassInEverySplitsValue = 1
        /// else it will be skipped
        /// </summary>
        /// <param name="s"></param>
        internal bool AddMostPopulatedClassInTheSplitIfMissing(List<Split> splits, Split s)
        {
            bool ret = false;

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
            int mostPopTarget = TakeCars(splits, s, mostPopClassId, null, fieldSize);
            mostPopTarget -= s.CountClassCars(carClassesIds.Count - 1);
            // -->

            // there is a move up to do
            if (mostPopTarget > 0)
            {
                // so do it
                mostPopAdd.Add(mostPopClassId, mostPopTarget);
                Calc.SplitsRepartitionOptimizer.UpCarsToSplit(carClassesIds, splits, s, mostPopAdd);
                ret = true;
                // -->

            }
            return ret;
        }

    }

}
