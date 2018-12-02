using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADO.NETHelper
{
    /// <summary>
    /// 分析结果
    /// </summary>
    public class AnalyseResult
    {

        private string name;
        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get => name; set => name = value; }

        private Dictionary<string, object> projectWithRate;
        /// <summary>
        /// 项目名称和完成率
        /// </summary>
        public Dictionary<string, object> ProjectWithRate
        {
            get => projectWithRate;
            set => projectWithRate = value;
        }

        
    }
}
