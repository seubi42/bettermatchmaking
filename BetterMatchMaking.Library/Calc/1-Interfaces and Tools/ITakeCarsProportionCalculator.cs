// Better Splits Project - https://board.ipitting.com/bettersplits
// Written by Sebastien Mallet (seubiracing@gmail.com - iRacer #281664)
// --------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Calc
{
    /// <summary>
    /// For algorithm which implements a proportion calculator
    /// to count how many cars to distribute in a split
    /// </summary>
    public interface ITakeCarsProportionCalculator
    {
        /// <summary>
        /// Calc how many cars to get
        /// </summary>
        /// <param name="fieldSize">field size or number of avaible slots</param>
        /// <param name="remCarClasses">number of classes wanted</param>
        /// <param name="classRemainingCars">Dictionnary of avaible cars. Key is class id. Value is avaible cars count.</param>
        /// <param name="classid">requested class id</param>
        /// <param name="carsListPerClass">class cars queue (remaining entry list)</param>
        /// <param name="split">split number</param>
        /// <returns></returns>
        int TakeClassCars(int fieldSize, int remCarClasses,
            Dictionary<int, int> classRemainingCars, int classid,
            List<Data.ClassCarsQueue> carsListPerClass, int split);
    }
}
