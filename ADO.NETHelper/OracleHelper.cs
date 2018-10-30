using ADO.NETHelper.DBUtil;
using ADO.NETHelper.Model;
using Oracle.DataAccess.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;

namespace ADO.NETHelper
{
    public class OracleHelper
    {
        #region 变量
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public static string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ToString();
        #endregion

        #region 事务的OracleConnection
        #region 数据库连接对象
        /// <summary>
        /// 获取打开的数据库连接对象
        /// </summary>
        /// <returns></returns>
        public static OracleConnection GetOpenConnection()
        {
            OracleConnection connection = null;
            string key = "Simpo_FQD_OracleConnection";
            if (HttpContext.Current.Items[key] == null)
            {
                connection = new OracleConnection(connectionString);
                connection.Open();
                HttpContext.Current.Items[key] = connection;
            }
            else
            {
                connection = (OracleConnection)HttpContext.Current.Items[key];
            }
            return connection;
        }
        #endregion
        #region 事务对象
        /// <summary>
        /// 获取事务对象
        /// </summary>
        public static OracleTransaction GetTran()
        {
            OracleTransaction tran = null;
            string key = "Simpo_FQD_OracleTransaction";
            if (HttpContext.Current.Items[key] == null)
            {
                tran = GetOpenConnection().BeginTransaction();
                HttpContext.Current.Items[key] = tran;
            }
            else
            {
                tran = (OracleTransaction)HttpContext.Current.Items[key];
            }
            return tran;
        }
        #endregion
        #region 开起事务标志
        /// <summary>
        /// 事务标志
        /// </summary>
        private static string tranFlagKey = "Simpo_FQD_OracleTransaction_Flag";
        /// <summary>
        /// 添加事务标志
        /// </summary>
        public static void AddTranFlag()
        {
            HttpContext.Current.Items[tranFlagKey] = true;
        }
        /// <summary>
        /// 移除事务标志
        /// </summary>
        public static void RemoveTranFlag()
        {
            HttpContext.Current.Items[tranFlagKey] = false;
        }
        /// <summary>
        /// 事务标志
        /// </summary>
        public static bool TranFlag
        {
            get
            {
                bool tranFlag = false;
                if (HttpContext.Current.Items[tranFlagKey] != null)
                {
                    tranFlag = (bool)HttpContext.Current.Items[tranFlagKey];
                }
                return tranFlag;
            }
        }
        #endregion
        #endregion
        #region 基础方法
        #region 公用方法
        #region GetMaxID
        private static int GetMaxID(string fieldName, string tableName)
        {
            string strsql = "select max(" + fieldName + ")+1 from " + tableName;
            object obj = OracleHelper.GetSingle(strsql);
            if (obj == null)
            {
                return 1;
            }
            else
            {
                return int.Parse(obj.ToString());
            }
        }
        #endregion
        #region Exists
        public static bool Exists(string strSql, params OracleParameter[] cmdParms)
        {
            object obj = OracleHelper.GetSingle(strSql, cmdParms);
            int cmdresult;
            if ((Object.Equals(obj, null)) || (Object.Equals(obj, System.DBNull.Value)))
            {
                cmdresult = 0;
            }
            else
            {
                cmdresult = int.Parse(obj.ToString());
            }
            if (cmdresult == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion
        #endregion
        #region 执行简单SQL语句
        #region Exists
        public static bool Exists(string SQLString)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(SQLString, connection))
                {
                    try
                    {
                        connection.Open();
                        object obj = cmd.ExecuteScalar();
                        if ((Object.Equals(obj, null)) || (Object.Equals(obj, System.DBNull.Value)))
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    catch (System.Data.OracleClient.OracleException ex)
                    {
                        connection.Close();
                        throw new Exception(ex.Message);
                    }
                    finally
                    {
                        cmd.Dispose();
                        connection.Close();
                    }
                }
            }
        }
        #endregion
        #region 执行SQL语句，返回影响的记录数
        /// <summary>
        /// 执行SQL语句，返回影响的记录数
        /// </summary>
        public static int ExecuteSql(string SQLString)
        {
            OracleConnection connection = GetOpenConnection();
            using (OracleCommand cmd = new OracleCommand(SQLString, connection))
            {
                try
                {
                    if (TranFlag) cmd.Transaction = GetTran();
                    int rows = cmd.ExecuteNonQuery();
                    return rows;
                }
                catch (System.Data.OracleClient.OracleException e)
                {
                    connection.Close();
                    throw new Exception(e.Message);
                }
                finally
                {
                    cmd.Dispose();
                    if (!TranFlag) connection.Close();
                }
            }
        }
        #endregion
        #region 执行多条SQL语句，实现数据库事务
        public static bool ExecuteSqlTran(ArrayList SQLStringList)
        {
            bool re = false;
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                connection.Open();
                OracleCommand cmd = new OracleCommand();
                cmd.Connection = connection;
                OracleTransaction tx = connection.BeginTransaction();
                cmd.Transaction = tx;
                try
                {
                    for (int n = 0; n < SQLStringList.Count; n++)
                    {
                        string strsql = SQLStringList[n].ToString();
                        if (strsql.Trim().Length > 1)
                        {
                            cmd.CommandText = strsql;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                    re = true;
                }
                catch (OracleException ex)
                {
                    re = false;
                    tx.Rollback();
                    throw new Exception(ex.Message);
                }
                finally
                {
                    cmd.Dispose();
                    connection.Close();
                }
            }
            return re;
        }
        #endregion
        #region 执行带一个存储过程参数的SQL语句
        /// <summary>
        /// 执行带一个存储过程参数的SQL语句
        /// </summary>
        /// <param name="sqlString"></param>
        /// <param name="content">参数内容，比如一个字段是格式复杂的文章，有特殊符号，可以通过这个方式添加</param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteSql(string sqlString, string content)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                OracleCommand cmd = new OracleCommand(sqlString, connection);
                OracleParameter myParameter = new OracleParameter("@content", OracleDbType.NVarchar2);
                myParameter.Value = content;
                cmd.Parameters.Add(myParameter);
                try
                {
                    connection.Open();
                    int rows = cmd.ExecuteNonQuery();
                    return rows;
                }
                catch (System.Data.OracleClient.OracleException e)
                {
                    throw new Exception(e.Message);
                }
                finally
                {
                    cmd.Dispose();
                    connection.Close();
                }
            }
        }
        #endregion
        #region 向数据库里插入图像格式的字段
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strSql"></param>
        /// <param name="fs">图像字节，数据库的字段类型为image的情况</param>
        /// <returns></returns>
        public static int ExecuteSqlInsertImg(string strSql, byte[] fs)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                OracleCommand cmd = new OracleCommand(strSql, connection);
                OracleParameter myParameter = new OracleParameter("@fs", OracleDbType.LongRaw);
                myParameter.Value = fs;
                cmd.Parameters.Add(myParameter);
                try
                {
                    connection.Open();
                    int rows = cmd.ExecuteNonQuery();
                    return rows;
                }
                catch (OracleException e)
                {
                    throw new Exception(e.Message);
                }
                finally
                {
                    cmd.Dispose();
                    connection.Close();
                }
            }
        }
        #endregion
        #region 执行一条计算查询结果语句，返回查询结果
        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果
        /// </summary>
        public static object GetSingle(string sqlString)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(sqlString, connection))
                {
                    try
                    {
                        connection.Open();
                        object obj = cmd.ExecuteScalar();
                        if ((Object.Equals(obj, null)) || (Object.Equals(obj, System.DBNull.Value)))
                        {
                            return null;
                        }
                        else
                        {
                            return obj;
                        }
                    }
                    catch (OracleException e)
                    {
                        connection.Close();
                        throw new Exception(e.Message);
                    }
                    finally
                    {
                        cmd.Dispose();
                        connection.Close();
                    }
                }
            }

        }
        #endregion
        #region 执行查询语句，返回OracleDataReader
        public static OracleDataReader ExecuteReader(string strSql)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(strSql, connection))
                {
                    try
                    {
                        connection.Open();
                        OracleDataReader myReader = cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
                        return myReader;
                    }
                    catch (OracleException e)
                    {
                        throw new Exception(e.Message);
                    }
                }
            }
        }
        #endregion
        #region 执行查询语句，返回DataSet
        public static DataSet Query(string SqlString)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                DataSet ds = new DataSet();
                try
                {
                    connection.Open();
                    OracleDataAdapter command = new OracleDataAdapter(SqlString, connection);
                    command.Fill(ds, "ds");
                }
                catch (OracleException e)
                {
                    throw new Exception(e.Message);
                }
                finally
                {
                    connection.Close();
                }
                return ds;
            }
        }
        #endregion
        #endregion
        #region 执行带参数的SQL语句
        #region 执行SQL语句，返回影响的记录数
        public static int ExecuteSql(string SqlString, params OracleParameter[] cmdParms)
        {
            OracleConnection connection = GetOpenConnection();
            using (OracleCommand cmd = new OracleCommand())
            {
                try
                {
                    PrepareCommand(cmd, connection, null, SqlString, cmdParms);
                    if (TranFlag) cmd.Transaction = GetTran();
                    int rows = cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                    return rows;
                }
                catch (OracleException e)
                {
                    throw new Exception(e.Message);
                }
                finally
                {
                    cmd.Dispose();
                    if (!TranFlag) connection.Close();
                }
            }
        }
        #endregion
        #region 执行多条SQL语句，实现数据库事务
        public static void ExecuteSqlTran(Hashtable SqlStringList)
        {
            using (OracleConnection conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (OracleTransaction trans = conn.BeginTransaction())
                {
                    OracleCommand cmd = new OracleCommand();
                    try
                    {
                        foreach (DictionaryEntry myDE in SqlStringList)
                        {
                            string cmdText = myDE.Key.ToString();
                            OracleParameter[] cmdParms = (OracleParameter[])myDE.Value;
                            PrepareCommand(cmd, conn, trans, cmdText, cmdParms);
                            int val = cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();
                            trans.Commit();
                        }
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }
        #endregion
        #region 执行一条计算查询结果语句，返回查询结果
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SqlString"></param>
        /// <param name="cmdParms"></param>
        /// <returns></returns>
        public static object GetSingle(string SqlString, params OracleParameter[] cmdParms)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(SqlString, connection))
                {
                    try
                    {
                        PrepareCommand(cmd, connection, null, SqlString, cmdParms);
                        object obj = cmd.ExecuteScalar();
                        cmd.Parameters.Clear();
                        if (Object.Equals(obj, null) || Object.Equals(obj, System.DBNull.Value))
                        {
                            return null;
                        }
                        else
                        {
                            return obj;
                        }
                    }
                    catch (OracleException e)
                    {
                        throw new Exception(e.Message);
                    }
                    finally
                    {
                        cmd.Dispose();
                        connection.Close();
                    }
                }
            }
        }
        #endregion
        #region 执行查询语句，返回OracleDataReader
        /// <summary>
        /// 执行查询语句，返回OracleDataReader(注意：调用该方法后，一定要对SqlDataReader进行Close)
        /// </summary>
        public static OracleDataReader ExecuteReader(string SqlString, params OracleParameter[] cmdParms)
        {
            OracleConnection connection = new OracleConnection(connectionString);
            OracleCommand cmd = new OracleCommand(SqlString, connection);
            try
            {
                PrepareCommand(cmd, connection, null, SqlString, cmdParms);
                OracleDataReader myReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return myReader;
            }
            catch (OracleException e)
            {
                throw new Exception(e.Message);
            }
        }
        #endregion
        #region 执行查询语句，返回DataSet
        public static DataSet Query(string SqlString, params OracleParameter[] cmdParms)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                OracleCommand cmd = new OracleCommand(SqlString, connection);
                PrepareCommand(cmd, connection, null, SqlString, cmdParms);
                using (OracleDataAdapter da = new OracleDataAdapter(cmd))
                {
                    DataSet ds = new DataSet();
                    try
                    {
                        da.Fill(ds, "ds");
                        cmd.Parameters.Clear();
                    }
                    catch (OracleException e)
                    {

                    }
                    finally
                    {
                        cmd.Dispose();
                        connection.Close();
                    }
                    return ds;
                }
            }
        }
        #endregion
        #region PrepareCommand
        private static void PrepareCommand(OracleCommand cmd, OracleConnection connection, OracleTransaction trans, string cmdText, OracleParameter[] cmdParms)
        {
            if (connection.State != ConnectionState.Open)
                connection.Open();
            cmd.Connection = connection;
            cmd.CommandText = cmdText;
            if (trans != null)
                cmd.Transaction = trans;
            cmd.CommandType = CommandType.Text;
            if (cmdParms != null)
            {
                foreach (OracleParameter param in cmdParms)
                {
                    cmd.Parameters.Add(param);
                }
            }
        }
        #endregion
        #endregion
        #region 存储过程操作
        #region 执行存储过程，返回SqlDataReader
        public static OracleDataReader RunProcedureReader(string storedProcName, IDataParameter[] parameters)
        {
            OracleConnection connection = new OracleConnection(connectionString);
            OracleDataReader returnReader;
            connection.Open();
            OracleCommand cmd = BuildQueryCommand(connection, storedProcName, parameters);
            cmd.CommandType = CommandType.StoredProcedure;
            returnReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            return returnReader;
        }
        #endregion
        #region 执行存储过程，返回受影响的行数
        public static int RunProcedure_rowsAffected(string storedProcName, IDataParameter[] parameters, out int rowsAffected)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                int result;
                connection.Open();
                OracleCommand cmd = BuildIntCommand(connection, storedProcName, parameters);
                rowsAffected = cmd.ExecuteNonQuery();
                result = (int)cmd.Parameters["ReturnValue"].Value;
                return result;
            }
        }
        #endregion
        #region 执行存储过程，什么都不返回
        public static void RunProcedure(string storedProcName, OracleParameter[] parameters)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                OracleCommand cmd = BuildQueryCommand(connection, storedProcName, parameters);
                try
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (OracleException ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    connection.Close();
                }
            }
        }
        #endregion
        #region 执行存储过程，返回数据集
        public static DataSet RunProcedureGetDataSet(string storedProcName, OracleParameter[] parms)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                DataSet ds = new DataSet();
                connection.Open();
                OracleDataAdapter da = new OracleDataAdapter();
                da.SelectCommand = BuildQueryCommand(connection, storedProcName, parms);
                da.Fill(ds, "ds");
                connection.Close();
                return ds;
            }
        }
        #endregion
        #region 构建 OracleCommand 对象
        /// <param name="connection">数据库链接</param>
        /// <param name="storedProcName">存储过程名称</param>
        /// <param name="parameters">存储过程参数</param>
        private static OracleCommand BuildQueryCommand(OracleConnection connection, string storedProcName, IDataParameter[] parameters)
        {
            OracleCommand command = new OracleCommand(storedProcName, connection);
            command.CommandType = CommandType.StoredProcedure;
            foreach(OracleParameter parms in parameters)
            {
                command.Parameters.Add(parms);
            }
            return command;
        }
        #endregion
        #region 创建OracleCommand对象实例，用来返回一个整数值
        public static OracleCommand BuildIntCommand(OracleConnection connection, string storedProceName, IDataParameter[] parameters)
        {
            OracleCommand command = BuildQueryCommand(connection, storedProceName, parameters);
            command.Parameters.Add(new OracleParameter("ReturnValue", OracleDbType.Int32, 4, ParameterDirection.ReturnValue,false,0,0,string.Empty,DataRowVersion.Default,null));
            return command;
        }
        #endregion
        #endregion
        #endregion
        #region 扩展方法
        #region 执行返回一行一列的数据操作
        public static int ExecuteScalar(string cmdText, CommandType commandType, params OracleParameter[] param)
        {
            int count = 0;
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(cmdText, connection))
                {
                    try
                    {
                        cmd.CommandType = commandType;
                        cmd.Parameters.AddRange(param);
                        connection.Open();
                        count = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    catch(OracleException e)
                    {
                        count = 0;
                    }
                }
            }
            return count;
        }
        #endregion
        #region 执行非查询操作
        public static int ExecuteNonQuery(string cmdText, CommandType commandType, params OracleParameter[] parms)
        {
            int result = 0;
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(cmdText, connection))
                {
                    try
                    {
                        cmd.CommandType = commandType;
                        cmd.Parameters.AddRange(parms);
                        connection.Open();
                        result = cmd.ExecuteNonQuery();
                    }
                    catch (OracleException e)
                    {
                        result = 0;
                    }
                    return result;
                }
            }
            return result;
        }
        #endregion
        #region 执行返回一条记录的泛型对象
        public static T ExecuteEntity<T>(string cmdText, CommandType commandType, params OracleParameter[] parms)
        {
            T obj = default(T);
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(cmdText, connection))
                {
                    cmd.CommandType = commandType;
                    cmd.Parameters.AddRange(parms);
                    connection.Open();
                    OracleDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    while (reader.Read())
                    {
                        obj = OracleHelper.ExecuteDataReader<T>(reader);
                    }
                }
            }
            return obj;
        }
        #endregion
        #region 执行返回多条记录的泛型对象
        public static List<T> ExecuteList<T>(string cmdText, CommandType commandType, params OracleParameter[] parms)
        {
            List<T> list = new List<T>();
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(cmdText, connection))
                {
                    try
                    {
                        cmd.CommandType = commandType;
                        cmd.Parameters.AddRange(parms);
                        connection.Open();
                        OracleDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                        while (reader.Read())
                        {
                            T obj = OracleHelper.ExecuteDataReader<T>(reader);
                            list.Add(obj);
                        }
                    }
                    catch (Exception e)
                    {
                        list = null;
                    }
                }
            }
            return list;
        }
        #endregion
        #region 处理OracleDataReader公用方法
        private static T ExecuteDataReader<T>(IDataReader reader)
        {
            T obj = default(T);
            try
            {
                Type type = typeof(T);
                obj = (T)Activator.CreateInstance(type);//从当前程序集里面通过反射的方式创建指定类型的对象
                PropertyInfo[] propertyInfos = type.GetProperties();//获取指定类型里面的所有属性
                foreach (PropertyInfo propertyInfo in propertyInfos)
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string fieldName = reader.GetName(i);
                        if (fieldName.ToLower() == propertyInfo.Name.ToLower())
                        {
                            object val = reader[propertyInfo.Name];
                            if (val != null && val != DBNull.Value)
                                propertyInfo.SetValue(obj, val, null);
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {

            }
            return obj;
        }
        #endregion
        #endregion
        #region 增删改查
        #region 根据sequence名称获取下一个ID
        public static int GetNextID(string sequenceName)
        {
            string sql = string.Format("select {0}.Nextval from dual", sequenceName);
            DataTable table = Query(sql).Tables[0];
            return int.Parse(table.Rows[0][0].ToString());
        }
        #endregion
        #region 添加
        public static void Insert(Object obj)
        {
            StringBuilder strBuilder = new StringBuilder();
            Type type = obj.GetType();
            strBuilder.Append(string.Format("insert into {0} (", type.Name));
            List<string> propertyNameList = new List<string>();
            PropertyInfo[] propertyInfoLists = type.GetProperties();
            int saveCount = 0;
            foreach (PropertyInfo propertyInfo in propertyInfoLists)
            {
                if (propertyInfo.GetCustomAttributes(typeof(NotSaveAttribute), false).Length == 0)
                {
                    if (propertyInfo.GetCustomAttributes(typeof(IsEntityAttribute), false).Length == 0)
                    {
                        object val = propertyInfo.GetValue(obj,null);
                        if (val != null)
                        {
                            propertyNameList.Add(propertyInfo.Name);
                            saveCount++;
                        }
                    }
                    else
                    {
                        object val = propertyInfo.GetValue(obj, null);
                        if (val != null)
                        {
                            propertyNameList.Add(propertyInfo.Name + "Id");
                            saveCount++;
                        }
                    }
                }
            }
            strBuilder.Append(string.Format("{0}) ", string.Join(",", propertyNameList.ToArray())));
            strBuilder.Append(string.Format("values {0}", string.Join(",", propertyNameList.ConvertAll<string>(a => ":" + a).ToArray())));
            OracleParameter[] parameters = new OracleParameter[saveCount];
            int i = 0;
            foreach (PropertyInfo propertyInfo in propertyInfoLists)
            {
                if (propertyInfo.GetCustomAttributes(typeof(NotSaveAttribute), false).Length == 0)
                {
                    if (propertyInfo.GetCustomAttributes(typeof(IsEntityAttribute), false).Length == 0)
                    {
                        object val = propertyInfo.GetValue(obj, null);
                        if (val != null)
                        {
                            OracleParameter oracleParameter = new OracleParameter(":" + propertyInfo.Name, propertyInfo.GetValue(obj, null));
                            parameters[i++] = oracleParameter;
                        }
                    }
                    else
                    {
                        object val = propertyInfo.GetValue(obj, null);
                        if (val != null)
                        {
                            object valProVal = val.GetType().GetProperty("Id").GetValue(val, null);
                            OracleParameter oracleParameter = new OracleParameter(":" + propertyInfo.Name + "Id", valProVal);
                            parameters[i++] = oracleParameter;
                        }
                    }
                }
            }
            ExecuteSql(strBuilder.ToString(), parameters);
        }
        #endregion
        #region 修改
        public static void Update(object obj)
        {
            object oldObj = Find(obj);
            if (oldObj == null) throw new Exception("无法获取旧数据");
            StringBuilder strSql = new StringBuilder();
            Type type = obj.GetType();
            strSql.Append(string.Format("update {0} set ", type.Name));
            PropertyInfo[] propertyInfoList = type.GetProperties();
            List<string> propertyNameList = new List<string>();
            int savedCount = 0;
            foreach (PropertyInfo propertyInfo in propertyInfoList)
            {
                if (propertyInfo.GetCustomAttributes(typeof(NotSaveAttribute), false).Length == 0)
                {
                    if (propertyInfo.GetCustomAttributes(typeof(IsEntityAttribute), false).Length == 0)
                    {
                        object oldValue = propertyInfo.GetValue(oldObj, null);
                        object val = propertyInfo.GetValue(obj, null);
                        if (!object.Equals(oldValue, val))
                        {
                            propertyNameList.Add(propertyInfo.Name);
                            savedCount++;
                        }
                    }
                    else
                    {
                        object oldValue = propertyInfo.GetValue(oldObj, null);
                        object val = propertyInfo.GetValue(obj, null);
                        object oldValProVal = oldValue == null ? null : GetIdVal(oldValue);
                        object valProVal = val == null ? null : GetIdVal(val);
                        if (!object.Equals(oldValProVal, valProVal))
                        {
                            propertyNameList.Add(propertyInfo.Name);
                            savedCount++;
                        }
                    }
                }
            }
            OracleParameter[] parameters = new OracleParameter[savedCount];
            int i = 0;
            StringBuilder sbPros = new StringBuilder();
            foreach (PropertyInfo propertyInfo in propertyInfoList)
            {
                if (propertyInfo.GetCustomAttributes(typeof(NotSaveAttribute), false).Length == 0)
                {
                    if (propertyInfo.GetCustomAttributes(typeof(IsEntityAttribute), false).Length == 0)
                    {
                        object oldValue = propertyInfo.GetValue(oldObj, null);
                        object value = propertyInfo.GetValue(obj, null);
                        if (!object.Equals(oldValue, value))
                        {
                            sbPros.Append(string.Format("{0}=:{0},", propertyInfo.Name));
                            OracleParameter parameter = new OracleParameter(":" + propertyInfo.Name, value == null ? DBNull.Value : value);
                            parameters[i++] = parameter;
                        }
                    }
                    else
                    {
                        object oldValue = propertyInfo.GetValue(oldObj, null);
                        object value = propertyInfo.GetValue(obj, null);
                        object oldValProVal = oldValue == null ? null : GetIdVal(oldValue);
                        object valProVal = value == null ? null : GetIdVal(value);
                        if (!object.Equals(oldValProVal, valProVal))
                        {
                            sbPros.Append(string.Format("{0}=:{0},", propertyInfo.Name + "Id"));
                            OracleParameter parameter = new OracleParameter(":" + propertyInfo.Name + "Id", valProVal == null ? DBNull.Value : valProVal);
                            parameters[i++] = parameter;
                        }
                    }
                }
                if (propertyInfo.GetCustomAttributes(typeof(IsEntityAttribute), false).Length > 0)
                {
                    object val = propertyInfo.GetValue(obj, null);
                    if (val != null && Find(val) != null)
                    {
                        Update(val);
                    }
                }
            }
            if (sbPros.Length > 0)
            {
                strSql.Append(sbPros.ToString(0, sbPros.Length - 1));
            }
            strSql.Append(string.Format(" where {0}={1}", GetIdName(obj.GetType()), int.Parse(GetIdVal(obj).ToString())));

            if (savedCount > 0)
            {
                ExecuteSql(strSql.ToString(), parameters);
            }
        }
        #endregion
        #region 删除
        public static bool Delete<T>(int id)
        {
            Type type = typeof(T);
            StringBuilder sqlString = new StringBuilder();
            sqlString.Append(string.Format("delete from {0} where {1} = {2}", type.Name, GetIdName(type), id));
            return ExecuteSql(sqlString.ToString()) > 0;
        }
        public static bool BatchDelete<T>(string ids)
        {
            if (string.IsNullOrEmpty(ids)) return false;
            Type type = typeof(T);
            StringBuilder sqlString = new StringBuilder();
            sqlString.Append(string.Format("delete from {0} where {1} in ({2})", type.Name, GetIdName(type), ids));
            return ExecuteSql(sqlString.ToString()) > 0;
        }
        public static bool Delete<T>(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return false;
            Type type = typeof(T);
            StringBuilder sqlString = new StringBuilder();
            sqlString.Append(string.Format("delete from {0} where {1}", type.Name, condition));
            return ExecuteSql(sqlString.ToString()) > 0;
        }
        #endregion
        #endregion
        #region 获取实体
        #region 根据实体获取实体
        /// <summary>
        /// 根据实体获取实体
        /// </summary>
        private static object Find(object obj)
        {
            Type type = obj.GetType();

            object result = Activator.CreateInstance(type);
            bool hasValue = false;
            IDataReader rd = null;

            string sql = string.Format("select * from {0} where {2}={1}", type.Name, GetIdVal(obj), GetIdName(obj.GetType()));

            try
            {
                rd = ExecuteReader(sql);

                PropertyInfo[] propertyInfoList = type.GetProperties();

                int fcnt = rd.FieldCount;
                List<string> fileds = new List<string>();
                for (int i = 0; i < fcnt; i++)
                {
                    fileds.Add(rd.GetName(i).ToUpper());
                }

                while (rd.Read())
                {
                    hasValue = true;
                    IDataRecord record = rd;

                    foreach (PropertyInfo pro in propertyInfoList)
                    {
                        if (pro.PropertyType.IsClass)
                        {
                            object[] objArray = pro.GetCustomAttributes(typeof(IsEntityAttribute), false);
                            if (objArray.Length > 0)
                            {
                                if (record[pro.Name + "Id"].GetType() == typeof(int))
                                {
                                    pro.SetValue(result, record[pro.Name + "Id"] == DBNull.Value ? null : FindById(pro.PropertyType, (int)record[pro.Name + "Id"]), null);
                                }
                                if (record[pro.Name + "Id"].GetType() == typeof(decimal))
                                {
                                    pro.SetValue(result, record[pro.Name + "Id"] == DBNull.Value ? null : FindById(pro.PropertyType, int.Parse(((decimal)record[pro.Name + "Id"]).ToString())), null);
                                }
                                continue;
                            }
                        }

                        if (!fileds.Contains(pro.Name.ToUpper()) || record[pro.Name] == DBNull.Value)
                        {
                            continue;
                        }

                        pro.SetValue(result, record[pro.Name] == DBNull.Value ? null : getReaderValue(record[pro.Name], pro.PropertyType), null);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (rd != null && !rd.IsClosed)
                {
                    rd.Close();
                    rd.Dispose();
                }
            }

            if (hasValue)
            {
                return result;
            }
            else
            {
                return null;
            }
        }
        #endregion
        #region 根据Id获取实体
        /// <summary>
        /// 根据Id获取实体
        /// </summary>
        private static object FindById(Type type, int id)
        {
            object result = Activator.CreateInstance(type);
            IDataReader rd = null;
            bool hasValue = false;

            string sql = string.Format("select * from {0} where {2}={1}", type.Name, id, GetIdName(type));

            try
            {
                rd = ExecuteReader(sql);

                PropertyInfo[] propertyInfoList = type.GetProperties();

                int fcnt = rd.FieldCount;
                List<string> fileds = new List<string>();
                for (int i = 0; i < fcnt; i++)
                {
                    fileds.Add(rd.GetName(i).ToUpper());
                }

                while (rd.Read())
                {
                    hasValue = true;
                    IDataRecord record = rd;

                    foreach (PropertyInfo pro in propertyInfoList)
                    {
                        if (pro.PropertyType.IsClass)
                        {
                            object[] objArray = pro.GetCustomAttributes(typeof(IsEntityAttribute), false);
                            if (objArray.Length > 0)
                            {
                                if (record[pro.Name + "Id"].GetType() == typeof(int))
                                {
                                    pro.SetValue(result, record[pro.Name + "Id"] == DBNull.Value ? null : FindById(pro.PropertyType, (int)record[pro.Name + "Id"]), null);
                                }
                                if (record[pro.Name + "Id"].GetType() == typeof(decimal))
                                {
                                    pro.SetValue(result, record[pro.Name + "Id"] == DBNull.Value ? null : FindById(pro.PropertyType, int.Parse(((decimal)record[pro.Name + "Id"]).ToString())), null);
                                }
                                continue;
                            }
                        }

                        if (!fileds.Contains(pro.Name.ToUpper()) || record[pro.Name] == DBNull.Value)
                        {
                            continue;
                        }

                        pro.SetValue(result, record[pro.Name] == DBNull.Value ? null : getReaderValue(record[pro.Name], pro.PropertyType), null);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (rd != null && !rd.IsClosed)
                {
                    rd.Close();
                    rd.Dispose();
                }
            }

            if (hasValue)
            {
                return result;
            }
            else
            {
                return null;
            }
        }
        #endregion
        #region 根据Id获取实体
        /// <summary>
        /// 根据Id获取实体
        /// </summary>
        public static T FindById<T>(int id) where T : new()
        {
            Type type = typeof(T);
            T result = (T)Activator.CreateInstance(type);
            IDataReader rd = null;
            bool hasValue = false;

            string sql = string.Format("select * from {0} where {2}={1}", type.Name, id, GetIdName(type));

            try
            {
                rd = ExecuteReader(sql);

                PropertyInfo[] propertyInfoList = type.GetProperties();

                int fcnt = rd.FieldCount;
                List<string> fileds = new List<string>();
                for (int i = 0; i < fcnt; i++)
                {
                    fileds.Add(rd.GetName(i).ToUpper());
                }

                while (rd.Read())
                {
                    hasValue = true;
                    IDataRecord record = rd;

                    foreach (PropertyInfo pro in propertyInfoList)
                    {
                        if (pro.PropertyType.IsClass)
                        {
                            object[] objArray = pro.GetCustomAttributes(typeof(IsEntityAttribute), false);
                            if (objArray.Length > 0)
                            {
                                if (record[pro.Name + "Id"].GetType() == typeof(int))
                                {
                                    pro.SetValue(result, record[pro.Name + "Id"] == DBNull.Value ? null : FindById(pro.PropertyType, (int)record[pro.Name + "Id"]), null);
                                }
                                if (record[pro.Name + "Id"].GetType() == typeof(decimal))
                                {
                                    pro.SetValue(result, record[pro.Name + "Id"] == DBNull.Value ? null : FindById(pro.PropertyType, int.Parse(((decimal)record[pro.Name + "Id"]).ToString())), null);
                                }
                                continue;
                            }
                        }

                        if (!fileds.Contains(pro.Name.ToUpper()) || record[pro.Name] == DBNull.Value)
                        {
                            continue;
                        }

                        pro.SetValue(result, record[pro.Name] == DBNull.Value ? null : getReaderValue(record[pro.Name], pro.PropertyType), null);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (rd != null && !rd.IsClosed)
                {
                    rd.Close();
                    rd.Dispose();
                }
            }

            if (hasValue)
            {
                return result;
            }
            else
            {
                return default(T);
            }
        }
        #endregion
        #endregion
        #region 获取列表
        /// <summary>
        /// 获取列表
        /// </summary>
        public static List<T> FindListBySql<T>(string sql) where T : new()
        {
            List<T> list = new List<T>();
            object obj;
            IDataReader rd = null;

            try
            {
                rd = ExecuteReader(sql);

                if (typeof(T) == typeof(int))
                {
                    while (rd.Read())
                    {
                        list.Add((T)rd[0]);
                    }
                }
                else if (typeof(T) == typeof(string))
                {
                    while (rd.Read())
                    {
                        list.Add((T)rd[0]);
                    }
                }
                else
                {
                    PropertyInfo[] propertyInfoList = (typeof(T)).GetProperties();

                    int fcnt = rd.FieldCount;
                    List<string> fileds = new List<string>();
                    for (int i = 0; i < fcnt; i++)
                    {
                        fileds.Add(rd.GetName(i).ToUpper());
                    }

                    while (rd.Read())
                    {
                        IDataRecord record = rd;
                        obj = new T();


                        foreach (PropertyInfo pro in propertyInfoList)
                        {
                            if (pro.PropertyType.IsClass)
                            {
                                if (pro.GetCustomAttributes(typeof(IsEntityAttribute), false).Length > 0)
                                {
                                    if (record[pro.Name + "Id"].GetType() == typeof(int))
                                    {
                                        pro.SetValue(obj, record[pro.Name + "Id"] == DBNull.Value ? null : FindById(pro.PropertyType, (int)record[pro.Name + "Id"]), null);
                                    }
                                    if (record[pro.Name + "Id"].GetType() == typeof(decimal))
                                    {
                                        pro.SetValue(obj, record[pro.Name + "Id"] == DBNull.Value ? null : FindById(pro.PropertyType, int.Parse(((decimal)record[pro.Name + "Id"]).ToString())), null);
                                    }
                                    continue;
                                }
                            }

                            if (!fileds.Contains(pro.Name.ToUpper()) || record[pro.Name] == DBNull.Value)
                            {
                                continue;
                            }

                            pro.SetValue(obj, record[pro.Name] == DBNull.Value ? null : getReaderValue(record[pro.Name], pro.PropertyType), null);
                        }
                        list.Add((T)obj);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (rd != null && !rd.IsClosed)
                {
                    rd.Close();
                    rd.Dispose();
                }
            }

            return list;
        }
        #endregion
        #region 获取列表
        /// <summary>
        /// 获取列表
        /// </summary>
        public static List<T> FindListBySql<T>(string sql, params OracleParameter[] cmdParms) where T : new()
        {
            List<T> list = new List<T>();
            object obj;
            IDataReader rd = null;

            try
            {
                rd = ExecuteReader(sql, cmdParms);

                if (typeof(T) == typeof(int))
                {
                    while (rd.Read())
                    {
                        list.Add((T)rd[0]);
                    }
                }
                else if (typeof(T) == typeof(string))
                {
                    while (rd.Read())
                    {
                        list.Add((T)rd[0]);
                    }
                }
                else
                {
                    PropertyInfo[] propertyInfoList = (typeof(T)).GetProperties();

                    int fcnt = rd.FieldCount;
                    List<string> fileds = new List<string>();
                    for (int i = 0; i < fcnt; i++)
                    {
                        fileds.Add(rd.GetName(i).ToUpper());
                    }

                    while (rd.Read())
                    {
                        IDataRecord record = rd;
                        obj = new T();


                        foreach (PropertyInfo pro in propertyInfoList)
                        {
                            if (pro.PropertyType.IsClass)
                            {
                                if (pro.GetCustomAttributes(typeof(IsEntityAttribute), false).Length > 0)
                                {
                                    if (record[pro.Name + "Id"].GetType() == typeof(int))
                                    {
                                        pro.SetValue(obj, record[pro.Name + "Id"] == DBNull.Value ? null : FindById(pro.PropertyType, (int)record[pro.Name + "Id"]), null);
                                    }
                                    if (record[pro.Name + "Id"].GetType() == typeof(decimal))
                                    {
                                        pro.SetValue(obj, record[pro.Name + "Id"] == DBNull.Value ? null : FindById(pro.PropertyType, int.Parse(((decimal)record[pro.Name + "Id"]).ToString())), null);
                                    }
                                    continue;
                                }
                            }

                            if (!fileds.Contains(pro.Name.ToUpper()) || record[pro.Name] == DBNull.Value)
                            {
                                continue;
                            }

                            pro.SetValue(obj, record[pro.Name] == DBNull.Value ? null : getReaderValue(record[pro.Name], pro.PropertyType), null);
                        }
                        list.Add((T)obj);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (rd != null && !rd.IsClosed)
                {
                    rd.Close();
                    rd.Dispose();
                }
            }

            return list;
        }
        #endregion
        #region 分页获取列表
        /// <summary>
        /// 分页(任意entity，尽量少的字段)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static PageViewModel FindPageBySql<T>(string sql, string orderby, int pageSize, int currentPage) where T : new()
        {
            PageViewModel pageViewModel = new PageViewModel();

            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                connection.Open();
                string commandText = string.Format("select count(*) from ({0}) T", sql);
                IDbCommand cmd = new OracleCommand(commandText, connection);
                pageViewModel.total = int.Parse(cmd.ExecuteScalar().ToString());

                int startRow = pageSize * (currentPage - 1);
                int endRow = startRow + pageSize;

                StringBuilder sb = new StringBuilder();
                sb.Append("select * from ( select row_limit.*, rownum rownum_ from (");
                sb.Append(sql);
                if (!string.IsNullOrWhiteSpace(orderby))
                {
                    sb.Append(" ");
                    sb.Append(orderby);
                }
                sb.Append(" ) row_limit where rownum <= ");
                sb.Append(endRow);
                sb.Append(" ) where rownum_ >");
                sb.Append(startRow);

                List<T> list = FindListBySql<T>(sb.ToString());
                pageViewModel.rows = list;
            }

            return pageViewModel;
        }
        #endregion
        #region 分页获取列表
        /// <summary>
        /// 分页(任意entity，尽量少的字段)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static PageViewModel FindPageBySql<T>(string sql, string orderby, int pageSize, int currentPage, params OracleParameter[] cmdParms) where T : new()
        {
            PageViewModel pageViewModel = new PageViewModel();

            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                connection.Open();
                string commandText = string.Format("select count(*) from ({0}) T", sql);
                OracleCommand cmd = new OracleCommand(commandText, connection);
                PrepareCommand(cmd, connection, null, commandText, cmdParms);
                pageViewModel.total = int.Parse(cmd.ExecuteScalar().ToString());
                cmd.Parameters.Clear();

                int startRow = pageSize * (currentPage - 1);
                int endRow = startRow + pageSize;

                StringBuilder sb = new StringBuilder();
                sb.Append("select * from ( select row_limit.*, rownum rownum_ from (");
                sb.Append(sql);
                if (!string.IsNullOrWhiteSpace(orderby))
                {
                    sb.Append(" ");
                    sb.Append(orderby);
                }
                sb.Append(" ) row_limit where rownum <= ");
                sb.Append(endRow);
                sb.Append(" ) where rownum_ >");
                sb.Append(startRow);

                List<T> list = FindListBySql<T>(sb.ToString(), cmdParms);
                pageViewModel.rows = list;
            }

            return pageViewModel;
        }


        #endregion
        #region 分页获取列表
        /// <summary>
        /// 分页(任意entity，尽量少的字段)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static DataSet FindPageBySql(string sql, string orderby, int pageSize, int currentPage, out int resultCount, params OracleParameter[] cmdParms)
        {
            DataSet ds = null;

            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                connection.Open();
                string commandText = string.Format("select count(*) from ({0}) T", sql);
                IDbCommand cmd = new OracleCommand(commandText, connection);
                resultCount = int.Parse(cmd.ExecuteScalar().ToString());

                int startRow = pageSize * (currentPage - 1);
                int endRow = startRow + pageSize;

                StringBuilder sb = new StringBuilder();
                sb.Append("select * from ( select row_limit.*, rownum rownum_ from (");
                sb.Append(sql);
                if (!string.IsNullOrWhiteSpace(orderby))
                {
                    sb.Append(" ");
                    sb.Append(orderby);
                }
                sb.Append(" ) row_limit where rownum <= ");
                sb.Append(endRow);
                sb.Append(" ) where rownum_ >");
                sb.Append(startRow);

                ds = Query(sql, cmdParms);
            }

            return ds;
        }
        #endregion
        #region getReaderValue 转换数据
        /// <summary>
        /// 转换数据
        /// </summary>
        private static Object getReaderValue(Object rdValue, Type ptype)
        {

            if (ptype == typeof(decimal))
                return Convert.ToDecimal(rdValue);

            if (ptype == typeof(int))
                return Convert.ToInt32(rdValue);

            if (ptype == typeof(long))
                return Convert.ToInt64(rdValue);

            return rdValue;
        }
        #endregion
        #region 获取主键名称
        /// <summary>
        /// 获取主键名称
        /// </summary>
        public static string GetIdName(Type type)
        {
            PropertyInfo[] propertyInfoList = type.GetProperties();
            foreach (PropertyInfo propertyInfo in propertyInfoList)
            {
                if (propertyInfo.GetCustomAttributes(typeof(IsIdAttribute), false).Length > 0)
                {
                    return propertyInfo.Name;
                }
            }
            return "Id";
        }
        #endregion
        #region 获取主键值
        /// <summary>
        /// 获取主键名称
        /// </summary>
        public static object GetIdVal(object val)
        {
            string idName = GetIdName(val.GetType());
            if (!string.IsNullOrWhiteSpace(idName))
            {
                return val.GetType().GetProperty(idName).GetValue(val, null);
            }
            return 0;
        }
        #endregion
        #region 事务
        #region 开始事务
        public static void BeginTransaction()
        {
            GetTran();
            AddTranFlag();
        }
        #endregion
        #region 结束事务(正常结束)
        public static void EndTransaction()
        {
            try
            {
                GetTran().Commit();
                RemoveTranFlag();
            }
            catch (Exception ex)
            {
                GetTran().Rollback();
                RemoveTranFlag();
            }
            finally
            {
                GetOpenConnection().Close();
            }
        }
        #endregion
        #region 回滚事务(出错时调用该方法回滚)
        public static void RollbackTransaction()
        {
            GetTran().Rollback();
            RemoveTranFlag();
            GetOpenConnection().Close();
        }
        #endregion
        #endregion
        #region 批量插入数据
        
        #region 使用OracleBulkCopy批量插入
        public static bool ExecuteBulkCopy(DataTable table, string targetTableName)
        {
            bool result = false;
            using (OracleConnection conn = GetOpenConnection())
            {
                using (OracleBulkCopy bulkCopy = new OracleBulkCopy(GetOpenConnection(), OracleBulkCopyOptions.Default))
                {
                    bulkCopy.BatchSize = 1000; //一次性插入的数据量
                    bulkCopy.BulkCopyTimeout = 60;//操作所允许的秒数，超时事务不会提交，数据会回滚
                    //bulkCopy.NotifyAfter = 10000;
                    if (table != null && table.Rows.Count > 0)
                    {
                        bulkCopy.DestinationTableName = targetTableName;
                        for (int i = 0; i < table.Columns.Count; i++)
                        {
                            string col = table.Columns[i].ColumnName;
                            bulkCopy.ColumnMappings.Add(col, col);
                        }
                        conn.Open();
                        bulkCopy.WriteToServer(table);
                        result = true;
                    }
                }
            }
            return result;
        }
        #endregion

        #region 使用参数数据的方式批量插入数据
        public static void ExecuteBulkInsert(string[] cards, string[] BatchNos)
        {
            using (OracleConnection conn = GetOpenConnection())
            {
                using (OracleCommand cmd = new OracleCommand("insert into cards(cards,batchno) values (:card,:batchno)", conn))
                {
                    cmd.ArrayBindCount = cards.Length;//关键点
                    cmd.Parameters.Add(new OracleParameter("cards", OracleDbType.Varchar2, cards, System.Data.ParameterDirection.Input));
                    cmd.Parameters.Add(new OracleParameter("batchno", OracleDbType.Varchar2, BatchNos, System.Data.ParameterDirection.Input));
                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                }
            }
        }
        #endregion

        #region 另一种参数的方式批量插入数据
        public static int BatchInsert(string tableName, Dictionary<string, object> columnRowData, string conStr, int len)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentException("必须制定批量插入的表名称", "tableName");
            }
            if (columnRowData == null || columnRowData.Count < 1)
            {
                throw new ArgumentException("必须指定批量插入的字段名称", "columnRowData");
            }
            int iResult = 0;
            string[] dbColumns = columnRowData.Keys.ToArray();
            StringBuilder sbCmdText = new StringBuilder();
            if (columnRowData.Count > 0)
            {
                //准备插入的SQL
                sbCmdText.AppendFormat("insert into {0}(", tableName);
                sbCmdText.Append(string.Join(",", dbColumns));
                sbCmdText.Append(") VALUES (");
                sbCmdText.Append(":" + string.Join(",:", dbColumns));
                sbCmdText.Append(")");

                using (OracleConnection conn = GetOpenConnection())
                {
                    using (OracleCommand cmd = conn.CreateCommand())
                    {
                        //绑定批处理的行数
                        cmd.ArrayBindCount = len;
                        cmd.BindByName = true;
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = sbCmdText.ToString();
                        cmd.CommandTimeout = 600;

                        //创建参数
                        OracleParameter oraParam;
                        List<IDbDataParameter> cacher = new List<IDbDataParameter>();
                        OracleDbType dbType = OracleDbType.Object;
                        foreach (string colName in dbColumns)
                        {
                            dbType = GetOracleDbType(columnRowData[colName]);
                            oraParam = new OracleParameter(colName, dbType);
                            oraParam.Direction = ParameterDirection.Input;
                            oraParam.OracleDbTypeEx = dbType;

                            oraParam.Value = columnRowData[colName];
                            cmd.Parameters.Add(oraParam);
                        }
                        conn.Open();

                        //执行批处理
                        var trans = conn.BeginTransaction();
                        try
                        {
                            cmd.Transaction = trans;
                            iResult = cmd.ExecuteNonQuery();
                            trans.Commit();
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            throw ex;
                        }
                        finally
                        {
                            cmd.Dispose();
                            conn.Close();
                        }
                    }
                }
            }
            return iResult;
        }

        #region 工具方法取得数据类型
        private static OracleDbType GetOracleDbType(object value)
        {
            OracleDbType dataType = OracleDbType.Object;
            if (value is string[])
            {
                dataType = OracleDbType.Varchar2;
            }
            else if (value is DateTime[])
            {
                dataType = OracleDbType.TimeStamp;
            }
            else if (value is int[] || value is short[])
            {
                dataType = OracleDbType.Int32;
            }
            else if (value is long[])
            {
                dataType = OracleDbType.Int64;
            }
            else if (value is decimal[] || value is double[] || value is float[])
            {
                dataType = OracleDbType.Decimal;
            }
            else if (value is Guid[])
            {
                dataType = OracleDbType.Varchar2;
            }
            else if (value is bool[] || value is Boolean[])
            {
                dataType = OracleDbType.Byte;
            }
            else if (value is byte[])
            {
                dataType = OracleDbType.Blob;
            }
            else if (value is char[])
            {
                dataType = OracleDbType.Char;
            }
            return dataType;
        }
        #endregion
        #endregion
        #endregion
    }
}
