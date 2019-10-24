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
    /// This algorithm is based on the SmartMoveDownAffineDistribution.
    /// Please see and tun the SmartMoveDownProportionsRuled algorithm first to understand the process.
    /// 
    /// The process is the same but the TakeCars implementation is base on a ClassicProportionnalBalanced
    /// which allows the use of the UseParameterClassPropMinPercent parameter.
    /// It is less efficient, it is just to compare,
    /// </summary>
    public class SmartMoveDownProportionnalBalanced : SmartMoveDownAffineDistribution
    {
        #region Enabled Parameters
        public override bool UseParameterClassPropMinPercent
        {
            get { return true;  }
        }
        #endregion

        #region Disabled Parameters
        public override bool UseParameterMinCars
        {
            get { return false; }
        }
        #endregion

        /// <summary>
        /// The car class distribution is made by the ClassicProportionnalBalanced algorithm
        /// </summary>
        /// <returns></returns>
        internal override IMatchMaking GetBaseAlgorithm()
        {
            return new ClassicProportionnalBalanced();
        }

        internal override int TakeCars(List<Split> splits, Split split, int classId, List<int> exceptionClassId, int fieldSizeOrLimit)
        {
            // do thw bride between the 2 algorithm.
            // not very elegant because lot of objects convertions ...
            // but it works...


            int splitIndex = split.Number - 1;
            int classIndex = carClassesIds.IndexOf(classId);


            // objects convertion to fit the original TakeClassCars methods
            Dictionary<int, int> carsInThisSplitAndNexts = new Dictionary<int, int>();
            for (int i = splitIndex; i < splits.Count; i++)
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
                        int classcount = splits[i].CountClassCars(c);

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
            // end of the objects convertion. sorry for that :) --> 


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
