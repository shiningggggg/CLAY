using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADO.NETHelper.Dal
{
    public class BrandDal:DalBase
    {
        public static int GetNextID()
        {
            return OracleHelper.GetNextID("SE1_BRAND");
        }
    }
}
