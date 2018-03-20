using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADO.NETHelper
{
    public class SqlHelper
    {
        //连接字符串
        static string connStr = ConfigurationManager.ConnectionStrings["sql"].ConnectionString;
        /// <summary>
        /// 实现非查询操作，增删改返回受影响的行数，否则返回-1
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string sql, params SqlParameter[] ps)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddRange(ps);
                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
        }
        public static int ExecuteNonQuery(string sql)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
        }
        /// <summary>
        /// 执行查询，并返回，查询所返回的结果集的，第一行第一列。忽略其他行或列。
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static object ExecuteScalar(string sql, params SqlParameter[] ps)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddRange(ps);
                    conn.Open();
                    return cmd.ExecuteScalar();
                }
            }
        }
        /// <summary>
        /// 读取数据库中的数据
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static SqlDataReader ExecuteReader(string sql, params SqlParameter[] ps)
        {
            SqlConnection conn = new SqlConnection(connStr);
            try
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddRange(ps);
                    conn.Open();
                    return cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
                }
            }
            catch (Exception ex)
            {
                conn.Dispose();
                throw ex;
            }
        }
        /// <summary>
        /// 取得一个数据表的集合
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static DataSet GetDataSet(string sql, params SqlParameter[] ps)
        {
            DataSet ds = new DataSet();
            using (SqlDataAdapter sda = new SqlDataAdapter(sql, connStr))
            {
                sda.SelectCommand.Parameters.Add(ps);
                sda.Fill(ds);
            }
            return ds; 
        }

        public static DataSet GetDataSetWithView(string sql)
        {
            DataSet ds = new DataSet();
            using (SqlDataAdapter sda = new SqlDataAdapter(sql, connStr))
            {
                sda.Fill(ds);
            }
            return ds;
        }
    }
}
