using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADO.NETHelper.Model
{
    public class PageViewModel
    {
        /// <summary>
        /// 记录
        /// </summary>
        public IEnumerable rows { get; set; }
        /// <summary>
        /// 记录数
        /// </summary>
        public int total { get; set; }
    }
}
