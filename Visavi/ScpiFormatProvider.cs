using System;
using System.Collections;
using System.Globalization;
using System.Linq;

namespace Visavi
{
    class ScpiFormatProvider : IFormatProvider, ICustomFormatter
    {
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg is bool b)                    // format boolean as 0/1
            {
                return b ? "1" : "0";
            }
            else if (arg is IFormattable argForm) // format IFormattable using InvariantCulture
            {
                return argForm.ToString(format, CultureInfo.InvariantCulture);
            }
            else if (arg is IEnumerable arr)      // format arrays
            {
                return String.Join(",", arr.Cast<object>().Select(v => Format(format, v, this)));
            }
            else if (arg != null)
            {
                return arg.ToString();
            }
            else
            {
                return String.Empty;
            }
        }

        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
                return this;
            return CultureInfo.InvariantCulture.GetFormat(formatType);
        }
    }

}
