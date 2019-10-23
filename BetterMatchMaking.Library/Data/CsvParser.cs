// Better Splits Project - https://board.ipitting.com/bettersplits
// Written by Sebastien Mallet (seubiracing@gmail.com - iRacer #281664)
// --------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BetterMatchMaking.Library.Data
{
    /// <summary>
    /// This tool can read a CSV file to create a List of Line (the entry list before the split).
    /// It also include a function to group "distinct lines" of cars (from driver lines) 
    /// </summary>
    public class CsvParser
    {
        #region Constructor
        public CsvParser()
        {
            Data = new List<Line>();
            DistinctCars = new List<Line>();
        }
        #endregion

        /// <summary>
        /// The raw Lines data. 
        /// If team event, it will contain 1 line per driver + 1 line for the team itself (with driverid < 0 and no irating)
        /// So, multiple lines can have same TeamId (when drivers are sharing the same car)
        /// </summary>
        public List<Line> Data { get; private set; }

        /// <summary>
        /// Distinct lines. 1 line per car.
        /// So, if team event, iRating of this line will be the average of drivers.
        /// To help debugging, Name property concatenate driver names too.
        /// </summary>
        public List<Line> DistinctCars { get; private set; }

        
        /// <summary>
        /// To add a line
        /// </summary>
        /// <param name="line">Can be a DRIVER line (with driverid more than 0 + and iRating value)
        /// or TEAM line (with driverid less than 0 + irating = 0)
        /// </param>
        public void Add(Line line)
        {
            Data.Add(line);
        }

        /// <summary>
        /// To add a line
        /// </summary>
        /// <param name="carId">iRacing Car Id (Ex: 102 = Porsche RSR)</param>
        /// <param name="carClassId">iRacing Class Id (Ex: 100 = GTE Class)</param>
        /// <param name="teamId">The iRacing Team Id.
        /// If not a team event, teamId is same than driverId.</param>
        /// <param name="driverId">The iRacing Customer Id.
        /// For a driver line iRacing Id is more than 0.
        /// For a team line iRacing Id is less than 0 and equals to team_id.
        /// </param>
        /// <param name="name">Team Name or Driver Name</param>
        /// <param name="rating">The iRating of the Driver (or the average iRating of the team if team race).
        /// For a team line, iRating is 0.</param>
        public void Add(int carId, int carClassId, int teamId, int driverId, string name, int rating)
        {
            Line l = new Line
            {
                car_id = carId,
                car_class_id = carClassId,
                team_id = teamId,
                driver_id = driverId,
                name = name,
                rating = rating
            };
            Add(l);
        }


        /// <summary>
        /// Read a CSV file.
        /// Columns needed are : car_id;car_class_i;car_class_id;team_id;driver_id;name;%rating%
        /// </summary>
        /// <param name="file">The path of the CSV file</param>
        public void Read(string file)
        {
            Data.Clear();

            string[] raw = File.ReadAllLines(file);


            //Get Columns Positions
            string[] columns = raw[0].Split(';');
            int index_car_id = -1;
            int index_car_class_id = -1;
            int index_team_id = -1;
            int index_driver_id = -1;
            int index_name = -1;
            int index_rating = -1;
            for (int i = 0; i < columns.Length; i++)
            {
                if (columns[i] == "car_id") index_car_id = i;
                else if (columns[i] == "car_class_i") index_car_class_id = i;
                else if (columns[i] == "car_class_id") index_car_class_id = i;
                else if (columns[i] == "team_id") index_team_id = i;
                else if (columns[i] == "driver_id") index_driver_id = i;
                else if (columns[i] == "name") index_name = i;
                else if (columns[i].Contains("rating")) index_rating = i;
            }
            // -->


            if(index_rating == -1)
            {
                throw new Exception("Invalid CSV File");
            }



            // Read each line and fill the Data List
            for (int i = 1; i < raw.Length; i++)
            {
                string line = raw[i];
                if(!String.IsNullOrWhiteSpace(line) && line.Contains(";"))
                {
                    string[] cells = line.Split(';');
                    Line lineobj = new Line();
                    lineobj.car_id = Convert.ToInt32(cells[index_car_id]);
                    lineobj.car_class_id = Convert.ToInt32(cells[index_car_class_id]);
                    lineobj.team_id = Convert.ToInt32(cells[index_team_id]);
                    lineobj.driver_id = Convert.ToInt32(cells[index_driver_id]);
                    lineobj.name = cells[index_name];
                    lineobj.rating = Convert.ToInt32(cells[index_rating]);
                    Data.Add(lineobj);
                }
            }
            // -->



            GroupDistinctCars();
        }

        /// <summary>
        /// This method will bind the DistinctCars List from Data list.
        /// It will group drivers of the same team using the team_id properties of each lines.
        /// </summary>
        public void GroupDistinctCars()
        {
            // Get distinct cars
            DistinctCars = (from r in Data where r.driver_id < 0 select r).ToList();
            if (DistinctCars.Count == 0) // not a team race
            {
                DistinctCars = Data;
            }
            else // it's a team race, compile drivers data
            {
                foreach (var car in DistinctCars)
                {
                    var carDrivers = (from r in Data where r.team_id == car.team_id && r.driver_id > 0 select r).ToList();
                    if (carDrivers.Count > 0)
                    {
                        car.rating = Convert.ToInt32((from r in carDrivers select r.rating).Average());
                        foreach (var driver in carDrivers) car.name += "; " + driver.name;
                    }
                }
            }
            // -->
        }
    }
}
