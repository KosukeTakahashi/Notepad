using System;

namespace Notepad
{
    class Utils
    {
        public enum Encoding
        {
            UTF8,
            ASCII,
            SJIS,
            HEX
        }

        public static int CountLines(string str)
        {
            var split = str.Split('\n');
            return split.Length;
        }

        public static int CalcNumOfDigits(int n)
        {
            var log = Math.Log10(n);
            if (log == ((int)log))
                return (int)log + 1;
            else
                return (int)Math.Ceiling(log);
        }

        public static string GenCounterString(string str)
        {
            var count = CountLines(str);
            var digits = CalcNumOfDigits(count);

            var format = "{0:";
            for (int n = 0; n < digits; n++)
                format += "0";
            format += "}";

            var counterString = "";
            
            if (count == 1)
            {
                counterString += string.Format(format, 1);
                counterString += "\n";
            }
            else
            {
                for (int n = 1; n < count; n++)
                {
                    counterString += string.Format(format, n);
                    counterString += "\n";
                }

                counterString += string.Format(format, count);
            }

            return counterString;
        }
    }
}
