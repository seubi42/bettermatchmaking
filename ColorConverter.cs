using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BetterMatchMaking
{
    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex("\\([0-9][0-9]%\\)");
            string input = value as string;
            if(!String.IsNullOrWhiteSpace(input))
            {
                var m = r.Match(input);
                if (m.Success)
                {
                    int percent = 0;
                    if(Int32.TryParse(m.Value.Substring(1,2), out percent))
                    {
                        return GetPercentColor(percent);
                    }
                    
                }
            }
           
            return DependencyProperty.UnsetValue;
        }

        public static SolidColorBrush GetPercentColor(int percent)
        {
            if (percent < 7)
            {
                return new SolidColorBrush(Color.FromArgb(255, 91, 156, 74));
            }
            else if (percent < 14)
            {
                return new SolidColorBrush(Color.FromArgb(255, 90, 142, 53));
            }
            else if (percent < 21)
            {
                return new SolidColorBrush(Color.FromArgb(255, 153, 168, 59));
            }
            else if (percent < 28)
            {
                return new SolidColorBrush(Color.FromArgb(255, 236, 192, 65));
            }
            else if (percent < 35)
            {
                return new SolidColorBrush(Color.FromArgb(255, 246, 175, 65));
            }
            else if (percent < 60)
            {
                return new SolidColorBrush(Color.FromArgb(255, 232, 131, 61));
            }
            return new SolidColorBrush(Color.FromArgb(255, 193, 54, 53));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
