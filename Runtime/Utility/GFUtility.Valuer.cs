using System;
using GameFramework;
using TMPro;


public static partial class GFUtility
{
    public class Valuer
    {
        public static string GetByteLengthString(long byteLength)
        {
            if (byteLength < 1024L) // 2 ^ 10
            {
                return Utility.Text.Format("{0} Bytes", byteLength);
            }

            if (byteLength < 1048576L) // 2 ^ 20
            {
                return Utility.Text.Format("{0:F2} KB", byteLength / 1024f);
            }

            if (byteLength < 1073741824L) // 2 ^ 30
            {
                return Utility.Text.Format("{0:F2} MB", byteLength / 1048576f);
            }

            if (byteLength < 1099511627776L) // 2 ^ 40
            {
                return Utility.Text.Format("{0:F2} GB", byteLength / 1073741824f);
            }

            if (byteLength < 1125899906842624L) // 2 ^ 50
            {
                return Utility.Text.Format("{0:F2} TB", byteLength / 1099511627776f);
            }

            if (byteLength < 1152921504606846976L) // 2 ^ 60
            {
                return Utility.Text.Format("{0:F2} PB", byteLength / 1125899906842624f);
            }

            return Utility.Text.Format("{0:F2} EB", byteLength / 1152921504606846976f);
        }

        public static string HideChars(string str)
        {
            char[] emailChars = str.ToCharArray();
            if (emailChars.Length > 5)
            {
                for (int i = 3; i < emailChars.Length - 2; i++)
                {
                    emailChars[i] = '*';
                }
            }
            else
            {
                for (int i = 1; i < emailChars.Length - 1; i++)
                {
                    emailChars[i] = '*';
                }
            }
            return emailChars.ArrayToString();
        }
        /// <summary>
        /// 转换成1K 1M
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static string ToCoins(int num)
        {
            if (num >= 100000000)
                return (num / 1000000D).ToString("0.#M");
            if (num >= 1000000)
                return (num / 1000000D).ToString("0.##M");
            if (num >= 100000)
                return (num / 1000D).ToString("0K");
            if (num >= 100000)
                return (num / 1000D).ToString("0.#K");
            if (num >= 1000)
                return (num / 1000D).ToString("0.##K");
            return num.ToString("#,0");
        }
        public static string ToCoins(float num)
        {
            return ToCoins(Valuer.RoundToInt(num));
        }
        /// <summary>
        /// 转换成美元格式
        /// </summary>
        /// <param name="usd"></param>
        /// <returns></returns>
        public static string ToUsd(float usd)
        {
            return Utility.Text.Format("{0:N2}", usd);
        }
        public static string ToUsd(long usdCent)
        {
            return ToUsd(usdCent * 0.01f);
        }
        public static string Float2String(float v, int dotNum)
        {
            if (v - (int)v != 0)
            {
                return v.ToString(Utility.Text.Format("N{0}", dotNum));
            }
            else
            {
                return v.ToString();
            }
        }
        /// <summary>
        /// 格式化秒为 00:00:00
        /// </summary>
        /// <param name="seconds">单位秒</param>
        /// <returns></returns>
        public static string ToTime(float seconds)
        {
            return System.TimeSpan.FromSeconds(seconds).ToString("hh\\:mm\\:ss");
        }

        public static int RoundToInt(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}