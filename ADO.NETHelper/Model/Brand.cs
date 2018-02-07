using ADO.NETHelper.DBUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADO.NETHelper.Model
{
    public class Brand
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int BrandCount { get; set; }
        public DateTime? BrandDate { get; set; }
        [NotSave]
        public string NotSaved { get; set; }
        [IsEntity]
        public BrandChild bc { get; set; }
        public byte[] Blb { get; set; }
        public string Clb { get; set; }
    }
}
