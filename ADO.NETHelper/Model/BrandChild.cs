using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADO.NETHelper.Model
{
    public class BrandChild
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Val { get; set; }
        [IsEntity]
        public BrandChildChild bcc { get; set; }
    }
}
