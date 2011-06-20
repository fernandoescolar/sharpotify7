using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Text;

namespace Sharpotify7.Library.Util
{
    public class ASCIIEncoding
    {
        public static byte[] GetBytes(string s)
        {
            byte[] retval = new byte[s.Length];
            for (int ix = 0; ix < s.Length; ++ix)
            {
                char ch = s[ix];
                if (ch <= 0x7f) retval[ix] = (byte)ch;
                else retval[ix] = (byte)'?';
            }
            return retval;
        }

        public static string GetString(byte[] bytes)
        {
            return GetString(bytes, 0, bytes.Length);
        }
        public static string GetString(byte[] bytes, int start, int size)
        {
            StringBuilder sb = new StringBuilder();
            for (int ix = start; ix < size; ++ix)
            {
                char ch = (char)bytes[ix];
                if (ch <= 0x7f) sb.Append(ch);
                else sb.Append('?');
            }
            return sb.ToString();
        }
    }
}
