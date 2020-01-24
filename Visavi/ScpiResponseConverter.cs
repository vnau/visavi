using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Visavi
{
    class ScpiResponseConverter
    {
        /// <summary>
        /// Convert SCPI response to specified type
        /// </summary>
        /// <typeparam name="T">Expected type of response</typeparam>
        /// <param name="response">SCPI response</param>
        /// <returns>Response converted to specified type</returns>
        public static T ConvertFromString<T>(string response)
        {
            if (typeof(T).IsArray)
            {
                Type elementType = typeof(T).GetElementType();
                TypeConverter typeConverter = TypeDescriptor.GetConverter(elementType);
                var res1 = response.Trim(new[] { '\r', '\n', '"', ' ' }).Split(',');
                Array array;
                // Workaround for Keysight returning EMPTY strings if there is no items in list
                if (res1.Length == 1 && res1.First() == "EMPTY")
                    array = Array.CreateInstance(elementType, 0);
                else
                {
                    array = Array.CreateInstance(elementType, res1.Length);

                    for (int i = 0; i < res1.Length; i++)
                        array.SetValue(typeConverter.ConvertFromString(null, CultureInfo.InvariantCulture, res1[i]), i);
                }

                return (T)(object)array;
            }
            else
            {
                TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(T));
                return (T)typeConverter.ConvertFromString(null, CultureInfo.InvariantCulture, response);
            }
        }
    }
}
