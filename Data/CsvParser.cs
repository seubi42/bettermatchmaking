using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BetterMatchMaking.Data
{
    public class CsvParser
    {
        public List<Line> Data { get; private set; }
        public List<Line> DistinctCars { get; private set; }

        public CsvParser()
        {
            Data = new List<Line>();
            DistinctCars = new List<Line>();
        }

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


            // Get distinct cars
            DistinctCars = (from r in Data where r.driver_id < 0 select r).ToList();
            if(DistinctCars.Count == 0) // not a team race
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
