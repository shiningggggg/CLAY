using System;
using System.Reflection;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace SomeMethod
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }

        public void AboutAttribute<T>() where T : new()
        {
            //排除含有NotMapped的字段
            PropertyInfo[] propertis = new T().GetType().GetProperties().Where(v => v.GetCustomAttributes(typeof(NotMappedAttribute), true).Length == 0).ToArray();

            PropertyInfo[] propertys = new T().GetType().GetProperties().Where(p => !Attribute.IsDefined(p, typeof(NotMappedAttribute))).ToArray();
        }
    }
}
