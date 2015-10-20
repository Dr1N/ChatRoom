using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MyChat
{
    class MyConverter : IValueConverter
    {
        public double Ratio { get; set; }

        public MyConverter() { }

        public MyConverter(double r)
        {
            this.Ratio = r;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double val;
            try
            {
                val = (Double)value;
                return val * this.Ratio;
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
