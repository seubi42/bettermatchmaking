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
    /// A line in the entry list
    /// </summary>
    public class Line
    {
        /// <summary>
        /// The iRacing Car Id.
        /// Ex: 102 = Porsche RSR.
        /// </summary> 
        public int car_id { get; set; }

        /// <summary>
        /// The iRacing Class Id.
        /// Ex: 100 = GTE Class.
        /// </summary>
        public int car_class_id { get; set; }

        /// <summary>
        /// The iRacing Team Id.
        /// If not a team event, teamId is same than driverId.
        /// </summary>
        public int team_id { get; set; }

        /// <summary>
        /// The iRacing Customer Id.
        /// For a driver line iRacing Id is more than 0.
        /// For a team line iRacing Id is less than 0 and equals to team_id.
        /// </summary>
        public int driver_id { get; set; }

        /// <summary>
        /// The driver/team name (to help debugging because it is better to read than only IDs).
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// The iRating of the Driver (or the average iRating of the team if team race).
        /// For a team line, iRating is 0.
        /// </summary>
        public int rating { get; set; }

   
    }
}
