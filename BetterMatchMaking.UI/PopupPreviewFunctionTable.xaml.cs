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

        public int X { get; set; }
        public int A { get; set; }
        public int B { get; set; }

        public int Min { get; set; }

        public void Render()
        {
            List<Preview> items = new List<Preview>();
            for (int i = 6000; i >= 500; i-=125)
            {
                items.Add(new Preview
                {
                    Sof = i,
                    TargetDiff = Calc(i)
                });
            }
            grid.ItemsSource = items;
        }

        private double Calc(int ir)
        {
            if (!(X == 0 || A == 0 || B == 0))
            {
                double r = (Convert.ToDouble(ir) / Convert.ToDouble(X)) * Convert.ToDouble(A);
                r += B;
                r = Math.Max(r, Min);
                return r;
            }
            return Min;
        }
    }

    public class Preview
    {
        public double Sof { get; set; }
        public double TargetDiff { get; set; }
    }
}
