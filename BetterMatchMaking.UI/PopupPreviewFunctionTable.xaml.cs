using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BetterMatchMaking.UI
{
    /// <summary>
    /// Logique d'interaction pour PopupPreviewFunctionTable.xaml
    /// </summary>
    public partial class PopupPreviewFunctionTable : Window
    {
        public PopupPreviewFunctionTable()
        {
            InitializeComponent();
        }

        public int StartingIR { get; set; }
        public int StartingThreshold { get; set; }
        public int ExtraThresoldPerK { get; set; }

        public void Render()
        {
            List<Preview> items = new List<Preview>();
            for (int i = 6000; i >= 500; i-=125)
            {
                items.Add(new Preview
                {
                    Sof = i,
                    TargetDiff = Library.Calc.SofDifferenceEvaluator.EvalFormula(StartingIR, StartingThreshold, ExtraThresoldPerK, i)
                });
            }
            grid.ItemsSource = items;
        }

        
    }

    public class Preview
    {
        public double Sof { get; set; }
        public double TargetDiff { get; set; }
    }
}
