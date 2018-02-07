using ADO.NETHelper.DBUtil;
using ADO.NETHelper.Model;
using Oracle.DataAccess.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Reflection;
using System.Text;
using System.Web;

namespace ADO.NETHelper
{
    public class OracleHelper
    {
        public static string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        #region 事物的OracleConnection
        /// <summary>
        /// 获取打开的数据库连接对象
        /// </summary>
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
        #region 开启事务标志
        /// <summary>
        /// 事物标志
        /// </summary>
        private static string tranFlayKey = "Simpo_FQD_OracleTransaction_Flag";
        /// <summary>
        /// 添加事务标志
        /// </summary>
        public static void AddTranFlag()
        {
            HttpContext.Current.Items[tranFlayKey] = true;
        }
        /// <summary>
        /// 移除事务标志
        /// </summary>
        public static void RemoveTranFlag()
        {
            HttpContext.Current.Items[tranFlayKey] = false;
        }
        public static bool TranFlag
        {
            get
            {
                bool tranFlag = false;
                if (HttpContext.Current.Items[tranFlayKey] != null)
                {
                    tranFlag = (bool)HttpContext.Current.Items[tranFlayKey];
                }
                return tranFlag;
            }
        }
        #endregion

        #endregion

        #region 基础方法
        #region 公用方法
        #region GetMaxID
        /// <summary>
        /// 不支持多用户并发，慎用，请使用GetNextID方法
        /// </summary>
        public static int GetMaxID(string fieldName, string tableName)
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
                    catch (System.Data.OracleClient.OracleException e)
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
        #region 执行SQL语句，返回影响的记录数
        /// <summary>
        /// 执行SQL语句，返回影响的记录数
        /// </summary>
        /// <param name="SQLString">SQL语句</param>
        /// <returns>影响的记录数</returns>
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
        public static bool ExecuteSqlTran(ArrayList SqlStringList)
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
                    for (int n = 0; n < SqlStringList.Count; n++)
                    {
                        string strsql = SqlStringList[n].ToString();
                        if (strsql.Trim().Length > 1)
                        {
                            cmd.CommandText = strsql;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                    re = true;
                }
                catch (System.Data.OracleClient.OracleException e)
                {
                    re = false;
                    tx.Rollback();
                    throw new Exception(e.Message);
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
        /// 执行第一个存储过程的SQL语句
        /// </summary>
        /// <param name="SqlString">SQL语句</param>
        /// <param name="content">参数内容，比如一个字段是格式复杂的文章，有特殊符号，可以通过这个方式添加</param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteSql(string SqlString, string content)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                OracleCommand cmd = new OracleCommand(SqlString, connection);
                System.Data.OracleClient.OracleParameter myParameter = new System.Data.OracleClient.OracleParameter("@content", OracleDbType.NVarchar2);
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
        #region 向数据库中插入图像你格式的字段
        public static int ExecuteSqlInsertImg(string strSql, byte[] fs)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                OracleCommand cmd = new OracleCommand(strSql, connection);
                System.Data.OracleClient.OracleParameter myParameter = new System.Data.OracleClient.OracleParameter("@fs", OracleDbType.LongRaw);
                myParameter.Value = fs;
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
        #region 执行一条计算查询结果语句，放回查询结果(object)。
        /// <summary>
        /// 执行一条计算查询结果语句，放回查询结果(object)。
        /// </summary>
        /// <param name="SqlString">计算查询结果语句</param>
        /// <returns>查询结果</returns>
        public static object GetSingle(string SqlString)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(SqlString, connection))
                {
                    try
                    {
                        connection.Open();
                        Object obj = cmd.ExecuteScalar();
                        if ((Object.Equals(obj, null)) || (Object.Equals(obj, System.DBNull.Value)))
                        {
                            return null;
                        }
                        else
                        {
                            return obj;
                        }
                    }
                    catch (System.Data.OracleClient.OracleException e)
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
        public static OracleDataReader ExecuteReader(string strSQL)
        {
            OracleConnection connection = new OracleConnection(connectionString);
            OracleCommand cmd = new OracleCommand(strSQL, connection);
            try
            {
                connection.Open();
                OracleDataReader myReader = cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
                return myReader;
            }
            catch (System.Data.OracleClient.OracleException e)
            {
                throw new Exception(e.Message);
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
                catch (System.Data.OracleClient.OracleException ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    connection.Close();
                }
                return ds;
            }
        }
        #endregion
        #region 执行带参数的SQL语句
        #region 执行SQL语句，返回影响的记录数
        public static int ExecuteSql(string SqlString, params OracleParameter[] cmdParms)
        {
            OracleConnection connection = new OracleConnection();
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
                catch (System.Data.OracleClient.OracleException e)
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
                        //循环
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
        public static object GetSingle(string SqlString, params OracleParameter[] cmdParms)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    try
                    {
                        PrepareCommand(cmd, connection, null, SqlString, cmdParms);
                        object obj = cmd.ExecuteScalar();
                        cmd.Parameters.Clear();
                        if ((Object.Equals(obj, null)) || (Object.Equals(obj, System.DBNull.Value)))
                        {
                            return null;
                        }
                        else
                        {
                            return obj;
                        }
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
        }
        #endregion
        #region 执行查询语句，返回OracleDataReader
        public static OracleDataReader ExecuteReader(string SqlString, params OracleParameter[] cmdParms)
        {
            OracleConnection connection = new OracleConnection(connectionString);
            OracleCommand cmd = new OracleCommand();
            try
            {
                PrepareCommand(cmd, connection, null, SqlString, cmdParms);
                OracleDataReader myReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return myReader;
            }
            catch (System.Data.OracleClient.OracleException e)
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
                OracleCommand cmd = new OracleCommand();
                PrepareCommand(cmd, connection, null, SqlString, cmdParms);
                using (OracleDataAdapter da = new OracleDataAdapter(cmd))
                {
                    DataSet ds = new DataSet();
                    try
                    {
                        da.Fill(ds, "ds");
                        cmd.Parameters.Clear();
                    }
                    catch (System.Data.OracleClient.OracleException ex)
                    {
                        throw new Exception(ex.Message);
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
        private static void PrepareCommand(OracleCommand cmd, OracleConnection conn, OracleTransaction trans, string cmdText, OracleParameter[] cmdParms)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            if (trans != null)
                cmd.Transaction = trans;
            cmd.CommandType = CommandType.Text;
            if (cmdParms != null)
            {
                foreach (OracleParameter parm in cmdParms)
                    cmd.Parameters.Add(parm);
            }
        }
        #endregion
        #region 存储过程操作
        #region 执行存储过程 返回SqlDataReader
        public static OracleDataReader RunProcedureReader(string storedProcName, IDataParameter[] parameters)
        {
            OracleConnection connection = new OracleConnection(connectionString);
            OracleDataReader returnReader;
            connection.Open();
            OracleCommand command = BuildQueryCommand(connection, storedProcName, parameters);
            command.CommandType = CommandType.StoredProcedure;
            returnReader = command.ExecuteReader(CommandBehavior.CloseConnection);
            return returnReader;
        }
        #endregion
        #region 执行存储过程，返回影响的行数
        public static int RunProcedure(string storedProcName, IDataParameter[] parameters, out int rowsAffected)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                int result;
                connection.Open();
                OracleCommand command = BuildIntCommand(connection, storedProcName, parameters);
                rowsAffected = command.ExecuteNonQuery();
                result = (int)command.Parameters["ReturnValue"].Value;
                return result;
            }
        }
        #endregion
        #region 执行存储过程，什么值也不返回
        public static void RunProcedure(string storedProcName, OracleParameter[] parameters)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                OracleCommand cmd = BuildQueryCommand(connection, storedProcName, parameters);
                try
                {
                    if (connection.State != ConnectionState.Open)
                    {
                        connection.Open();
                    }
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    ex.Message.ToString();
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
        public static DataSet RunProcedureGetDataSet(string storedProcName, OracleParameter[] parameters)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                DataSet dataSet = new DataSet();
                connection.Open();
                OracleDataAdapter sqlDA = new OracleDataAdapter();
                sqlDA.SelectCommand = BuildQueryCommand(connection, storedProcName, parameters);
                sqlDA.Fill(dataSet,"dt");
                connection.Close();
                return dataSet;
            }
        }

        #endregion
        #region 构建OracleCommand对象
        private static OracleCommand BuildQueryCommand(OracleConnection connection, string storedProcName, IDataParameter[] parameters)
        {
            OracleCommand command = new OracleCommand(storedProcName, connection);
            command.CommandType = CommandType.StoredProcedure;
            foreach (OracleParameter parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }
            return command;
        }
        #endregion
        #region 执行存储过程，返回影响的行数
        public static int RunProcedure_rowsAffected(string storedProcName, IDataParameter[] parameters, out int rowsAffected)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                int result;
                connection.Open();
                OracleCommand command = BuildIntCommand(connection, storedProcName, parameters);
                rowsAffected = command.ExecuteNonQuery();
                result = (int)command.Parameters["ReturnValue"].Value;
                return result;
            }
        }
        #endregion
        #region 创建OracleCommand对象实例(用来返回一个整数值)
        private static OracleCommand BuildIntCommand(OracleConnection connection, string storedProcName, IDataParameter[] parameters)
        {
            OracleCommand command = BuildQueryCommand(connection, storedProcName, parameters);
            command.Parameters.Add(new OracleParameter("ReturnValue", OracleDbType.Int32, 3,ParameterDirection.ReturnValue, false, 0, 0, string.Empty, DataRowVersion.Default, null));
            return command;
        }
        #endregion
        #endregion
        #region 扩展方法
        #region 执行返回一行一列的数据库操作
        public static int ExecuteScalar(string commandText, CommandType commandType, params OracleParameter[] param)
        {
            int count = 0;
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(commandText, connection))
                {
                    try
                    {
                        cmd.CommandType = commandType;
                        cmd.Parameters.AddRange(param);
                        connection.Open();
                        count = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    catch (Exception ex)
                    {
                        count = 0;
                    }
                }
            }
            return count;
        }
        #endregion
        #region 执行非查询的数据库操作
        public static int ExecuteNonQuery(string commandText, CommandType commandType, params OracleParameter[] param)
        {
            int result = 0;
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(commandText, connection))
                {
                    try
                    {
                        cmd.CommandType = commandType;
                        cmd.Parameters.AddRange(param);
                        connection.Open();
                        result = cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        result = 0;
                    }
                }
            }
            return result;
        }
        #endregion
        #region 执行返回一条记录的泛型对象
        private static T ExecuteDataReader<T>(IDataReader reader)
        {
            T obj = default(T);
            try
            {
                Type type = typeof(T);
                obj = (T)Activator.CreateInstance(type);//从当前程序集里面通过反射的方式创建指定类型的对象
                //obj = (T)Assembly.Load(OracleHelper._assemblyName).CreateInstance(OracleHelper._assemblyName + "." + type.Name);//从另一个程序集里面通过反射的方式创建指定类型的对象
                PropertyInfo[] propertyInfos = type.GetProperties();//获取制定类型里面的所有属性
                foreach (PropertyInfo propertyInfo in propertyInfos)
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string fieldName = reader.GetName(i);
                        if (fieldName.ToLower() == propertyInfo.Name.ToLower())
                        {
                            object val = reader[propertyInfo.Name];//读取表中某一条记录里面的某一列信息
                            if (val != null && val != DBNull.Value)
                                propertyInfo.SetValue(obj, val, null);//给对象的某一个属性赋值
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return obj;
        }
        #endregion
        #region 执行返回一条记录的泛型对象
        public static T ExecuteEntity<T>(string commandText, CommandType commandType, params OracleParameter[] param)
        {
            T obj = default(T);
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(commandText, connection))
                {
                    cmd.CommandType = commandType;
                    cmd.Parameters.AddRange(param);
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
        #region 执行返回多条记录的泛型集合对象
        public static List<T> ExecuteList<T>(string commandText, CommandType commandType, params OracleParameter[] param)
        {
            List<T> list = new List<T>();
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(commandText, connection))
                {
                    try
                    {
                        cmd.CommandType = commandType;
                        cmd.Parameters.AddRange(param);
                        connection.Open();
                        OracleDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                        while (reader.Read())
                        {
                            T obj = OracleHelper.ExecuteDataReader<T>(reader);
                            list.Add(obj);
                        }
                    }
                    catch (Exception ex)
                    {
                        list = null;
                    }
                }
                return list;
            }
        }
        #endregion
        #endregion
        #region 增删改查
        #region 根据sequence名称获取下一个ID
        /// <summary>
        /// 根据sequence名称获取下一个ID
        /// </summary>
        public static int GetNextID(string sequenceName)
        {
            string sql = string.Format("select {0}.Nextval from dual", sequenceName);
            DataTable dt = Query(sql).Tables[0];
            return int.Parse(dt.Rows[0][0].ToString());
        }
        #endregion

        #region 添加
        /// <summary>
        /// 添加
        /// </summary>
        public static void Insert(object obj)
        {
            StringBuilder strSql = new StringBuilder();
            Type type = obj.GetType();
            strSql.Append(string.Format("insert into {0}(", type.Name));

            PropertyInfo[] propertyInfoList = type.GetProperties();
            List<string> propertyNameList = new List<string>();
            int savedCount = 0;
            foreach (PropertyInfo propertyInfo in propertyInfoList)
            {
                if (propertyInfo.GetCustomAttributes(typeof(NotSaveAttribute), false).Length == 0)
                {
                    if (propertyInfo.GetCustomAttributes(typeof(IsEntityAttribute), false).Length == 0)
                    {
                        object val = propertyInfo.GetValue(obj, null);
                        if (val != null)
                        {
                            propertyNameList.Add(propertyInfo.Name);
                            savedCount++;
                        }
                    }
                    else
                    {
                        object val = propertyInfo.GetValue(obj, null);
                        if (val != null)
                        {
                            propertyNameList.Add(propertyInfo.Name + "Id");
                            savedCount++;
                        }
                    }
                }
            }

            strSql.Append(string.Format("{0})", string.Join(",", propertyNameList.ToArray())));
            strSql.Append(string.Format(" values ({0})", string.Join(",", propertyNameList.ConvertAll<string>(a => ":" + a).ToArray())));
            OracleParameter[] parameters = new OracleParameter[savedCount];
            int i = 0;
            foreach (PropertyInfo propertyInfo in propertyInfoList)
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

            ExecuteSql(strSql.ToString(), parameters);
        }
        #endregion

        #region 修改
        /// <summary>
        /// 修改
        /// </summary>
        public static void Update(object obj)
        {
            object oldObj = Find(obj);
            if (oldObj == null) throw new Exception("无法获取到旧数据");

            StringBuilder strSql = new StringBuilder();
            Type type = obj.GetType();
            strSql.Append(string.Format("update {0} ", type.Name));

            PropertyInfo[] propertyInfoList = type.GetProperties();
            List<string> propertyNameList = new List<string>();
            int savedCount = 0;
            foreach (PropertyInfo propertyInfo in propertyInfoList)
            {
                if (propertyInfo.GetCustomAttributes(typeof(NotSaveAttribute), false).Length == 0)
                {
                    if (propertyInfo.GetCustomAttributes(typeof(IsEntityAttribute), false).Length == 0)
                    {
                        object oldVal = propertyInfo.GetValue(oldObj, null);
                        object val = propertyInfo.GetValue(obj, null);
                        if (!object.Equals(oldVal, val))
                        {
                            propertyNameList.Add(propertyInfo.Name);
                            savedCount++;
                        }
                    }
                    else
                    {
                        object val = propertyInfo.GetValue(obj, null);
                        object oldVal = propertyInfo.GetValue(oldObj, null);
                        object oldValProVal = oldVal == null ? null : oldVal.GetType().GetProperty("Id").GetValue(oldVal, null);
                        object valProVal = val == null ? null : val.GetType().GetProperty("Id").GetValue(val, null);
                        if (!object.Equals(oldValProVal, valProVal))
                        {
                            propertyNameList.Add(propertyInfo.Name);
                            savedCount++;
                        }
                    }
                }
            }

            strSql.Append(string.Format(" set "));
            OracleParameter[] parameters = new OracleParameter[savedCount];
            int i = 0;
            StringBuilder sbPros = new StringBuilder();
            foreach (PropertyInfo propertyInfo in propertyInfoList)
            {
                if (propertyInfo.GetCustomAttributes(typeof(NotSaveAttribute), false).Length == 0)
                {
                    if (propertyInfo.GetCustomAttributes(typeof(IsEntityAttribute), false).Length == 0)
                    {
                        object oldVal = propertyInfo.GetValue(oldObj, null);
                        object val = propertyInfo.GetValue(obj, null);
                        if (!object.Equals(oldVal, val))
                        {
                            sbPros.Append(string.Format(" {0}=:{0},", propertyInfo.Name));
                            OracleParameter oracleParameter = new OracleParameter(":" + propertyInfo.Name, val == null ? DBNull.Value : val);
                            parameters[i++] = oracleParameter;
                        }
                    }
                    else
                    {
                        object oldVal = propertyInfo.GetValue(oldObj, null);
                        object val = propertyInfo.GetValue(obj, null);
                        object oldValProVal = oldVal == null ? null : oldVal.GetType().GetProperty("Id").GetValue(oldVal, null);
                        object valProVal = val == null ? null : val.GetType().GetProperty("Id").GetValue(val, null);
                        if (!object.Equals(oldValProVal, valProVal))
                        {
                            sbPros.Append(string.Format(" {0}=:{0},", propertyInfo.Name + "Id"));

                            OracleParameter oracleParameter = new OracleParameter(":" + propertyInfo.Name + "Id", valProVal == null ? DBNull.Value : valProVal);
                            parameters[i++] = oracleParameter;
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
            strSql.Append(string.Format(" where Id={0}", int.Parse(type.GetProperty("Id").GetValue(obj, null).ToString())));

            if (savedCount > 0)
            {
                ExecuteSql(strSql.ToString(), parameters);
            }
        }
        #endregion

        #region 删除
        /// <summary>
        /// 根据Id删除
        /// </summary>
        public static void Delete<T>(int id)
        {
            Type type = typeof(T);
            StringBuilder sbSql = new StringBuilder();
            sbSql.Append(string.Format("delete from {0} where Id={1}", type.Name, id));

            ExecuteSql(sbSql.ToString());
        }
        /// <summary>
        /// 根据Id集合删除
        /// </summary>
        public static void BatchDelete<T>(string ids)
        {
            if (string.IsNullOrWhiteSpace(ids)) return;

            Type type = typeof(T);
            StringBuilder sbSql = new StringBuilder();
            sbSql.Append(string.Format("delete from {0} where Id in ({1})", type.Name, ids));

            ExecuteSql(sbSql.ToString());
        }
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

            string sql = string.Format("select * from {0} where Id={1}", type.Name, type.GetProperty("Id").GetValue(obj, null));

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

            string sql = string.Format("select * from {0} where Id={1}", type.Name, id);

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

            string sql = string.Format("select * from {0} where Id={1}", type.Name, id);

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
        #endregion
        #region 事务
        #region 开始事务
        /// <summary>
        /// 开始事务
        /// </summary>
        public static void BeginTransaction()
        {
            GetTran();
            AddTranFlag();
        }
        #endregion

        #region 结束事务(正常结束)
        /// <summary>
        /// 结束事务(正常结束)
        /// </summary>
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
        /// <summary>
        /// 回滚事务(出错时调用该方法回滚)
        /// </summary>
        public static void RollbackTransaction()
        {
            GetTran().Rollback();
            RemoveTranFlag();
            GetOpenConnection().Close();
        }
        #endregion
        #endregion
        #endregion
        #endregion
        #endregion
    }
}
