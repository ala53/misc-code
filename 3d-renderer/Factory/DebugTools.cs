using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Factory
{
    static class DebugTools
    {
        public static void Log(string text)
        {
#if !DEBUG
            Console.WriteLine(text);
#endif
        }
    }
}
