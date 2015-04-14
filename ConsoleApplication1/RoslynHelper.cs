using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    public static class RoslynHelper
    {
        public static bool IsMvcController(INamedTypeSymbol x)
        {
            var classBaseType = x.BaseType;
            if (classBaseType.ToString() == "object")
            {
                return false;
            }

            if (classBaseType.ToString() == "System.Web.Mvc.Controller")
            {
                return true;
            }

            return IsMvcController(classBaseType);
        }
    }
}
