using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SmallHorse.Spider.Craigslist
{
    static class Assert
    {
        // Probably, FailedException instances should be created
        // only from within the Assert class.
        public class FailedException : ApplicationException
        {
            public FailedException(string s) : base(s) { }
        }

        [System.Diagnostics.Conditional("ASSERT")]
        public static void Test(bool condition)
        {
            if (condition) { return; }
            throw new FailedException("Assertion failed.");
        }

        [System.Diagnostics.Conditional("ASSERT")]
        public static void Test(bool condition, string message)
        {
            if (condition) { return; }
            throw new FailedException("Assertion '" + message + "' failed.");
        }
    }

    static partial class MathHelper
    {
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            T result = value;
            if (value.CompareTo(max) > 0)
                result = max;
            if (value.CompareTo(min) < 0)
                result = min;
            return result;
        }
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
