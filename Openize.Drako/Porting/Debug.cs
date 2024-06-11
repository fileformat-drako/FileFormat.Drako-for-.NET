using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Openize.Draco
{
    internal class Debug
    {
        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {

        }
    }
}
