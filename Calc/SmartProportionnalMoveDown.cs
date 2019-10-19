using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Data;

namespace BetterMatchMaking.Calc
{
    public class SmartProportionnalMoveDown : IMatchMaking
    {
        public List<Split> Splits { get; private set; }

        // parameters
        public bool UseParameterP
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

        

        ProportionnalBalancedMatchMaking firstpart;

        int fieldSize;


        public void Compute(List<Line> data, int fieldSize)
        {
           

            // Split cars per class
            carclasses = Tools.SplitCarsPerClass(data);
            carClassesIds = (from r in carclasses select r.CarClassId).ToList();

            // first pass with a simple algoritm
            this.fieldSize = fieldSize;
            firstpart = new ProportionnalBalancedMatchMaking();
            firstpart.ParameterIRValue = ParameterIRValue;
            firstpart.ParameterPValue = ParameterPValue;
            firstpart.Compute(data, fieldSize);
            
            Splits = firstpart.Splits;

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

            


            foreach (var classId in carClassesIds)
            {
                EnsureMinCarsInClassSplits(classId);
            }




            // si plusieurs split single class à la toute fin, les regrouper
            GroupEndedSingleClassSplits();

        }

        private void GroupEndedSingleClassSplits()
        {
            int splitsCount = 0;
            for (int i = Splits.Count - 1; i >= 0; i--)
            {
                if(Splits[i].GetClassesCount() == 1)
                {
                    splitsCount++;
                }
                else
                {
                    break;
                }
            }

            // end splits to merge
            if (splitsCount > 0)
            {
                List<int> carsCount = new List<int>();
                List<Line> data = new List<Line>();
                for (int i = 0; i < splitsCount; i++)
                {
                    data.AddRange(Splits[Splits.Count - 1 - i].AllCars);

                    carsCount.Add(Splits[Splits.Count - 1 - i].AllCars.Count);
                }

                int endAvg = Convert.ToInt32((from r in carsCount select r).Average())+1;
                firstpart.Compute(data, endAvg);
                for (int i = 0; i < splitsCount; i++)
                {
                    Splits[Splits.Count - splitsCount + i] = firstpart.Splits[i];
                    Splits[Splits.Count - splitsCount + i].Number = Splits.Count - splitsCount + i + 1;
                }
            }
        }

        private void EnsureMinCarsInClassSplits(int classId)
        {


            int classIndex = carClassesIds.IndexOf(classId);

            var splitsContainingThisClass = (from r in Splits
                                             where r.CountClassCars(classIndex) > 0
                                             select r).ToList();

            var lastSplitContainingThisClass = splitsContainingThisClass.Last();

            int avgCarsInSplit = Convert.ToInt32((from r in splitsContainingThisClass select r.CountClassCars(classIndex)).Average());


            //int minToEnsure = ParameterMinCarsInLastSplitClass;
            //minToEnsure = Math.Min(minToEnsure, avgCarsInSplit);

            // int minToEnsure = Convert.ToInt32(Convert.ToDouble(avgCarsInSplit) * 0.75);
            int minToEnsure = avgCarsInSplit;


            // and it is a single class last split
            if (lastSplitContainingThisClass.GetClassesCount() == 1)
            {
                minToEnsure = (from r in Splits
                               where r.TotalCarsCount > fieldSize / 2
                               select r.TotalCarsCount).Min();
            }
            

            

            int missingcars = Math.Max(0, minToEnsure - lastSplitContainingThisClass.CountClassCars(classIndex));
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

                if(toCollect > missingcars - collectedcars)
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

            int classesToCompute = classesSof.Count - 1;

            for (int i = 0; i < classesToCompute; i++)
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

            
            


            for (int j = 0; j < classesSof.Count; j++)
            {
                int classId = carClassesIds[j];
                if (!movedCategories.Contains(classId)) // not moved class
                {
                    for (int spi = splitIndex; spi < Splits.Count; spi++)
                    {

                       
                        List<int> exception = null;
                        if (spi == splitIndex) exception = movedCategories;

                        var daSplit = GetSplit(spi);
                        

                        int carsToAdd = 0;
                        do
                        {
                            int maxcars = TakeCars(daSplit, spi, j, exception);
                            int currentCars = daSplit.CountClassCars(j);

                            carsToAdd = Math.Max(0, maxcars - currentCars);
                            int availableSlots = fieldSize - daSplit.TotalCarsCount;
                            carsToAdd = Math.Min(carsToAdd, availableSlots);
                            carsToAdd = Math.Max(0, carsToAdd);

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
                    int maxcars = TakeCars(Splits[j], j, i, null);
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


    }
}
