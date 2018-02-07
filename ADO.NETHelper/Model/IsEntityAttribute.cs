using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADO.NETHelper.Model
{
    /// <summary>
    /// 标识该属性是实体
    /// </summary>
    [Serializable,AttributeUsage(AttributeTargets.Property|AttributeTargets.Class)]
    public class IsEntityAttribute:Attribute
    {
    }
}
