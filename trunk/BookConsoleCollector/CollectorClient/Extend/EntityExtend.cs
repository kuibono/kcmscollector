using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Voodoo;
namespace CollectorClient.Extend
{
    public static class EntityExtend
    {

        public static object GetClass(this Type type)
        {
            return Activator.CreateInstance(type, null);
        }
    }
}
