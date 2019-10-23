using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Sample
{
    public class HowToCodeId
    {
        public void Demo()
        {
            // 1 : read or create a dataset containing drivers and teams
            var dataset = LoadCsv("..\\..\\..\\BetterMatchMaking.UI\\Bin\\Debug\\petit-le-mans-2019-fieldsize45.csv");
            // or load from code
            //var dataset = LoadFromCode();


            // 2 : instanciate calculator and parameters
            BetterMatchMaking.Library.BetterMatchMakingCalculator calculator = new Library.BetterMatchMakingCalculator("SmartMoveDownAffineDistribution");
            calculator.ParameterClassPropMinPercentValue = 37;
            calculator.ParameterMaxSofDiffValue = 18;
            calculator.ParameterMaxSofFunctAValue = 12;
            calculator.ParameterMaxSofFunctXValue = 1000;
            calculator.ParameterMaxSofFunctBValue = -20;
            calculator.ParameterTopSplitExceptionValue = 1;

            // 3 : Launch
            int fieldSize = 45;
            calculator.Compute(dataset, fieldSize);

            // 4 : Display Results in console
            foreach (var split in calculator.Splits)
            {
                Console.WriteLine("### SPLIT " + split.Number + " ###");
                for (int i = 0; i < 4; i++)
                {
                    int classid = split.GetClassId(i);
                    if (classid > 0)
                    {
                        Console.WriteLine("   !!! CLASS " + classid + " !!!");
                        var cars = split.GetClassCars(i);
                        if (cars != null)
                        {
                            foreach (var car in cars)
                            {
                                Console.WriteLine("      - [IR " + car.rating + "]" + car.name + " - ");
                            }
                        }
                    }
                }
            }

            // 5 : enjoy
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }


        public List<BetterMatchMaking.Library.Data.Line> LoadCsv(string csvFile)
        {
            var parser = new BetterMatchMaking.Library.Data.CsvParser();
            parser.Read(csvFile);
            return parser.DistinctCars;
    
        }

        public List<BetterMatchMaking.Library.Data.Line> LoadFromCode()
        {
            var parser = new BetterMatchMaking.Library.Data.CsvParser();

            
            parser.Add(1, 100, -92014, -92014, "Rebellion iRacing Team #White", 0);
            parser.Add(1, 100, -92014, 141847, "Mickael Lamoureux", 4697);
            parser.Add(1, 100, -92014, 293434, "Michael M Meier", 4465);

            parser.Add(1, 100, -86236, -86236, "Rebellion iRacing Team #Black", 0);
            parser.Add(1, 100, -86236, 221010, "Kelian Tocqueville", 4261);
            parser.Add(1, 100, -86236, 313425, "Cyril Deforge", 4047);
            parser.Add(1, 100, -86236, 329766, "Maxime Duval", 4047);

            parser.Add(1, 100, -112572, -112572, "Rebellion iRacing Team #Silver", 0);
            parser.Add(1, 100, -112572, 281664, "Sebastien Mallet", 3343);
            parser.Add(1, 100, -112572, 130898, "Cyril Gitzhoffer", 3342);
            parser.Add(1, 100, -112572, 337108, "Frédéric Paolino", 3057);

            parser.Add(1, 100, -121343, -121343, "Rebellion iRacing Team #Gold", 0);
            parser.Add(1, 100, -121343, 234038, "Xav Bourgeois", 3010);
            parser.Add(1, 100, -121343, 296447, "Dario Sullo", 2990);
            parser.Add(1, 100, -121343, 305737, "Nicolas Bellet", 2725);

            parser.Add(1, 100, -119057, -119057, "Rebellion iRacing Team #Special", 0);
            parser.Add(1, 100, -119057, 300677, "Alfonso Asenjo", 2457);
            parser.Add(1, 100, -119057, 301335, "Ruelle Franck", 2393);

            parser.Add(1, 100, -135485, -135485, "Rebellion iRacing Team #Special2", 0);
            parser.Add(1, 100, -135485, 335977, "Thomas Corriger", 2350);
            parser.Add(1, 100, -135485, 222457, "Sébastien Poidevin", 2158);
            parser.GroupDistinctCars(); // don't forget to call this method when your list is finished

            return parser.DistinctCars;
        }
    }
}
