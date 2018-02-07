using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CacheHelper
{
    public class CacheHelper
    {
        public static void Set(string key, object value)
        {
            System.Web.Caching.Cache cache = HttpRuntime.Cache;
            cache[key] = value;
        }
        public static object Get(string key)
        {
            System.Web.Caching.Cache cache = HttpRuntime.Cache;
            return cache[key];
        }
        public static void Delete(string key)
        {
            System.Web.Caching.Cache cache = HttpRuntime.Cache;
            cache.Remove(key);
        }
    }
}
