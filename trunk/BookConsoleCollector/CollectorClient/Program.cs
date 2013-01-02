using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Voodoo;
namespace CollectorClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //NewRule();

            Collect c = new Collect();
            c.FechRules();

        }

        static void NewRule()
        {
            BookRule r = new BookRule();

            Type type = typeof(BookRule);
            object obj = Activator.CreateInstance(type);
            PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo p in props)
            {
                Console.WriteLine(string.Format("{0}:",p.Name));

                var value=Console.ReadLine();
                if (p.PropertyType == typeof(string))
                {
                    p.SetValue(r, value, null);
                }
                else if(p.PropertyType == typeof(int))
                {
                    //Int 类型
                     p.SetValue(r, value.ToInt32(), null);
                }
                else
                {
                    //Boolean类型的数据
                    p.SetValue(r, value.ToBoolean(), null);
                }
                
            }
            r.Save();
        }
    }
}
