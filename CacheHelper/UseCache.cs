using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheHelper
{
    public class UseCache
    {
        public string GetValue(string name)
        {
            object obj = CacheHelper.Get("setting_" + name);
            if (obj == null)
            {
                string value = "get from Db";
                CacheHelper.Set("setting_" + name, value);
                return value;
            }
            return obj.ToString();

        }
    }
}
