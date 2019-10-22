using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Library.Data;

namespace BetterMatchMaking.Library.Calc
{
    public class SmartRuledMoveDown : IMatchMaking
    {
        public List<Split> Splits { get; private set; }

        // parameters
        public virtual bool UseParameterP
        {
            get { return false; }
        }



        public bool UseParameterIR
        {
            get { return false; }

        }

        public bool UseParameterMaxSofDiff
        {
            get { return true; }

        }

        public bool UseParameterTopSplitException
        {
            get { return true; }

        }

        public bool UseParameterMostPopulatedClassInEverySplits
        {
            get { return true; }
        }

        public int ParameterPValue { get; set; }
        public int ParameterIRValue { get; set; }
        public int ParameterMaxSofDiff { get; set; }
        public int ParameterMaxSofFunctA { get; set; }
        public int ParameterMaxSofFunctB { get; set; }
        public int ParameterMaxSofFunctX { get; set; }
        public int ParameterTopSplitException { get; set; }
        public int ParameterMostPopulatedClassInEverySplits { get; set; }

        // -->



        List<CarsPerClass> carclasses;
        List<int> carClassesIds;



        RuledMatchMaking firstpart;

        int fieldSize;


        

        //Dictionary<int, int> avgFieldSize;

        List<Line> data;


        public void Compute(List<Line> data, int fieldSize)
        {
            

            this.data = data;
            //avgFieldSize = new Dictionary<int, int>();

            // Split cars per class
            carclasses = Tools.SplitCarsPerClass(data);

            carClassesIds = (from r in carclasses select r.CarClassId).ToList();

            // first pass with a simple algoritm
            firstpart = new RuledMatchMaking();
            BetterMatchMaking.Library.BetterMatchMakingCalculator.CopyParameters(this, firstpart as IMatchMaking);
            firstpart.Init(carClassesIds);
            (firstpart as IMatchMaking).Compute(data, fieldSize);

            Splits = (firstpart as IMatchMaking).Splits;
            this.fieldSize = Convert.ToInt32(Math.Ceiling((from r in Splits select r.TotalCarsCount).Average()));
            

            SmartProcess();



        }

        private void SmartProcess()
        {
            // move down cars process
            for (int i = 0; i < Splits.Count; i++)
            {
                MoveDownCarsSplits(Splits[i], i);
            }

            CleanEmptySplits();

            
            GroupLastSplit();
            CleanEmptySplits();


            OptimizeAndSolveDifferences();
            CleanEmptySplits();

        }

        

        private void OptimizeAndSolveDifferences()
        {
            List<MultiClassChanges> modes = ConvertCurrentSplitsToModesDirective();
            AverageSameLinesClassCarCounts(modes);
            SolveSplitsExceedFieldSize(modes);
            Splits = ImplementOptimizedVersion(modes);
        }


        private List<MultiClassChanges> ConvertCurrentSplitsToModesDirective()
        {
            List<MultiClassChanges> modes = new List<MultiClassChanges>();
            foreach (var item in Splits)
            {
                MultiClassChanges m = new MultiClassChanges();
                m.FromSplit = item.Number;
                m.ToSplit = item.Number;
                m.ClassesCount = item.GetClassesCount();
                m.ClassCarsTarget = new Dictionary<int, int>();



                foreach (var classid in carClassesIds)
                {
                    int classIndex = carClassesIds.IndexOf(classid);
                    int classCarCount = item.CountClassCars(classIndex);
                    if (classCarCount > 0)
                    {
                        m.ClassCarsTarget.Add(classid, classCarCount);
                    }
                }

                m.TempTotal = m.CountTotalTargets();

                modes.Add(m);
            }
            return modes;
        }

        private void EqualizeLastSplitsClassCarsWithOthers(List<MultiClassChanges> modes)
        {
            var classesToEqualize = carClassesIds.ToArray().Reverse();
            foreach (var classid in classesToEqualize)
            {
                int classIndex = carClassesIds.IndexOf(classid);


                var lastmode = (from r in modes where r.ClassCarsTarget.ContainsKey(classid) select r).LastOrDefault();
                if (lastmode != null)
                {
                    int diff = 2;
                    double min = lastmode.ClassCarsTarget[classid];
                    var othermodes = (from r in modes where r.ClassCarsTarget.ContainsKey(classid) select r).ToList();
                    double max = 0;
                    do
                    {
                        max = (from r in othermodes select r.ClassCarsTarget[classid]).Max();
                        if (max - min > diff)
                        {
                            var splitToReduce = (from r in modes
                                                 where r.ClassCarsTarget.ContainsKey(classid)
                                                 && r.ToSplit < lastmode.FromSplit
                                                 orderby r.ClassCarsTarget[classid] descending,
                                                 r.FromSplit descending
                                                 select r).FirstOrDefault();
                            splitToReduce.ClassCarsTarget[classid]--;
                            lastmode.ClassCarsTarget[classid]++;


                            min = lastmode.ClassCarsTarget[classid];
                            max = (from r in othermodes select r.ClassCarsTarget[classid]).Max();
                        }


                    } while (max - min > diff);

                }
            }
        }

        private List<Data.Split> ImplementOptimizedVersion(List<MultiClassChanges> modes)
        {
            List<Data.Split> splits2 = new List<Split>();


            int number = 1;
            foreach (var mode in modes)
            {
                Data.Split split = new Split();
                split.Number = number;
                foreach (var classid in mode.ClassCarsTarget.Keys)
                {
                    int take = mode.ClassCarsTarget[classid];
                    int classIndex = carClassesIds.IndexOf(classid);

                    var cars = carclasses[classIndex].GetCars(take);
                    split.SetClass(classIndex, cars, classid);
                }

                number++;
                splits2.Add(split);
            }

            for (int i = 0; i < splits2.Count; i++)
            {
                splits2[i].Info = Splits[i].Info;
            }

            return splits2;
        }

        private void AverageSameLinesClassCarCounts(List<MultiClassChanges> modes)
        {
            foreach (var mode in modes)
            {
                var allsamemodes = (from r in modes where r.ClassesKey == mode.ClassesKey select r).ToList();
                if (allsamemodes.Count > 1)
                {
                    foreach (var classid in carClassesIds)
                    {
                        if (mode.ClassCarsTarget.ContainsKey(classid))
                        {

                            List<int> classTarget = new List<int>();
                            foreach (var samemode in allsamemodes)
                            {

                                classTarget.Add(samemode.ClassCarsTarget[classid]);
                            }

                            int classTargetAvg = Convert.ToInt32(classTarget.Average());
                            int classTargetSum = Convert.ToInt32(classTarget.Sum());
                            foreach (var samemode in allsamemodes)
                            {
                                samemode.ClassCarsTarget[classid] = classTargetAvg;
                            }

                            int missing = classTargetSum - (classTargetAvg * allsamemodes.Count);
                            while (missing < 0)
                            {
                                var missingtarget = (from r in modes
                                                     where r.ClassCarsTarget.ContainsKey(classid)

                                                     orderby
                                                     r.CountTotalTargets() descending,
                                                     r.ToSplit descending

                                                     select r).FirstOrDefault();

                                missingtarget.ClassCarsTarget[classid]--;
                                missing++;
                            }
                            while (missing > 0)
                            {
                                var missingtarget = (from r in modes
                                                     where r.ClassCarsTarget.ContainsKey(classid)
                                     && r.CountTotalTargets() < fieldSize
                                                     orderby r.CountTotalTargets()
                                                     select r).FirstOrDefault();

                                if (missingtarget == null)
                                {
                                    int mostpopulatedclass = carClassesIds.LastOrDefault();

                                    var othersplit = (from r in modes
                                                      where r.ClassCarsTarget.ContainsKey(classid)
                                                      && r.ClassCarsTarget.ContainsKey(mostpopulatedclass)
                                                      orderby r.CountTotalTargets()
                                                      select r).FirstOrDefault();

                                    othersplit.ClassCarsTarget[classid]++;


                                    othersplit.ClassCarsTarget[mostpopulatedclass]--;

                                    othersplit = (from r in modes
                                                  where r.ClassCarsTarget.ContainsKey(mostpopulatedclass)
                                                  orderby r.CountTotalTargets()
                                                  select r).FirstOrDefault();

                                    othersplit.ClassCarsTarget[mostpopulatedclass]++;
                                }
                                else
                                {
                                    missingtarget.ClassCarsTarget[classid]++;
                                }
                                missing--;
                            }

                        }

                    }

                }


            }
        }
        private void SolveSplitsExceedFieldSize(List<MultiClassChanges> modes)
        {
            foreach (var mode in modes)
            {
                while (mode.CountTotalTargets() > fieldSize)
                {

                    var excess = (from r in mode.ClassCarsTarget orderby r.Value descending select r).FirstOrDefault();
                    int classid = excess.Key;

                    var modewithless = (from r in modes
                                        where
                                        r.ClassCarsTarget.ContainsKey(excess.Key)
                                        && r.CountTotalTargets() < fieldSize
                                        orderby r.ClassCarsTarget[excess.Key] ascending
                                        select r).FirstOrDefault();



                    if (modewithless != null)
                    {
                        modewithless.ClassCarsTarget[classid]++;
                        mode.ClassCarsTarget[classid]--;
                    }
                    else
                    {

                        List<int> mostpopulatedclassed = carClassesIds.ToList();
                        mostpopulatedclassed.Reverse();
                        mostpopulatedclassed.Remove(classid);

                        foreach (int mostpopulatedclass in mostpopulatedclassed)
                        {



                            var othersplit1 = (from r in modes
                                               where
                                               r.ClassCarsTarget.ContainsKey(classid)
                                               && r.ClassCarsTarget.ContainsKey(mostpopulatedclass)
                                               && r.ClassCarsTarget[mostpopulatedclass] > 0
                                               orderby r.CountTotalTargets() ascending, r.ToSplit descending
                                               select r).FirstOrDefault();
                            var othersplit2 = (from r in modes
                                               where
                                               r.ClassCarsTarget.ContainsKey(mostpopulatedclass)
                                               && r.CountTotalTargets() < fieldSize
                                               && r.ClassCarsTarget[mostpopulatedclass] > 0
                                               orderby r.CountTotalTargets() ascending
                                               select r).FirstOrDefault();
                            if (othersplit1 != null && othersplit2 != null)
                            {

                                othersplit1.ClassCarsTarget[mostpopulatedclass]--;
                                othersplit1.ClassCarsTarget[classid]++;
                                mode.ClassCarsTarget[classid]--;
                                othersplit2.ClassCarsTarget[mostpopulatedclass]++;
                                break;
                            }
                            else
                            {

                            }
                        }
                    }

                }
            }
        }

        private void GroupLastSplit()
        {

            if (Splits.Count > 1)
            {
                var lastSplit1 = Splits[Splits.Count - 1];
                var lastSplit2 = Splits[Splits.Count - 2];

                if (lastSplit1.TotalCarsCount + lastSplit2.TotalCarsCount < fieldSize)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        var pick = lastSplit1.PickClassCars(i);
                        if (pick.Count > 0)
                        {
                            lastSplit2.AddClassCars(i, pick);
                        }
                    }
                }

            }
        }




        private void CleanEmptySplits()
        {
            var splits = (from r in Splits where r.TotalCarsCount > 0 select r).ToList();

            for (int i = 0; i < splits.Count; i++)
            {
                splits[i].Number = i + 1;
            }

            Splits = splits;


            foreach (var s in Splits)
            {
                s.CleanEmptyClasses();
            }
        }



       

        public void MoveDownCarsSplits(Split s, int splitIndex)
        {
            List<int> classesSof = CalcSplitSofs(s);

            List<int> movedCategories = new List<int>();
            int movedDownCars = 0;


            AddMostPopulatedClassInTheSplitIfMissing(s);

            // move cars down
            for (int i = 0; i < classesSof.Count - 1; i++)
            {
                if (HaveToMoveDown(s, i, classesSof))
                {
                    var cars = s.PickClassCars(i);
                    movedDownCars += cars.Count;
                    s.SetClassTarget(i, 0);

                    Split nextSplit = GetSplit(splitIndex + 1);
                    AddCarsToSplit(nextSplit, i, cars);

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

            // to much cars in the split ?
            var reducedClasses = MoveDownTooMuchCarsInTheSplit(s);
            // -->

            // up cars to fill leaved slots again
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
            UpCarsToSplit(s, movedCategories);
            // -->





        }


        private void AddMostPopulatedClassInTheSplitIfMissing(Split s)
        {
            if (ParameterMostPopulatedClassInEverySplits == 1)
            {
                // add most populated class in the split
                List<int> classesNotInThisSplit = new List<int>();
                for (int i = 0; i < carClassesIds.Count - 2; i++)
                {
                    if (s.CountClassCars(i) == 0) classesNotInThisSplit.Add(carClassesIds[i]);
                }

                int mostPopClassId = carClassesIds.Last();
                Dictionary<int, int> mostPopAdd = new Dictionary<int, int>();
                int mostPopTarget = TakeCars(s, mostPopClassId, null, fieldSize);
                mostPopTarget -= s.CountClassCars(carClassesIds.Count - 1);
                if (mostPopTarget > 0)
                {
                    mostPopAdd.Add(mostPopClassId, mostPopTarget);
                    UpCarsToSplit(s, mostPopAdd);
                }
            }
        }

        private List<int> MoveDownTooMuchCarsInTheSplit(Split s)
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

            


            // get limits list (key = classid; value = maximum)
            Dictionary<int, int> classLimits = new Dictionary<int, int>();
            
            foreach (var c in classesInTheSplit)
            {
                
                int limit = TakeCars(s, c, classesNotInTheSplit, fieldSize);
                classLimits.Add(c, limit);
            }


            foreach (var limit in classLimits)
            {
                int classId = limit.Key;
                int classIndex = carClassesIds.IndexOf(classId);
                int max = limit.Value;

                int carsToMove = s.CountClassCars(classIndex) - max;
                if (carsToMove > 0)
                {

                    for (int i = 0; i < carsToMove; i++)
                    {


                        var nextSplitContainingSameClassCars = (from r in Splits
                                                                where r.Number > s.Number
                                                                && r.CountClassCars(classIndex) > 0
                                                                select r).FirstOrDefault();

                        if (ParameterMostPopulatedClassInEverySplits == 1)
                        {
                            if (classIndex == carClassesIds.Count - 1)
                            {
                                nextSplitContainingSameClassCars = (from r in Splits
                                                                    where r.Number > s.Number
                                                                    select r).FirstOrDefault();

                                if (nextSplitContainingSameClassCars != null)
                                {
                                    if (nextSplitContainingSameClassCars.GetClassId(classIndex) != classId)
                                    {
                                        nextSplitContainingSameClassCars.SetClass(classIndex, classId);
                                    }
                                }
                            }
                        }

                        if (nextSplitContainingSameClassCars == null)
                        {
                            nextSplitContainingSameClassCars = (from r in Splits
                                                                where r.Number > s.Number
                                                                select r).FirstOrDefault();
                            if (nextSplitContainingSameClassCars != null)
                            {
                                if (nextSplitContainingSameClassCars.GetClassId(classIndex) != classId)
                                {
                                    nextSplitContainingSameClassCars.SetClass(classIndex, classId);
                                }
                            }
                        }

                        if (nextSplitContainingSameClassCars != null)
                        {
                            var pick = s.PickClassCars(classIndex, 1, true);
                            nextSplitContainingSameClassCars.AddClassCars(classIndex, pick);
                            
                        }
                        else
                        {
                            // c'est la der, tant pis on la remet dedans

                        }

                        if (!classesToReducted.Contains(classId)) classesToReducted.Add(classId);
                    }
                }
                
            }


            return classesToReducted;
            
        }


        private void UpCarsToSplit(Split s, List<int> movedCategories)
        {
            int availableSlots = fieldSize - s.TotalCarsCount;

            
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

            while ((from r in carsToUp select r.Value).Sum() > Math.Max(0, availableSlots))
            {
                var ck = (from r in carsToUp orderby r.Value descending select r.Key).FirstOrDefault();
                carsToUp[ck]--;
            }
            UpCarsToSplit(s, carsToUp);
        }


        private void UpCarsToSplit(Split s, Dictionary<int, int> carsToUp)
        {
            foreach (int classId in carsToUp.Keys)
            {
                int classMissing = carsToUp[classId];
                int classIndex = carClassesIds.IndexOf(classId);

                for (int i = 0; i < classMissing; i++)
                {



                    var nextSplitContainingSameClassCars = (from r in Splits
                                                            where r.Number > s.Number
                                                            && r.CountClassCars(classIndex) > 0
                                                            select r).FirstOrDefault();

                    

                    if (nextSplitContainingSameClassCars != null)
                    {
                        // pick the top car
                        var pick = nextSplitContainingSameClassCars.PickClassCars(classIndex, 1, false);

                        if(s.GetClassId(classIndex) != classIndex)
                        {
                            s.SetClass(classIndex, classId);
                        }

                        s.AddClassCars(classIndex, pick);

                    }
                    else
                    {
                        // y en a plus que faire
                    }


                    
                }
            }
        }

        private void AddCarsToSplit(Split split, int carClassIndex, List<Line> cars)
        {
            split.SetClass(carClassIndex, carClassesIds[carClassIndex]);
            split.AddClassCars(carClassIndex, cars);
        }


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



        private int TakeCars(Split split, int classId, List<int> exceptionClassId, int fieldSizeOrLimit)
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

            return firstpart.TakeClassCars(fieldSizeOrLimit, classesToSelect.Count, classesRemaining, classId, null, split.Number);
        }


        public bool HaveToMoveDown(Split s, int classIndex, List<int> splitSofs)
        {
            if (ParameterTopSplitException == 1 && s.Number == 1)
            {
                return false;
            }

            if(s.Number == Splits.Count)
            {
                return false;
            }

            int classSof = splitSofs[classIndex];




            int min = classSof; 
            int max = s.GlobalSof;
            max = Math.Max(max, s.Class1Sof);
            max = Math.Max(max, s.Class2Sof);
            max = Math.Max(max, s.Class3Sof);
            max = Math.Max(max, s.Class4Sof);


            if (min == 0 && max == 0) return false;



            int diff = 100 * min / max;
            diff = 100 - diff;

            if (diff < 0)
            {
                diff = Math.Abs(diff);
            }

            double limit = ParameterMaxSofDiff;


            double fx = ParameterMaxSofFunctX;
            double fa = ParameterMaxSofFunctA;
            double fb = ParameterMaxSofFunctB;

            if (!(fx == 0 || fa == 0 || fb == 0))
            {
                limit = ((Convert.ToDouble(s.GlobalSof) / fx) * fa) + fb;
                limit = Math.Max(limit, ParameterMaxSofDiff);
                s.Info = "Diff Target=" + Convert.ToInt32(limit);
            }



            if (diff > limit)
            {
                return true; // have to move down the class because more than max allowed sof difference
            }



            return false;
        }

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



    }

}
