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
    /*
     *  For future release:
     *  
     *  To make something easy to read in debugger,
     *  This C# class is based on groups of 4 properties.
     *  Exemples : Class1Name, Class2Name, Class3Name, Class4Name.
     *             Class1Id,   Class2Id,   Class3Id,   Class4Id.
     *  
     *  + Theses properties have also generic Setters and Getters when needed:
     *    like: SetClassName(i, xxx)
     * 
     *  /!\ It is possible to optimize this class and to remove the 4 classed limitation
     *  to manage illimited classes, by relacing this groups of properties by array or Lists.
     *  But it will be harder to read or to set breakpoints in code.
     *  That's why it was not the case in this version.
     *  
     */

    [Serializable]
    /// <summary>
    /// Race split description.
    /// It includes the split entry list, SoF and other value.
    ///     All that values are splitted in properties corresponding to each classes.
    /// </summary>
    public class Split
    {
        /// <summary>
        /// The split number. Starting from 1.
        /// </summary>
        public int Number { get; set; }


        #region Constructors (with number parameter or not)
        public Split()
        {
            Info = "";
        }

        public Split(int number)
        {
            Info = "";
            Number = number;
        }
        #endregion


        #region public string Class{i} {get; }

        /// <summary>
        /// Text description of Class1
        /// Ex: "11 C7"
        ///   + "(SoF 3000)"
        /// </summary>
        public string Class1
        {
            get
            {
                if (Class1Cars == null) return null;
                return Class1Cars.Count + " " + Class1Name
                    + "\n(SoF " + Class1Sof + ")";
            }
        }

        /// <summary>
        /// Text description of Class2
        /// Ex: "13 GT3"
        ///   + "(SoF 3500)"
        /// </summary>
        public string Class2
        {
            get
            {
                if (Class2Cars == null) return null;
                return Class2Cars.Count + " " + Class2Name
                    + "\n(SoF " + Class2Sof + ")";
            }
        }

        /// <summary>
        /// Text description of Class3
        /// Ex: "20 GTE"
        ///   + "(SoF 4000)"
        /// </summary>
        public string Class3
        {
            get
            {
                if (Class3Cars == null) return null;
                return Class3Cars.Count + " " + Class3Name
                    + "\n(SoF " + Class3Sof + ")";
            }
        }


        /// <summary>
        /// Text description of Class4
        /// Ex: "20 GTE"
        ///   + "(SoF 4000)"
        /// </summary>
        public string Class4
        {
            get
            {
                if (Class4Cars == null) return null;
                return Class4Cars.Count + " " + Class4Name
                    + "\n(SoF " + Class4Sof + ")";
            }
        }
        #endregion


        #region public Class{i}Target {get;set;} + Helpers

        /// <summary>
        /// Numeric value to store the targeted cars number in Class1
        /// Can be used by some algorithm
        /// </summary>
        public int Class1Target { get; set; }
        /// <summary>
        /// Numeric value to store the targeted cars number in Class2
        /// Can be used by some algorithm
        /// </summary>
        public int Class2Target { get; set; }
        /// <summary>
        /// Numeric value to store the targeted cars number in Class3
        /// Can be used by some algorithm
        /// </summary>
        public int Class3Target { get; set; }
        /// <summary>
        /// Numeric value to store the targeted cars number in Class4
        /// Can be used by some algorithm
        /// </summary>
        public int Class4Target { get; set; }

        /// <summary>
        /// Return the sum of all Class{i}Target
        /// Corresponding to the fieldsize target for this specific split
        /// Can be used by some algorithm
        /// </summary>
        public int TotalTarget
        {
            get {
                return Class1Target
                + Class2Target
                + Class3Target
                + Class4Target;
            }
        }


        /// <summary>
        /// Getter for Class{i}Target
        /// </summary>
        /// <param name="i"></param>
        /// <returns>Class Index, start from 0</returns>
        public int GetClassTarget(int i)
        {
            int ret = Tools.GetProperty<int>(this, "Class{i}Target", i);
            return ret;
        }

        /// <summary>
        /// Setter for Class{i}Target
        /// </summary>
        /// <param name="i">Class Index, start from 0</param>
        /// <param name="target">new target to store</param>
        public void SetClassTarget(int i, int target)
        {
            Tools.SetProperty(this, "Class{i}Target", i, target);
        }
        #endregion


        #region  public Class{i}Cars {get;set;} + Helpers

        /// <summary>
        /// Entry list of Class1 cars in this split
        /// </summary>
        public List<Line> Class1Cars { get; set; }
        /// <summary>
        /// Entry list of Class2 cars in this split
        /// </summary>
        public List<Line> Class2Cars { get; set; }
        /// <summary>
        /// Entry list of Class3 cars in this split
        /// </summary>
        public List<Line> Class3Cars { get; set; }
        /// <summary>
        /// Entry list of Class4 cars in this split
        /// </summary>
        public List<Line> Class4Cars { get; set; }

        /// <summary>
        /// Getter for Class{i}Cars
        /// </summary>
        /// <param name="i">Class Index, start from 0</param>
        /// <returns>the class entry list</returns>
        public List<Line> GetClassCars(int i)
        {
            List<Line> theClass = Tools.GetProperty<List<Line>>(this, "Class{i}Cars", i);
            if (theClass != null && theClass.Count == 0) return null;
            return theClass;
        }

        /// <summary>
        /// This method get and removes cars in the split to the corresponding class.
        /// Usefull for some algorithm which need to move cars from one split to another.
        /// </summary>
        /// <param name="carclass">Class Index, start from 0</param>
        /// <param name="maxcars">Number of cars to pick. (null=all)</param>
        /// <param name="lastests">If true : Get cars from the bottom of the list (cars with less iRating)
        ///                        If false : Get cars from the top of the list (cars with more iRating)
        /// </param>
        /// <returns></returns>
        public List<Line> PickClassCars(int carclass, int? maxcars = null, bool lastests = false)
        {
            List<Line> pick = new List<Line>();
            List<Line> theClass = Tools.GetProperty<List<Line>>(this, "Class{i}Cars", carclass);

            if (theClass == null)
            {
                return pick;
            }

            if (maxcars == null)
            {
                pick = (from r in theClass select r).ToList();
            }
            else
            {
                if (lastests)
                {
                    pick = (from r in theClass orderby r.rating ascending select r).Take(maxcars.Value).ToList();
                }
                else
                {
                    pick = (from r in theClass orderby r.rating descending select r).Take(maxcars.Value).ToList();
                }
            }

            foreach (var r in pick)
            {
                theClass.Remove(r);
            }

            RefreshSofs();

            if (theClass.Count == 0)
            {
                Tools.SetProperty<List<Line>>(this, "Class{i}Cars", carclass, null);
                Tools.SetProperty<List<Line>>(this, "Class{i}Name", carclass, null);
            }

            return pick;
        }


        /// <summary>
        /// This method appends cars to the corresponding class entry list.
        /// Usefull for some algorithm which need to move cars from one split to another¨.
        /// </summary>
        /// <param name="carclass"></param>
        /// <param name="newcars"></param>
        public void AppendClassCars(int carclass, List<Line> newcars)
        {
            List<Line> theClass = Tools.GetProperty<List<Line>>(this, "Class{i}Cars", carclass);
            if (theClass == null) theClass = new List<Line>();
            theClass.AddRange(newcars);
            theClass = (from r in theClass orderby r.rating descending select r).ToList();
            Tools.SetProperty<List<Line>>(this, "Class{i}Cars", carclass, theClass);

            RefreshSofs();
        }

        /// <summary>
        /// Return the number of cars in the specific Class{i}
        /// </summary>
        /// <param name="carclass">Class Index, start from 0</param>
        /// <returns>Number of cars in that class split</returns>
        public int CountClassCars(int carclass)
        {
            List<Line> theClass = Tools.GetProperty<List<Line>>(this, "Class{i}Cars", carclass);
            if (theClass == null) return 0;
            return theClass.Count;
        }

        /// <summary>
        /// Returns all the cars in the split (all the Classes).
        /// Sum of every Class{i}Cars
        /// </summary>
        public List<Line> AllCars
        {
            get
            {
                List<Line> r = new List<Line>();
                if (Class1Cars != null) r.AddRange(Class1Cars);
                if (Class2Cars != null) r.AddRange(Class2Cars);
                if (Class3Cars != null) r.AddRange(Class3Cars);
                if (Class4Cars != null) r.AddRange(Class4Cars);
                return r;
            }
        }

        /// <summary>
        /// Get the number of cars in this split
        /// (sum of all classes)
        /// </summary>
        public int TotalCarsCount
        {
            get
            {
                int ret = 0;
                if (Class1Cars != null) ret += Class1Cars.Count;
                if (Class2Cars != null) ret += Class2Cars.Count;
                if (Class3Cars != null) ret += Class3Cars.Count;
                if (Class4Cars != null) ret += Class4Cars.Count;
                return ret;
            }
        }


        #endregion


        #region  public Class{i}Name {get;set;} + Helpers


        /// <summary>
        /// Usefull for debugging. Allow you to set a name to the Class1.
        /// Because it is easier to read than the Class Id
        /// </summary>
        public string Class1Name { get; set; }
        /// <summary>
        /// Usefull for debugging. Allow you to set a name to the Class2.
        /// Because it is easier to read than the Class Id
        /// </summary>
        public string Class2Name { get; set; }
        /// <summary>
        /// Usefull for debugging. Allow you to set a name to the Class3.
        /// Because it is easier to read than the Class Id
        /// </summary>
        public string Class3Name { get; set; }
        /// <summary>
        /// Usefull for debugging. Allow you to set a name to the Class4.
        /// Because it is easier to read than the Class Id
        /// </summary>
        public string Class4Name { get; set; }

        /// <summary>
        /// Setter for Class{i}Name. Allow you to set a name to the Class{i}.
        /// Because it is easier to read than the Class Id
        /// </summary>
        /// <param name="i">Class Index, start from 0</param>
        /// <param name="name">Name to store (to help debugging)</param>
        public void SetClassName(int i, string name)
        {
            Tools.SetProperty<string>(this, "Class{i}Name", i, name);
        }

        #endregion


        #region  public Class{i}Id {get;set;} + Helpers

        /// <summary>
        /// To store Class1 iRacing ID
        /// </summary>
        public int Class1Id { get; set; }
        /// <summary>
        /// To store Class2 iRacing ID
        /// </summary>
        public int Class2Id { get; set; }
        /// <summary>
        /// To store Class3 iRacing ID
        /// </summary>
        public int Class3Id { get; set; }
        /// <summary>
        /// To store Class4 iRacing ID
        /// </summary>
        public int Class4Id { get; set; }

        /// <summary>
        /// Getter for Class{i}Name.
        /// </summary>
        /// <param name="i">Class Index, start from 0</param>
        /// <returns>the iRacing class ID</returns>
        public int GetClassId(int i)
        {
            return Tools.GetProperty<int>(this, "Class{i}Id", i);
        }

        #endregion


        #region  public Class{i}Sof {get;set;} + Helpers and GlobalSof

        /// <summary>
        /// Class1 SoF
        /// </summary>
        public int Class1Sof { get; set; }
        /// <summary>
        /// Class2 SoF
        /// </summary>
        public int Class2Sof { get; set; }
        /// <summary>
        /// Class3 SoF
        /// </summary>
        public int Class3Sof { get; set; }
        /// <summary>
        /// Class4 SoF
        /// </summary>
        public int Class4Sof { get; set; }

        /// <summary>
        /// Getter for Class{i}Sof
        /// </summary>
        /// <param name="i">Class Index, start from 0</param>
        /// <returns>the class SoF</returns>
        public int GetClassSof(int i)
        {
            int ret = Tools.GetProperty<int>(this, "Class{i}Sof", i);
            return ret;
        }

        /// <summary>
        /// Get the biggest class SoF in this split
        /// </summary>
        /// <param name="exceptionClassId">(optionnal) do not include this classIndex</param>
        /// <returns></returns>
        public int GetMaxClassSof(int? exceptionClassIndex = null)
        {
            List<int> sofs = new List<int>();
            if (exceptionClassIndex == null || exceptionClassIndex.Value != 0) sofs.Add(Class1Sof);
            if (exceptionClassIndex == null || exceptionClassIndex.Value != 1) sofs.Add(Class2Sof);
            if (exceptionClassIndex == null || exceptionClassIndex.Value != 2) sofs.Add(Class3Sof);
            if (exceptionClassIndex == null || exceptionClassIndex.Value != 3) sofs.Add(Class4Sof);
            return sofs.Max();
        }

        /// <summary>
        /// Get the lowest class SoF in this split
        /// </summary>
        /// <returns></returns>
        public int GetMinClassSof()
        {
            List<int> sofs = new List<int>();
            if (Class1Sof > 0) sofs.Add(Class1Sof);
            if (Class2Sof > 0) sofs.Add(Class2Sof);
            if (Class3Sof > 0) sofs.Add(Class3Sof);
            if (Class4Sof > 0) sofs.Add(Class4Sof);
            return sofs.Max();
        }


        /// <summary>
        /// Global SoF (of the whole split cars)
        /// </summary>
        public int GlobalSof { get; private set; }

        /// <summary>
        /// Method to refresh and recalc classes SoF and global SoF.
        /// To use after a car list change.
        /// </summary>
        internal void RefreshSofs()
        {
            if (Class1Cars != null) Class1Sof = Calc.Tools.Sof((from r in Class1Cars where r.rating > 0 select r.rating).ToList());
            if (Class2Cars != null) Class2Sof = Calc.Tools.Sof((from r in Class2Cars where r.rating > 0 select r.rating).ToList());
            if (Class3Cars != null) Class3Sof = Calc.Tools.Sof((from r in Class3Cars where r.rating > 0 select r.rating).ToList());
            if (Class4Cars != null) Class4Sof = Calc.Tools.Sof((from r in Class4Cars where r.rating > 0 select r.rating).ToList());
            GlobalSof = Calc.Tools.Sof((from r in AllCars where r.rating > 0 select r.rating).ToList());
        }


        /// <summary>
        /// Return the difference between classes SoF in this split
        /// between the lowest and the highest.
        /// The value is a % difference based on highest Sof.
        /// </summary>
        public double ClassesSofDiff
        {
            get
            {
                double min = 0;
                double max = 0;


                List<double> sofs = new List<double>();
                if (Class1Sof > 0) sofs.Add(Class1Sof);
                if (Class2Sof > 0) sofs.Add(Class2Sof);
                if (Class3Sof > 0) sofs.Add(Class3Sof);
                if (Class4Sof > 0) sofs.Add(Class4Sof);

                if (sofs.Count > 0)
                {
                    min = sofs.Min();
                    max = sofs.Max();
                }

                var delta = max - min;

                return Math.Round(delta / max * 100);
            }
        }

        #endregion

        #region To create of set a Class{i} (to init a class)

        /// <summary>
        /// Setter for Class{i} by setting cars list and iRacing Class ID
        /// </summary>
        /// <param name="i">Class Index, start from 0</param>
        /// <param name="cars">The cars (entry list)</param>
        /// <param name="id">iRacing Class ID</param>
        public void SetClass(int i, List<Line> cars, int id)
        {
            Tools.SetProperty<int>(this, "Class{i}Id", i, id);
            Tools.SetProperty<List<Line>>(this, "Class{i}Cars", i, cars);
            RefreshSofs();
        }

        /// <summary>
        /// Setter for Class{i} by setting iRacing Class ID
        /// (the Cars list will be created but empty)
        /// </summary>
        /// <param name="i">Class Index, start from 0</param>
        /// <param name="id">iRacing Class ID</param>
        public void SetClass(int i, int id)
        {
            Tools.SetProperty<int>(this, "Class{i}Id", i, id);
            var t = Tools.GetProperty<List<Line>>(this, "Class{i}Cars", i);
            if (t == null)
            {
                t = new List<Line>();
                Tools.SetProperty<List<Line>>(this, "Class{i}Cars", i, t);
            }
        }
        #endregion


        #region To manage existing Classes
        /// <summary>
        /// Remove ghosts classes.
        /// Set null value to empty classes (count=0).
        /// </summary>
        internal void CleanEmptyClasses()
        {
            for (int i = 0; i < 4; i++)
            {
                List<Line> theClass = Tools.GetProperty<List<Line>>(this, "Class{i}Cars", i);
                if(theClass != null && theClass.Count == 0)
                {
                    Tools.SetProperty<List<Line>>(this, "Class{i}Cars", i, null);
                }
            }
        }


        /// <summary>
        /// Return the number of Classes containing cars
        /// </summary>
        /// <returns>number of Classes containing cars</returns>
        public int GetClassesCount()
        {
            int i = 0;
            if (Class1Cars != null && Class1Cars.Count > 0) i++;
            if (Class2Cars != null && Class2Cars.Count > 0) i++;
            if (Class3Cars != null && Class3Cars.Count > 0) i++;
            if (Class4Cars != null && Class4Cars.Count > 0) i++;
            return i;
        }



        /// <summary>
        /// Returns Class Indexes containing cars
        /// </summary>
        /// <returns>A list containing classes iRacing ID containing cars</returns>
        public List<int> GetClassesIndex()
        {
            List<int> ret = new List<int>();
            if (Class1Cars != null && Class1Cars.Count > 0) ret.Add(0);
            if (Class2Cars != null && Class2Cars.Count > 0) ret.Add(1);
            if (Class3Cars != null && Class3Cars.Count > 0) ret.Add(2);
            if (Class4Cars != null && Class4Cars.Count > 0) ret.Add(3);
            return ret;
        }
        #endregion


        #region Debugging Extra Info

        /// <summary>
        /// You can store anything in Info properties.
        /// Just to help debugging.
        /// </summary>
        public string Info { get; set; }


        /// <summary>
        /// Return the difference between classes SoF in this split
        /// between the lowest and the highest.
        /// Format : "587 (16%)"
        /// Where : - 587 is here the points difference
        ///         - 16 is here the % difference based on highest Sof.
        /// Just to help debugging.
        /// </summary>
        public string ClassesSofDifference
        {
            get
            {
                double min = 0;
                double max = 0;

                List<double> sofs = new List<double>();
                if (Class1Sof > 0) sofs.Add(Class1Sof);
                if (Class2Sof > 0) sofs.Add(Class2Sof);
                if (Class3Sof > 0) sofs.Add(Class3Sof);
                if (Class4Sof > 0) sofs.Add(Class4Sof);

                if (sofs.Count > 0)
                {
                    min = sofs.Min();
                    max = sofs.Max();
                }
                var delta = max - min;

                if (delta > 0)
                {
                    string ret = delta.ToString();
                    ret += " (";
                    double pcent = Math.Round(delta / max * 100);
                    if (pcent < 10) ret += "0";
                    ret += pcent;
                    ret += "%)";
                    return ret;
                }
                return "";
            }
        }

        /// <summary>
        /// This function will ouput Split Number and an Array of cars count for each class.
        /// Format : "Split 1 [10;12;20;0].
        /// Just to help debugging.
        /// </summary>
        /// <returns>a debug string usefull for debugging</returns>
        public override string ToString()
        {

            string ret = "Split " + Number;

            ret += "[";
            if (Class1Cars != null)
            {
                ret += Class1Cars.Count;
            }
            else
            {
                ret += "0";
            }
            ret += ";";
            if (Class2Cars != null)
            {
                ret += Class2Cars.Count;
            }
            else
            {
                ret += "0";
            }
            ret += ";";
            if (Class3Cars != null)
            {
                ret += Class3Cars.Count;
            }
            else
            {
                ret += "0";
            }
            ret += ";";
            if (Class4Cars != null)
            {
                ret += Class4Cars.Count;
            }
            else
            {
                ret += "0";
            }
            ret += "]";

            return ret;

        }
        #endregion



    }
}
