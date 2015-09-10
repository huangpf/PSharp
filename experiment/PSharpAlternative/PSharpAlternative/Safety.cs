using System.Diagnostics;

namespace PSharpAlternative
{
    internal class Safety
    {
        public static void Assert(bool condition, string message, params object[] args)
        {
            Debug.Assert(condition, string.Format(message, args));
        }

        public static void Assert(bool condition)
        {
            Debug.Assert(condition);
        }
    }
}