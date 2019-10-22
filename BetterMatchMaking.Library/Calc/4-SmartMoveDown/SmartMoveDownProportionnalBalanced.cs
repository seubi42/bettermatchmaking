using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Library.Data;

namespace BetterMatchMaking.Library.Calc
{
    public class SmartMoveDownProportionnalBalanced : SmartMoveDownProportionsRuled
    {
        public override bool UseParameterClassPropMinPercent
        {
            get { return true;  }
        }

        internal override IMatchMaking GetBaseAlgorithm()
        {
            return new ClassicProportionnalBalanced();
        }

        internal override int TakeCars(Split split, int classId, List<int> exceptionClassId, int fieldSizeOrLimit)
        {
            // do thw bride between the 2 algorithm.
            // not very elegant...

            int splitIndex = split.Number - 1;
            int classIndex = carClassesIds.IndexOf(classId);

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
            if (classesCount == 0)
            {
                classesCount = (from r in carsInThisSplitAndNexts where r.Value > 0 select r).Count();
            }

            if (classesCount == 0)
            {
                return 0;
            }

            Dictionary<int, int> carsInThisSplitAndNexts2 = new Dictionary<int, int>();
            foreach (var k in carsInThisSplitAndNexts.Keys)
            {
                if (carsInThisSplitAndNexts[k] > 0) carsInThisSplitAndNexts2.Add(k, carsInThisSplitAndNexts[k]);
            }
            var carclasses2 = (from r in carclasses where r.Cars.Count > 0 select r).ToList();


            int take = (baseAlgorithm as ITakeCarsProportionCalculator).TakeClassCars(this.fieldSize,
                classesCount,
                carsInThisSplitAndNexts2,
                carClassesIds[classIndex],
                carclasses2,
                splitIndex + 1);

            return take;
        }
    }
}
