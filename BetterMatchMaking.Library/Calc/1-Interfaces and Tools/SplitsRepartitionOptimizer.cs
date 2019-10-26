using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Library.Data;

namespace BetterMatchMaking.Library.Calc
{
    public class SplitsRepartitionOptimizer
    {
        public List<Data.Split> Splits { get; private set; }
        int fieldSize;
        List<int> carClassesIds;
        List<ClassCarsQueue> carclasses;
        ClassicAffineDistribution algo;

        public SplitsRepartitionOptimizer(List<Data.Split> splits, 
            int fieldSize, 
            List<int> carClassesIds, List<ClassCarsQueue> carclasses,
            ClassicAffineDistribution algo)
        {
            this.Splits = splits;
            this.fieldSize = fieldSize;
            this.carClassesIds = carClassesIds;
            this.carclasses = carclasses;
            this.algo = algo;
        }

        

        /// <summary>
        /// To force a split having the most populated class.
        /// If ParameterMostPopulatedClassInEverySplitsValue = 1
        /// else it will be skipped
        /// </summary>
        /// <param name="s"></param>
        private bool AddMostPopulatedClassInTheSplitIfMissing(List<Split> splits, Split s)
        {
            bool ret = false;
            // if we want  the most populated class on each split
            if (true /*ParameterMostPopulatedClassInEverySplits*/)
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
                int mostPopTarget = TakeCars(carClassesIds, splits, s, mostPopClassId, null, fieldSize);
                mostPopTarget -= s.CountClassCars(carClassesIds.Count - 1);
                // -->

                // there is a move up to do
                if (mostPopTarget > 0)
                {
                    // so do it
                    mostPopAdd.Add(mostPopClassId, mostPopTarget);
                    UpCarsToSplit(carClassesIds, splits, s, mostPopAdd);
                    ret = true;
                    // -->
                }
            }
            return ret;
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
        internal static void UpCarsToSplit(List<int> carClassesIds, List<Split> splits, Split s, Dictionary<int, int> carsToUp)
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
                    var nextSplitContainingSameClassCars = (from r in splits
                                                            where r.Number > s.Number
                                                            && r.CountClassCars(classIndex) > 0
                                                            select r).FirstOrDefault();

                    // doest we got one ?
                    if (nextSplitContainingSameClassCars != null)
                    {
                        // pick the top car of the next split (the highest IR of it)
                        var pick = nextSplitContainingSameClassCars.PickClassCars(classIndex, 1, false);


                        // doest the the target split 's' already contains this class ?
                        if (s.GetClassId(classIndex) != classIndex)
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

        // Third pass of the algorithm,
        // it will fix every little difference
        // for exemple, after all the car moves
        // the C7 splits can have : 12 / 12 / 12 / 7 cars
        // we want to have : 11 / 11 / 11 / 10 instead
        public List<Data.Split> OptimizeAndSolveDifferences()
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

            return Splits;
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

                                if (excesstarget == null)
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
                        bool solutionfound = false;
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
                                solutionfound = true;
                                break;
                            }
                            else
                            {
                                // not found too...
                                // we will try with the next mostpopulatedclass
                            }
                        }

                        if (!solutionfound)
                        {
                            // main problem here, we have to found a solution
                            break;
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

        /// <summary>
        /// Helper to calc Cars to get
        /// </summary>
        /// <param name="split">the split</param>
        /// <param name="classId">the class id you want to fill</param>
        /// <param name="exceptionClassId">the class ids which are not (or not allowed) in this split</param>
        /// <param name="fieldSizeOrLimit">the available slots count to fill (with every splits)</param>
        /// <returns>the number of cars to get, which is the part of 'fieldSizeOrLimit' corresponding the the classId</returns>
        private int TakeCars(List<int> carClassesIds, List<Split> splits, Split split, int classId, List<int> exceptionClassId, int fieldSizeOrLimit)
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
            
            return algo.TakeClassCars(fieldSizeOrLimit, classesToSelect.Count, classesRemaining, classId, null, split.Number);
        }

        

    }
}
