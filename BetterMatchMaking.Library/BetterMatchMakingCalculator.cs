// Better Splits Project - https://board.ipitting.com/bettersplits
// Written by Sebastien Mallet (seubiracing@gmail.com - iRacer #281664)
// --------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterMatchMaking.Library.Data;

namespace BetterMatchMaking.Library
{
    public class BetterMatchMakingCalculator : Calc.IMatchMaking
    {
        Calc.IMatchMaking instance;

        public BetterMatchMakingCalculator(string algorithm)
        {
            var type = this.GetType().Assembly.GetType("BetterMatchMaking.Library.Calc." + algorithm);
            instance = Activator.CreateInstance(type) as Calc.IMatchMaking;
        }

        #region Wrapping instance
        public List<Split> Splits
        {
            get { return instance.Splits; }
        }

        public bool UseParameterClassPropMinPercent
        {
            get { return instance.UseParameterClassPropMinPercent; }
        }

        public bool UseParameterRatingThreshold
        {
            get { return instance.UseParameterRatingThreshold; }
        }

        public bool UseParameterMaxSofDiff
        {
            get { return instance.UseParameterMaxSofDiff; }
        }

        public virtual bool UseParameterMaxSofFunct
        {
            get { return instance.UseParameterMaxSofFunct; }

        }

        public bool UseParameterTopSplitException
        {
            get { return instance.UseParameterTopSplitException; }
        }

        public bool UseParameterMostPopulatedClassInEverySplits
        {
            get { return instance.UseParameterMostPopulatedClassInEverySplits; }
        }

        public virtual bool UseParameterMinCars
        {
            get { return instance.UseParameterMinCars; }
        }

        


        public int ParameterClassPropMinPercentValue
        {
            get { return instance.ParameterClassPropMinPercentValue; }
            set { instance.ParameterClassPropMinPercentValue = value; }
        }
        public int ParameterRatingThresholdValue
        {
            get { return instance.ParameterRatingThresholdValue; }
            set { instance.ParameterRatingThresholdValue = value; }
        }
        public int ParameterMaxSofDiffValue
        {
            get { return instance.ParameterMaxSofDiffValue; }
            set { instance.ParameterMaxSofDiffValue = value; }
        }
        
        public int ParameterMaxSofFunctStartingIRValue
        {
            get { return instance.ParameterMaxSofFunctStartingIRValue; }
            set { instance.ParameterMaxSofFunctStartingIRValue = value; }
        }
        public int ParameterMaxSofFunctStartingThreshold
        {
            get { return instance.ParameterMaxSofFunctStartingThreshold; }
            set { instance.ParameterMaxSofFunctStartingThreshold = value; }
        }
        public int ParameterMaxSofFunctExtraThresoldPerK
        {
            get { return instance.ParameterMaxSofFunctExtraThresoldPerK; }
            set { instance.ParameterMaxSofFunctExtraThresoldPerK = value; }
        }
        public int ParameterTopSplitExceptionValue
        {
            get { return instance.ParameterTopSplitExceptionValue; }
            set { instance.ParameterTopSplitExceptionValue = value; }
        }
        public int ParameterMostPopulatedClassInEverySplitsValue
        {
            get { return instance.ParameterMostPopulatedClassInEverySplitsValue; }
            set { instance.ParameterMostPopulatedClassInEverySplitsValue = value; }
        }

        public int ParameterMinCarsValue
        {
            get { return instance.ParameterMinCarsValue; }
            set { instance.ParameterMinCarsValue = value; }
        }
        #endregion

        public int FieldSize { get; set; }
        public List<Line> EntryList { get; private set; }

        public static void CopyParameters(Calc.IMatchMaking source, Calc.IMatchMaking target)
        {
            // copy all .ParameterXXX properties from source to target instance
            var type = typeof(Calc.IMatchMaking);
            var parameters = (from r in type.GetProperties() where r.Name.StartsWith("Parameter") select r).ToList();
            foreach (var parameter in parameters)
            {
                var o = parameter.GetValue(source);
                parameter.SetValue(target, o);
            }
        }

        public void Compute(List<Line> data, int fieldSize)
        {
            FieldSize = fieldSize;
            Compute(data);
        }

        public void Compute(List<Line> data)
        {
            EntryList = data;
            processStart = DateTime.Now;
            instance.Compute(data, FieldSize);
            processEnd = DateTime.Now;

        }


        DateTime processStart;
        DateTime processEnd;

        public Data.Audit GetAudit()
        {
            Data.Audit ret = new Audit();
            ret.Success = true;
            ret.ComputingTimeInMs = Convert.ToInt32(processEnd.Subtract(processStart).TotalMilliseconds);
            ret.SplitsExceedsFieldSize = new List<int>();
            ret.CarsMissingInAnySplit = new List<int>();
            ret.NotExpectedCarsRegistred = new List<int>();
            ret.IROrderInconsistencySplits = new List<int>();
            ret.Cars = 0;
            ret.Splits = 0;



            List<int> allcars = (from r in EntryList select r.driver_id).ToList();

            
            foreach (var split in Splits)
            {
                ret.Cars += split.TotalCarsCount;
                ret.Splits++;
                foreach (var car in split.AllCars)
                {
                    int car_id = car.driver_id;
                    if (allcars.Contains(car_id))
                    {
                        allcars.Remove(car_id);
                    }
                    else
                    {
                        ret.NotExpectedCarsRegistred.Add(car_id);
                        ret.Success = false;
                    }
                }

                if (split.TotalCarsCount > FieldSize)
                {
                    ret.SplitsExceedsFieldSize.Add(split.Number);
                    ret.Success = false;
                }

                foreach (int classIndex in split.GetClassesIndex())
                {
                    double minIR = (from r in split.GetClassCars(classIndex) select r.rating).Min();

                    var nextSplits = (from r in Splits where r.Number >= split.Number + 1 select r).ToList();
                    var nextCars = new List<Data.Line>();
                    foreach (var nextSplit in nextSplits)
                    {
                        var cars = nextSplit.GetClassCars(classIndex);
                        if (cars != null) nextCars.AddRange(cars);
                    }
                     
                    var higherIRs = (from r in nextCars where r.rating > minIR + 1 select r).Count();
                    if(higherIRs > 0)
                    {
                        ret.IROrderInconsistencySplits.Add(split.Number);
                    }

                    
                }
            }



            if (allcars.Count > 0 )
            {
                ret.CarsMissingInAnySplit.AddRange(allcars);
                ret.Success = false;
            }

            
            int splitsHavingDiffClassesSof = (from r in Splits where r.ClassesSofDiff > 0 select r.ClassesSofDiff).Count();
            if (splitsHavingDiffClassesSof > 0)
            {
                ret.AverageSplitClassesSofDifference = Convert.ToInt32(Math.Round((from r in Splits where r.ClassesSofDiff > 0 select r.ClassesSofDiff).Average()));
            }

            double splitAvgSize = (from r in Splits select r.TotalCarsCount).Average();
            double splitMinSize = (from r in Splits select r.TotalCarsCount).Min();
            ret.MinSplitSizePercent = splitMinSize / splitAvgSize;
            

            return ret;
        }
    }
}

