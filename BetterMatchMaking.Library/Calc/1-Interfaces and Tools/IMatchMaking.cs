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
    /// Describes algorithm which are a Match Making (splitting computing) system
    /// </summary>
    public interface IMatchMaking
    {
        /// <summary>
        /// Result of the computing : Splits with classes and cars in it.
        /// </summary>
        List<Data.Split> Splits {get;}


        /// <summary>
        /// Says if the algorithm use the ParameterClassPropMinPercentValue parameter.
        /// If true, you need to set a value to it.
        /// </summary>
        bool UseParameterClassPropMinPercent { get;  }
        /// <summary>
        /// This parameter is used for 'Balanced proportionnal' car class distribution algorithms.
        /// It is a floor value to apply to the % of cars between the less populated class
        /// and the more populated class in a same split.
        /// </summary>
        int ParameterClassPropMinPercentValue { get; set; }

        /// <summary>
        /// Says if the algorithm use the ParameterRatingThresholdValue parameter.
        /// If true, you need to set a value to it.
        /// </summary>
        bool UseParameterRatingThreshold { get;  }
        /// <summary>
        /// This parameter is used for 'RatingThresholded' algorithms.
        /// It allow to set an iRating Thresholded to cut the entry list in 2 distinct list
        /// and compute them separatly before mixing them
        /// </summary>
        int ParameterRatingThresholdValue { get; set; }


        /// <summary>
        /// Says if the algorithm use ParameterRatingThresholdValue parameters.
        /// If true, you need to set a value to them.
        /// ParameterRatingThresholdValue is a constant value.
        /// </summary>
        bool UseParameterMaxSofDiff { get;  }
        /// <summary>
        /// This parameter is used for 'MoveDown' algorithms.
        /// It will set the targetted % SoF difference allowed or not to do a move down.
        /// This value is a constant or minimum value to set.
        /// </summary>
        /// 
        int ParameterMaxSofDiffValue { get; set; }
        /// <summary>
        /// Says if the algorithm use ParameterMaxSofFunctStartingIRValue, ParameterMaxSofFunctStartingThreshold andParameterMaxSofFunctExtraThresoldPerK  parameters.
        /// If true, you need to set a value to them.
        /// ParameterRatingThresholdValue defines an affine function to get the Threshold depending to the irating.
        /// </summary>
        bool UseParameterMaxSofFunct { get; }
        /// <summary>
        /// This parameter is used for 'MoveDown' algorithms.
        /// This option of is a parameter of the affine function to get the maximum diffrence allowed % depending of irating or sof.
        /// You can check BetterMatchMaking.Library.Calc.SofDifferenceEvaluator.EvalFormula to get the formula.
        /// </summary>
        /// 
        int ParameterMaxSofFunctStartingIRValue { get; set; }
        /// <summary>
        /// This parameter is used for 'MoveDown' algorithms.
        /// This option of is a parameter of the affine function to get the maximum diffrence allowed % depending of irating or sof.
        /// You can check BetterMatchMaking.Library.Calc.SofDifferenceEvaluator.EvalFormula to get the formula.
        /// </summary>
        int ParameterMaxSofFunctStartingThreshold { get; set; }
        /// <summary>
        /// This parameter is used for 'MoveDown' algorithms.
        /// This option of is a parameter of the affine function to get the maximum diffrence allowed % depending of irating or sof.
        /// You can check BetterMatchMaking.Library.Calc.SofDifferenceEvaluator.EvalFormula to get the formula.
        /// </summary>
        int ParameterMaxSofFunctExtraThresoldPerK { get; set; }

        /// <summary>
        /// Says if the algorithm use the ParameterTopSplitExceptionValue parameter.
        /// If true, you need to set a value to it.
        /// </summary>
        bool UseParameterTopSplitException { get;  }
        /// <summary>
        /// This parameter is used for 'MoveDown' algorithms.
        /// It set to 1, Top Split will not have any move down to keep all car class in it. 
        /// </summary>
        int ParameterTopSplitExceptionValue { get; set; }


        /// <summary>
        /// Says if the algorithm use the ParameterMostPopulatedClassInEverySplitsValue parameter.
        /// If true, you need to set a value to it.
        /// </summary>
        bool UseParameterMostPopulatedClassInEverySplits { get;  }
        /// <summary>
        /// This parameter is used for 'MoveDown' algorithms.
        /// It set to 1, the most populated class will not have any move down.
        /// It means, if 1, every splits will containt the most populated class.
        /// </summary>
        int ParameterMostPopulatedClassInEverySplitsValue { get; set; }




        /// <summary>
        /// Says if the algorithm use the ParameterMinCarsValue parameter.
        /// If true, you need to set a value to it.
        /// </summary>
        bool UseParameterMinCars { get; }
        /// <summary>
        /// This parameter is used for 'AffineDistribution' algorithms.
        /// You can set the number of minimum cars you want to insure
        /// in the less populated car class
        /// </summary>
        int ParameterMinCarsValue { get; set; }








        /// <summary>
        /// Method to launch the algorithm.
        /// </summary>
        /// <param name="data">The entry list. Please see BetterMatchMaking.Library.Data.CsvParser to fill it.</param>
        /// <param name="fieldSize">Field size.</param>
        void Compute(List<Data.Line> data, int fieldSize);
    }
}
