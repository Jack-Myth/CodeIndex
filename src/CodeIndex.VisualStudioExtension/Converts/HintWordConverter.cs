using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CodeIndex.VisualStudioExtension
{
    [ValueConversion(typeof(List<Models.HintWord>), typeof(List<String>))]
    public class HintWordConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value == null)
            {
                return null;
            }
            var hintWordStrList = new List<string>();
            foreach(var word in value as List<Models.HintWord>)
            {
                hintWordStrList.Add(word.Word);
            }
            return hintWordStrList;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            var hintWordStrList = new List<Models.HintWord>();
            foreach (var word in value as List<string>)
            {
                if(word!="")
                {
                    hintWordStrList.Add(new Models.HintWord() { Word = word });
                }
            }
            return hintWordStrList;
        }
    }
}
