using ADO.NETHelper.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADO.NETHelper.Dal
{
    public class DalBase
    {
        /// <summary>
        /// 添加
        /// </summary>
        public static void Insert(object obj)
        {
            OracleHelper.Insert(obj);
        }
        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="obj"></param>
        public static void Update(object obj)
        {
            OracleHelper.Update(obj);
        }
        public static void Delete<T>(int id)
        {
            OracleHelper.Delete<T>(id);
        }
        public static void BatchDelete<T>(string ids)
        {
            OracleHelper.BatchDelete<T>(ids);
        }
        /// <summary>
        /// 查询列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static List<T> FindListBySql<T>(string sql) where T : new()
        {
            return OracleHelper.FindListBySql<T>(sql);
        }
        public static PageViewModel FindPageBySql<T>(string sql, string orderby, int pageSize, int currentPage) where T : new()
        {
            return OracleHelper.FindPageBySql<T>(sql, orderby, pageSize, currentPage);
        }
    }
}
