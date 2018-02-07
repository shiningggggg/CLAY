using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADO.NETHelper.DBUtil
{
    /// <summary>
    /// ORM在保存数据的时候，会忽略打上NotSave批注的属性
    /// </summary>
    [Serializable,AttributeUsage(AttributeTargets.Property|AttributeTargets.Class)]
    public class NotSaveAttribute:Attribute
    {
    }
}
