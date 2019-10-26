﻿// Better Splits Project - https://board.ipitting.com/bettersplits
// Written by Sebastien Mallet (seubiracing@gmail.com - iRacer #281664)
// --------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Data
{
    /// <summary>
    /// Queue of cars for a specific class.
    /// Usefull for algorithm
    /// </summary>
    public class ClassCarsQueue
    {
        /// <summary>
        /// The iRacing Class Id.
        /// Ex: 100 = GTE Class.
        /// </summary>
        internal int CarClassId { get; set; }

        /// <summary>
        /// The cars queue (the entry list in that class)
        /// </summary>
        internal List<Data.Line> Cars { get; set; }


        /// <summary>
        /// This method get and removes items from the Cars queue list.
        /// Usefull for algorithm which need to dequeue cars.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        internal List<Data.Line> PickCars(int i)
        {
            var selection = Cars.Take(i).ToList();
            foreach (var c in selection) Cars.Remove(c);
            return selection;
        }

        /// <summary>
        /// Get cars without removing from queue
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        internal List<Data.Line> GetFirstCars(int skip, int i)
        {
            return Cars.Skip(skip).Take(i).ToList();
        }


        public int CarsCount
        {
            get
            {
                if (Cars == null) return 0;
                return Cars.Count;
            }
        }


    }
}
