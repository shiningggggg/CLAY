using ADO.NETHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            int returnValue = OracleHelper.ExecuteNonQuery("", System.Data.CommandType.Text);
        }
    }
}
