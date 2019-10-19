using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterMatchMaking.Library.Data
{
    public class Split
    {
        public int Number { get; set; }


        public Split()
        {

        }

        public Split(int number)
        {
            Number = number;
        }

        public string Class1
        {
            get
            {
                if (Class1Cars == null) return null;
                return Class1Cars.Count + " " + Class1Name
                    + "\n(SoF " + Class1Sof + ")";
            }
        }
        public string Class2
        {
            get
            {
                if (Class2Cars == null) return null;
                return Class2Cars.Count + " " + Class2Name
                    + "\n(SoF " + Class2Sof + ")";
            }
        }

        public string Class3
        {
            get
            {
                if (Class3Cars == null) return null;
                return Class3Cars.Count + " " + Class3Name
                    + "\n(SoF " + Class3Sof + ")";
            }
        }



        public string Class4
        {
            get
            {
                if (Class4Cars == null) return null;
                return Class4Cars.Count + " " + Class4Name
                    + "\n(SoF " + Class4Sof + ")";
            }
        }

        public int Class1Target { get; set; }
        public int Class2Target { get; set; }
        public int Class3Target { get; set; }
        public int Class4Target { get; set; }


        public int TotalTarget
        {
            get {
                return Class1Target
                + Class2Target
                + Class3Target
                + Class4Target;
            }
        }

        public List<Line> Class1Cars { get; set; }
        public List<Line> Class2Cars { get; set; }
        public List<Line> Class3Cars { get; set; }
        public List<Line> Class4Cars { get; set; }


        public string Class1Name { get; set; }
        public string Class2Name { get; set; }
        public string Class3Name { get; set; }
        public string Class4Name { get; set; }

        public int Class1Id { get; set; }
        public int Class2Id { get; set; }
        public int Class3Id { get; set; }
        public int Class4Id { get; set; }


        public List<Line> PickClassContent(int i)
        {
            List<Line> ret = new List<Line>();
            List<Line> dataToPick = null;

            if (i == 0) dataToPick = Class1Cars;
            else if (i == 1) dataToPick = Class2Cars;
            else if (i == 2) dataToPick = Class3Cars;
            else if (i == 3) dataToPick = Class4Cars;

            foreach (var c in dataToPick) ret.Add(c);

            dataToPick.Clear();

            if (i == 0) Class1Cars = null;
            else if (i == 1) Class2Cars = null;
            else if (i == 2) Class3Cars = null;
            else if (i == 3) Class4Cars = null;


            if (i == 0) Class1Sof = 0;
            else if (i == 1) Class2Sof = 0;
            else if (i == 2) Class3Sof = 0;
            else if (i == 3) Class4Sof = 0;

            if (i == 0) Class1Target = 0;
            else if (i == 1) Class2Target = 0;
            else if (i == 2) Class3Target = 0;
            else if (i == 3) Class4Target = 0;

            return ret;
        }



        public int Class1Sof { get; set; }
        public int Class2Sof { get; set; }
        public int Class3Sof { get; set; }
        public int Class4Sof { get; set; }


        public string Info { get; set; }


        public List<Line> GetClassCars(int i)
        {
            List<Line> theClass = Tools.GetProperty<List<Line>>(this, "Class{i}Cars", i);
            if (theClass != null && theClass.Count == 0) return null;
            return theClass;
        }

        public List<Line> PickClassCars(int carclass, int? maxcars = null, bool lastests = false)
        {
            List<Line> pick = new List<Line>();
            List<Line> theClass = Tools.GetProperty<List<Line>>(this, "Class{i}Cars", carclass);

            if(theClass == null)
            {
                return pick;
            }

            if(maxcars == null)
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

            if(theClass.Count == 0)
            {
                Tools.SetProperty<List<Line>>(this, "Class{i}Cars", carclass, null);
                Tools.SetProperty<List<Line>>(this, "Class{i}Name", carclass, null);
            }

            return pick;
        }

        public void AddClassCars(int carclass, List<Line> newcars)
        {
            List<Line> theClass = Tools.GetProperty<List<Line>>(this, "Class{i}Cars", carclass);
            theClass.AddRange(newcars);
            theClass = (from r in theClass orderby r.rating descending select r).ToList();
            Tools.SetProperty<List<Line>>(this, "Class{i}Cars", carclass, theClass);

            RefreshSofs();
        }

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

        public int CountClassCars(int carclass)
        {
            List<Line> theClass = Tools.GetProperty<List<Line>>(this, "Class{i}Cars", carclass);
            if (theClass == null) return 0;
            return theClass.Count;
        }


        public int GlobalSof { get; set; }


        public int GetClassSof(int i)
        {
            int ret = Tools.GetProperty<int>(this, "Class{i}Sof", i);
            return ret;
        }


        public int GetClassTarget(int i)
        {
            int ret = Tools.GetProperty<int>(this, "Class{i}Target", i);
            return ret;
        }

        public void SetClassTarget(int i, int target)
        {
            Tools.SetProperty(this, "Class{i}Target", i, target);
        }

        public void SetClass(int i, List<Line> cars, int id)
        {
            Tools.SetProperty<int>(this, "Class{i}Id", i, id);
            Tools.SetProperty<List<Line>>(this, "Class{i}Cars", i, cars);
            RefreshSofs();
        }

        public void SetClass(int i, int id)
        {
            Tools.SetProperty<int>(this, "Class{i}Id", i, id);
            var t = Tools.GetProperty<List<Line>>(this, "Class{i}Cars", i);
            if(t == null)
            {
                t = new List<Line>();
                Tools.SetProperty<List<Line>>(this, "Class{i}Cars", i, t);
            }


            
        }

        public int GetClassId(int i)
        {
            return Tools.GetProperty<int>(this, "Class{i}Id", i);
        }
        public void SetClassName(int i, string name)
        {
            Tools.SetProperty<string>(this, "Class{i}Name", i, name);
        }



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

        


        internal void RefreshSofs()
        {
            if (Class1Cars != null) Class1Sof = Calc.Tools.Sof((from r in Class1Cars where r.rating > 0 select r.rating).ToList());
            if (Class2Cars != null) Class2Sof = Calc.Tools.Sof((from r in Class2Cars where r.rating > 0 select r.rating).ToList());
            if (Class3Cars != null) Class3Sof = Calc.Tools.Sof((from r in Class3Cars where r.rating > 0 select r.rating).ToList());
            if (Class4Cars != null) Class4Sof = Calc.Tools.Sof((from r in Class4Cars where r.rating > 0 select r.rating).ToList());
            GlobalSof = Calc.Tools.Sof((from r in AllCars where r.rating > 0 select r.rating).ToList());

        }

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

                double pcent = Math.Round(delta / max * 100);

                return pcent;
            }
        }

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


        public int GetClassesCount()
        {
            int i = 0;
            if (Class1Cars != null && Class1Cars.Count > 0) i++;
            if (Class2Cars != null && Class2Cars.Count > 0) i++;
            if (Class3Cars != null && Class3Cars.Count > 0) i++;
            if (Class4Cars != null && Class4Cars.Count > 0) i++;
            return i;
        }




    }
}
