using System;
using System.Text;

namespace AuthServer.Helpers
{
    public static class PeopleCoreCrypt
    {
        public static string Decrypt(string xToken = "")
        {
            try
            {
                long xCurrent = 0, xPrevious = 1, xNext = 0;
                string rVal = "";

                xToken = DecryptToDec4(xToken);

                for (int i = xToken.Length; i >= 1; i--)
                {
                    xNext = xCurrent;
                    xCurrent = xPrevious;
                    xPrevious = xNext + xCurrent;
                    char c = (char)(xToken[xToken.Length - i] - xCurrent - Math.Abs(xCurrent - i));
                    rVal += c;
                }

                xToken = "";
                for (int i = rVal.Length; i >= 1; i--)
                {
                    xToken += rVal[i - 1];
                }

                return xToken;
            }
            catch
            {
                return "";
            }
        }

        public static string Encrypt(string xToken = "")
        {
            try
            {
                long xCurrent = 0, xPrevious = 0, xNext = 1;
                string rVal = "";

                for (int i = xToken.Length; i >= 1; i--)
                {
                    xPrevious = xCurrent;
                    xCurrent = xNext;
                    xNext = xPrevious + xCurrent;

                    char c = (char)(xToken[i - 1] + xCurrent + Math.Abs(xCurrent - i));
                    rVal += EncryptToHex4(c);
                }

                return rVal;
            }
            catch
            {
                return "";
            }
        }

        private static string EncryptToHex(string tval)
        {
            string hex = ((int)tval[0]).ToString("X");
            return hex.Length == 1 ? "0" + hex : hex;
        }

        private static string DecryptToDec(string tval)
        {
            string ireturn = "";
            int x = tval.Length;
            double f = x;

            if (x > 0)
            {
                x /= 2;
                if (x < (f / 2)) x++;
            }

            for (int i = 0; i < x; i++)
            {
                ireturn += (char)Str2Byte(tval, i);
            }
            return ireturn;
        }

        private static string EncryptToHex4(char tval)
        {
            return ((int)tval).ToString("X4");
        }

        private static string DecryptToDec4(string tval)
        {
            string ireturn = "";
            int x = tval.Length;

            if (x > 0)
            {
                x /= 4;
            }

            for (int i = 0; i < x; i++)
            {
                ireturn += (char)Convert.ToInt32(tval.Substring(i * 4, 4), 16);
            }

            return ireturn;
        }

        private static int Str2Byte(string s, int index)
        {
            int b1, b2;
            string s1 = s.Substring(index * 2, 1);
            string s2 = s.Substring(index * 2 + 1, 1);

            b1 = s1[0] >= 'A' ? s1[0] - 'A' + 10 : s1[0] - '0';
            b2 = s2[0] >= 'A' ? s2[0] - 'A' + 10 : s2[0] - '0';

            return b1 * 16 + b2;
        }

        private static int Str2ByteArray(string s, byte[] b)
        {
            int l = s.Length / 2;
            for (int i = 0; i < l; i++)
            {
                b[i] = (byte)Str2Byte(s, i);
            }
            return l;
        }

        public static string DecryptLogs(string xToken = "")
        {
            string rVal = "";
            if (xToken.Length < 22 && xToken.Length > 0)
            {
                long xCurrent = 0, xPrevious = 1, xNext = 0;
                for (int i = xToken.Length; i >= 1; i--)
                {
                    xNext = xCurrent;
                    xCurrent = xPrevious;
                    xPrevious = xNext + xCurrent;
                    char c = (char)(xToken[xToken.Length - i] - xCurrent - Math.Abs(xCurrent - i));
                    rVal += c;
                }
                xToken = "";
                for (int i = rVal.Length; i >= 1; i--)
                {
                    xToken += rVal[i - 1];
                }
            }
            else
            {
                xToken = "";
            }

            return xToken;
        }

        public static string EncryptLogs(string xToken = "")
        {
            string rVal = "";
            if (xToken.Length <= 22 && xToken.Length > 0)
            {
                long xCurrent = 0, xPrevious = 0, xNext = 1;
                for (int i = xToken.Length; i >= 1; i--)
                {
                    xPrevious = xCurrent;
                    xCurrent = xNext;
                    xNext = xPrevious + xCurrent;
                    rVal += (char)(xToken[i - 1] + xCurrent + Math.Abs(xCurrent - i));
                }
            }

            return rVal;
        }
    }
}
