using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Library.Data;

namespace BetterMatchMaking.Library.Calc
{
    public class SmartProportionnalMoveDown : IMatchMaking
    {
        public List<Split> Splits { get; private set; }

        // parameters
        public virtual bool UseParameterP
        {
            get { return true; }
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

        public bool UseParameterEqualizeSplits
        {
            get { return true; }
        }
        public int ParameterEqualizeSplits { get; set; }

        public int ParameterPValue { get; set; }
        public int ParameterIRValue { get; set; }
        public int ParameterMaxSofDiff { get; set; }
        public int ParameterMaxSofFunctA { get; set; }
        public int ParameterMaxSofFunctB { get; set; }
        public int ParameterMaxSofFunctX { get; set; }
        public int ParameterTopSplitException { get; set; }
        // -->



        List<CarsPerClass> carclasses;
        List<int> carClassesIds;



        ITakeCarsProportionCalculator firstpart;

        int fieldSize;


        internal virtual ITakeCarsProportionCalculator GetFirstPassCalculator()
        {
            return new ProportionnalBalancedMatchMaking();
        }

        //Dictionary<int, int> avgFieldSize;


        public void Compute(List<Line> data, int fieldSize)
        {
            //avgFieldSize = new Dictionary<int, int>();

            // Split cars per class
            carclasses = Tools.SplitCarsPerClass(data);
            
            carClassesIds = (from r in carclasses select r.CarClassId).ToList();

            // first pass with a simple algoritm
            this.fieldSize = fieldSize;
            firstpart = GetFirstPassCalculator();
            BetterMatchMaking.Library.BetterMatchMakingCalculator.CopyParameters(this, firstpart as IMatchMaking);
            (firstpart as IMatchMaking).Compute(data, fieldSize);
            
            Splits = (firstpart as IMatchMaking).Splits;


            /*
            foreach (var cc in carclasses)
            {
                int classIndex = carClassesIds.IndexOf(cc.CarClassId);
                double avg = Convert.ToDouble(cc.Cars.Count);

                avg /= Convert.ToDouble((from r in Splits
                                         where r.CountClassCars(classIndex) > 0
                                         select r).Count());

                avg = Math.Ceiling(avg);
                avgFieldSize.Add(cc.CarClassId, Convert.ToInt32(avg));
            }
            */

            SmartProcess();
            
            
            
        }

        private void SmartProcess()
        {
            // move down cars process
            for (int i = 0; i < Splits.Count; i++)
            {
                MoveDownCarsSplits(Splits[i], i);
                CleanEmptySplits();
            }

            GroupLastSplit();
            CleanEmptySplits();


            if (ParameterEqualizeSplits > 0)
            {
                
                EqualizeSplits();


            }

        }


        private void EqualizeSplits()
        {
            // describe modes
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


            
            

            
            var classesToEqualize = carClassesIds.ToArray().Reverse();
            foreach (var classid in classesToEqualize)
            {
                var lastmode = (from r in modes where r.ClassCarsTarget.ContainsKey(classid) select r).LastOrDefault();
                if (lastmode != null)
                {
                    double totalcars = (from r in modes where r.ClassCarsTarget.ContainsKey(classid)   select r.ClassCarsTarget[classid]).Sum();

                    List<MultiClassChanges> othermodes = (from r in modes where r.ClassCarsTarget.ContainsKey(classid) where r.ToSplit < lastmode.FromSplit select r).ToList();
                    if(othermodes.Count == 0) othermodes = (from r in modes where r.ClassCarsTarget.ContainsKey(classid) select r).ToList();
                    var max = (from r in othermodes select r.ClassCarsTarget[classid]).Max();
                    double avgclass = (from r in othermodes select r.ClassesCount).Average();

                    double lastsplitcars = Convert.ToDouble(lastmode.ClassCarsTarget[classid]);
                    double lastsplitcarstarget = max;


                    lastsplitcarstarget = max;

                    if(lastmode.ClassesCount == 1)
                    {
                        lastsplitcarstarget = (from r in modes select r.CountTotalTargets()).Average();
                    }
                    else if (lastmode.ClassesCount < Math.Ceiling(avgclass))
                    {
                        lastsplitcarstarget *= avgclass / lastmode.ClassesCount;
                    }

                    

                    if (true)
                    {
                        double carstofind = lastsplitcarstarget - lastsplitcars;
                        double maxcarstofind = fieldSize - lastmode.TempTotal;
                        carstofind = Math.Min(carstofind, maxcarstofind);

                        if (carstofind > 0)
                        {
                            double ratio = carstofind / totalcars;



                            var splitsToReduce = (from r in modes
                                                  where r.ClassCarsTarget.ContainsKey(classid)
                                                  && r.ToSplit < lastmode.FromSplit
                                                  select r).ToList();

                            foreach (var splitToReduce in splitsToReduce)
                            {
                                double insplitcars = Convert.ToDouble(splitToReduce.ClassCarsTarget[classid]);
                                int toremove = Convert.ToInt32(insplitcars * ratio);
                                splitToReduce.ClassCarsTarget[classid] = Convert.ToInt32(insplitcars - toremove);


                                lastmode.ClassCarsTarget[classid] += toremove;
                            }
                        }
                    }
                }
            }

            
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
                                /*if (mode.ClassesCount == 1)
                                {
                                    double x = (from r in modes select r.CountTotalTargets()).Average();
                                    classTarget.Add(Convert.ToInt32(x) - 1);
                                }
                                else
                                {
                                    classTarget.Add(samemode.ClassCarsTarget[classid]);
                                }*/

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
                            while (missing > 0 )
                            {
                                var missingtarget = (from r in modes where r.ClassCarsTarget.ContainsKey(classid)
                                                     && r.CountTotalTargets() < fieldSize
                                                     orderby r.CountTotalTargets() select r).FirstOrDefault();

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

            



            
            foreach (var mode in modes)
            {
                while (mode.CountTotalTargets() > fieldSize)
                {
                    var excess = (from r in mode.ClassCarsTarget orderby r.Value descending select r).FirstOrDefault();

                    var modewithless = (from r in modes where
                                        r.ClassCarsTarget.ContainsKey(excess.Key)
                                        && r.CountTotalTargets() < fieldSize
                                        orderby r.ClassCarsTarget[excess.Key] ascending
                                        select r).FirstOrDefault();

                    if (modewithless == null)
                    {

                        // titanic pinpin lapinou excess
                        //modewithless = (from r in modes
                        //                where
                        //                r.ClassCarsTarget.ContainsKey(excess.Key)
                        //                && r.CountTotalTargets() < fieldSize
                        //                orderby r.ClassCarsTarget[excess.Key] ascending
                        //                select r).FirstOrDefault();
                        break;
                    }

                    if (modewithless != null)
                    {
                        modewithless.ClassCarsTarget[excess.Key]++;
                        mode.ClassCarsTarget[excess.Key]--;
                    }
                    
                } 
            }

            // implement final splits
            

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

            Splits = splits2;
        }


        private void GroupLastSplit()
        {
            if(Splits.Count > 3)
            {
                var lastSplit1 = Splits[Splits.Count - 1];
                var lastSplit2 = Splits[Splits.Count - 2];

                if(lastSplit1.TotalCarsCount + lastSplit2.TotalCarsCount < fieldSize)
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

        public void MoveDownCarsSplits(Split s, int splitIndex)
        {
            List<int> classesSof = CalcSplitSofs(s);

            List<int> movedCategories = new List<int>();

            
            // move cars down
            for (int i = 0; i < classesSof.Count - 1; i++)
            {
                if (HaveToMoveDown(s, i, classesSof))
                {
                    var cars = s.PickClassCars(i);
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




            
            for (int j = 0; j < classesSof.Count; j++)
            {
                int classId = carClassesIds[j];
                if (!movedCategories.Contains(classId)) // not moved class
                {
                    for (int spi = splitIndex; spi < Splits.Count; spi++)
                    {

                       
                        List<int> exception = movedCategories;

                        var daSplit = GetSplit(spi);
                        

                        int carsToAdd = 0;
                        do
                        {
                            int maxcars = TakeCars(daSplit, spi, j, exception);
                            int currentCars = daSplit.CountClassCars(j);

                            int availableSlots = fieldSize - daSplit.TotalCarsCount;

                            

                            carsToAdd = Math.Max(0, maxcars - currentCars);
                            carsToAdd = Math.Min(carsToAdd, availableSlots);
                            carsToAdd = Math.Max(0, carsToAdd);

                            if (carClassesIds.IndexOf(classId) == carClassesIds.Count - 1)
                            {
                                // last class
                                //carsToAdd = availableSlots;
                            }

                            if (carsToAdd == 0)
                            {
                                break; // no more cars to add
                            }

                            Split nextSplitWithSameClass = null;
                            for (int z = spi + 1; z < Splits.Count; z++)
                            {
                                var temp = GetSplit(z);
                                if (temp.CountClassCars(j) > 0)
                                {
                                    nextSplitWithSameClass = temp;
                                    break;
                                }
                            }

                            if (nextSplitWithSameClass != null)
                            {
                                


                                var pick = nextSplitWithSameClass.PickClassCars(j, carsToAdd, false);

                                AddCarsToSplit(daSplit, j, pick);
                                carsToAdd -= pick.Count;

                            }
                            

                            if(nextSplitWithSameClass == null)
                            {
                                carsToAdd = 0;
                            }
                        }
                        while (carsToAdd > 0);


                    }
                }
            }


            

            for (int i = 0; i < classesSof.Count; i++)
            {
                
                int classId = carClassesIds[i];
                // for next splits, move down cars excess
                for (int j = splitIndex + 1; j < Splits.Count; j++)
                {
                    int maxcars =TakeCars(Splits[j], j, i, null);
                    
                    int currentCars = Splits[j].CountClassCars(i);

                    int carsToRemove = Math.Max(0, currentCars - maxcars);
                    if (carsToRemove > 0)
                    {
                        var pick = Splits[j].PickClassCars(i, carsToRemove, true);
                        AddCarsToSplit(GetSplit(j + 1), i, pick);
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

        

        private int TakeCars(Split split, int splitIndex, int classIndex, List<int> exceptionClassId = null)
        {
            Dictionary<int, int> carsInThisSplitAndNexts = new Dictionary<int, int>();
            for (int i = splitIndex; i < Splits.Count; i++)
            {
                for (int c = 0; c < carClassesIds.Count; c++)
                {
                    int classid = carClassesIds[c];

                    if (exceptionClassId != null && exceptionClassId.Contains(classid))
                    {
                        if (!carsInThisSplitAndNexts.ContainsKey(classid))
                        {
                            carsInThisSplitAndNexts.Add(classid, 0);
                        }
                    }
                    else
                    {
                        int classcount = Splits[i].CountClassCars(c);

                        if (carsInThisSplitAndNexts.ContainsKey(classid))
                        {
                            carsInThisSplitAndNexts[classid] += classcount;
                        }
                        else
                        {
                            carsInThisSplitAndNexts.Add(classid, classcount);
                        }
                    }
                }
            }

            int classesCount = split.GetClassesCount();
            if(classesCount == 0)
            {
                classesCount = (from r in carsInThisSplitAndNexts where r.Value > 0 select r).Count();
            }

            if(classesCount == 0)
            {
                return 0;
            }

            Dictionary<int, int> carsInThisSplitAndNexts2 = new Dictionary<int, int>();
            foreach (var k in carsInThisSplitAndNexts.Keys)
            {
                if (carsInThisSplitAndNexts[k] > 0) carsInThisSplitAndNexts2.Add(k, carsInThisSplitAndNexts[k]);
            }
            var carclasses2 = (from r in carclasses where r.Cars.Count > 0 select r).ToList();


            int take = firstpart.TakeClassCars(this.fieldSize,
                classesCount,
                carsInThisSplitAndNexts2,
                carClassesIds[classIndex],
                carclasses2,
                splitIndex + 1);

            return take;
        }


        public bool HaveToMoveDown(Split s, int classIndex, List<int> splitSofs)
        {
            if (ParameterTopSplitException == 1 && s.Number == 1)
            {
                return false;
            }

            int classSof = splitSofs[classIndex];

            /*
            int mostPopulatedClassSof = 0;
            for (int i = splitSofs.Count - 1; i >= 0; i--)
            {
                if (splitSofs[i] > 0)
                {
                    mostPopulatedClassSof = splitSofs[i];
                    break;
                }
            }*/


            int min = classSof; //  Math.Min(classSof, mostPopulatedClassSof);
            //int max = mostPopulatedClassSof; // Math.Max(classSof, mostPopulatedClassSof);
            int max = s.GlobalSof;
            max = Math.Max(max, s.Class1Sof);
            max = Math.Max(max, s.Class2Sof);
            max = Math.Max(max, s.Class3Sof);
            max = Math.Max(max, s.Class4Sof);


            if (min == 0 && max == 0) return false;

            

            int diff = 100 * min / max;
            diff = 100 - diff;

            if(diff < 0)
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



        #region deprecated


        private void deprecated_EnsureMinCarsInClassSplits(int classId)
        {


            int classIndex = carClassesIds.IndexOf(classId);



            var splitsContainingThisClass = (from r in Splits
                                             where r.CountClassCars(classIndex) > 0
                                             select r).ToList();

            var lastSplitContainingThisClass = (from r in splitsContainingThisClass
                                                orderby r.Number descending
                                                select r).FirstOrDefault();




            int carsInUpperSplits = Convert.ToInt32((from r in Splits select r.TotalCarsCount).Average());

            int take = carsInUpperSplits;
            foreach (var otherClassId in carClassesIds)
            {
                if (otherClassId != classId)
                {
                    int otherClassIndex = carClassesIds.IndexOf(otherClassId);
                    take -= lastSplitContainingThisClass.CountClassCars(otherClassIndex);
                }
            }
            take--;

            /*

            int take = 0;

            foreach (var s in splitsContainingThisClass)
            {
                int carscount = s.CountClassCars(classIndex);
                int totalcars = s.TotalCarsCount;

                double p = Convert.ToDouble(carscount) / Convert.ToDouble(totalcars);

                p = Convert.ToDouble(carscount) / p;

                take += Convert.ToInt32(p);
            }
            if (splitsContainingThisClass.Count > 0)
            {
                take /= splitsContainingThisClass.Count;
            }
            take = Math.Min(take, carsInUpperSplits); // numer of cars to take if this class was alone



            Dictionary<int, int> fakeRemCars = new Dictionary<int, int>();
            List<CarsPerClass> fakeCars = new List<CarsPerClass>();
            foreach (var item in carClassesIds)
            {
                int lsClassIndex = carClassesIds.IndexOf(item);
                int lsClassIndexCarCount = lastSplitContainingThisClass.CountClassCars(lsClassIndex);
                if(lsClassIndex == classIndex)
                {
                    lsClassIndexCarCount = take;
                }
                if (lsClassIndexCarCount > 0)
                {

                    fakeRemCars.Add(item, lsClassIndexCarCount);
                    fakeCars.Add(new CarsPerClass
                    {
                        CarClassId = item,
                        Cars = new List<Line>()
                    });
                }
            }
            Calc.ProportionnalBalancedMatchMaking pbm = new ProportionnalBalancedMatchMaking();
            BetterMatchMakingCalculator.CopyParameters(this, pbm);
            int take2 = pbm.TakeClassCars(carsInUpperSplits,
                lastSplitContainingThisClass.GetClassesCount(),
                fakeRemCars,
                carClassesIds[classIndex],
                fakeCars,
                lastSplitContainingThisClass.Number - 1);

            take = Math.Min(take, take2);
            */
            int minToEnsure = take;





            int missingcars = Math.Max(0, minToEnsure - lastSplitContainingThisClass.CountClassCars(classIndex));
            missingcars = Math.Min(missingcars, fieldSize - lastSplitContainingThisClass.TotalCarsCount);
            if (missingcars == 0) return;
            int collectedcars = 0;

            int totalcarsupper = (from r in splitsContainingThisClass select r.CountClassCars(classIndex)).Sum();
            totalcarsupper -= lastSplitContainingThisClass.CountClassCars(classIndex);

            double percentToCollectInUpperSplits = Convert.ToDouble(missingcars) / Convert.ToDouble(totalcarsupper);

            for (int i = splitsContainingThisClass.Count - 2; i >= 0; i--)
            {

                if (i == 1)
                {

                }
                int cars = splitsContainingThisClass[i].CountClassCars(classIndex);
                int toCollect = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(cars) * percentToCollectInUpperSplits));
                toCollect = Math.Max(1, toCollect);

                if (toCollect > missingcars - collectedcars)
                {
                    toCollect = missingcars - collectedcars;
                }


                if (toCollect > 0)
                {
                    toCollect = MoveDownCarsLastToLastOfChain(toCollect, classIndex,
                        splitsContainingThisClass, splitsContainingThisClass[i].Number);
                }

                collectedcars += toCollect;


            }

        }

        private int MoveDownCarsLastToLastOfChain(int carToCollect, int classIndex, List<Split> splitsChain, int sourceSplitNumber)
        {
            int ret = Int32.MaxValue;

            var split = (from r in splitsChain where r.Number == sourceSplitNumber select r).FirstOrDefault();
            int startSplitIndex = splitsChain.IndexOf(split);

            for (int i = startSplitIndex; i < splitsChain.Count - 1; i++)
            {
                var pick = splitsChain[i].PickClassCars(classIndex, carToCollect, true);
                splitsChain[i + 1].AddClassCars(classIndex, pick);
                ret = Math.Min(pick.Count, ret);
            }

            if (ret == Int32.MaxValue) ret = 0;
            return ret;
        }



        private void deprecated_GroupEndedSingleClassSplits()
        {
            int splitsCount = 0;
            for (int i = Splits.Count - 1; i >= 0; i--)
            {
                if (Splits[i].GetClassesCount() == 1)
                {
                    splitsCount++;
                }
                else
                {
                    break;
                }
            }

            // end splits to merge
            if (splitsCount > 1)
            {

                List<int> carsCount = new List<int>();
                List<Line> data = new List<Line>();
                for (int i = 0; i < splitsCount; i++)
                {
                    data.AddRange(Splits[Splits.Count - 1 - i].AllCars);

                    carsCount.Add(Splits[Splits.Count - 1 - i].AllCars.Count);
                }


                (firstpart as IMatchMaking).Compute(data, fieldSize);
                for (int i = 0; i < splitsCount; i++)
                {
                    if (i < (firstpart as IMatchMaking).Splits.Count)
                    {
                        //Splits[Splits.Count - splitsCount + i] = firstpart.Splits[i];
                        Splits[Splits.Count - splitsCount + i] = new Split();

                        var src = (firstpart as IMatchMaking).Splits[i];
                        for (int o = 0; o < 4; o++)
                        {
                            var classid = src.GetClassId(o);
                            var classindex = carClassesIds.IndexOf(classid);
                            if (classindex >= 0)
                            {
                                var classcars = src.GetClassCars(o);
                                Splits[Splits.Count - splitsCount + i].SetClass(classindex, classcars, classid);
                            }
                        }

                        Splits[Splits.Count - splitsCount + i].Number = Splits.Count - splitsCount + i + 1;
                    }
                    else
                    {
                        Splits.RemoveAt(Splits.Count - splitsCount + i);
                    }

                }

            }
        }
        #endregion
    }

}
