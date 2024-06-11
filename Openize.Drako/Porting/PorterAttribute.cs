using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Openize.Draco
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    internal class PorterAttribute : Attribute
    {
        public PorterAttribute(string cfg)
        {

        }
    }
}
