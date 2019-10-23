// Better Splits Project - https://board.ipitting.com/bettersplits
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
    /// This class is an internal description of "How to make splits" or splits ranges.
    /// It is very helpfull to do some average value or optimisations by handling numbers instead of concrete car lists.
    /// Exemple: {FromSplit: 1, ToSplit: 4, ClassesCount: 3}
    /// </summary>
    class MultiClassChanges
    {

        #region RANGE (FromSplit; ToSplit)
        /// <summary>
        /// Split range Start
        /// </summary>
        public int FromSplit { get; set; }

        /// <summary>
        /// Split range End
        /// </summary>
        public int ToSplit { get; set; }
        #endregion



        /// <summary>
        /// Number of classes needed in that split range
        /// </summary>
        public int ClassesCount { get; set; }

        #region CLASSES (ClassCarsTarget; ClassesKey)

        /// <summary>
        /// Dictionnary to describre Number of cars to to target in each class
        /// Key: classId
        /// Value: number of cars
        /// </summary>
        public Dictionary<int, int> ClassCarsTarget { get; set; }



        /// <summary>
        /// Helper which return conctenation of all Class Ids.
        /// Exemple : "77;473;100;"
        /// Use full to test if same classes are used than other MultiClassChanges object.
        /// </summary>
        public string ClassesKey
        {
            get
            {
                string ret = "";
                List<int> ids = (from r in ClassCarsTarget orderby r.Key ascending select r.Key).ToList();
                foreach (var id in ids)
                {
                    ret += id + ";";
                }
                return ret;
            }
        }
        #endregion

        #region TOTAL 


        public int CountTotalTargets()
        {
            return (from r in ClassCarsTarget select r.Value).Sum();
        }
        #endregion
    }
}
