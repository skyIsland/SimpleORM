using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using MiniORM.Attributes;

namespace MiniORM
{
    public class ORMHelper
    {
        public string ConnStr { get; }

        public Type Type { get; }

        public ORMHelper(string s)
        {
            this.ConnStr = s;
        }

        #region 辅助方法

        /// <summary>
        /// 通过类型获得表名
        /// </summary>
        /// <returns></returns>
        private string GetTableName(Type type)
        {
            var attribute = type.GetCustomAttributes(typeof(KeyAttribute), false);
            var key = (KeyAttribute)attribute[0];

            return key.TableName;
        }

        /// <summary>
        /// 获取主键名
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private string GetPrimaryKey(Type type)
        {
            var attribute = type.GetCustomAttributes(typeof(KeyAttribute), false);
            var key = (KeyAttribute)attribute[0];

            return key.PKName;
        }

        #endregion

        public void CreateTable(Type type)
        {
            var sb = new StringBuilder(200);

            // 获得表名
            var tableName = GetTableName(type);

            sb.Append(string.Format("if not exists (select * from sysobjects where name = '{0}' and xtype = 'U') ", tableName));
            sb.Append(string.Format("create table [{0}]", tableName));
            sb.Append("(");

            // 获得主键名
            var pk = GetPrimaryKey(type);

            // 使用反射遍历实体所有的属性
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                // 获得特性
                var attribute = property.GetCustomAttributes(typeof(DataFieldAttribute), false);
                var column = (DataFieldAttribute)attribute[0];
                if (column.Name.Equals(pk,StringComparison.OrdinalIgnoreCase))
                    sb.Append(string.Format("[{0}] {1} IDENTITY NOT NULL PRIMARY KEY,",
                        column.Name,
                        column.Type));
                else
                {
                    sb.Append(string.Format("[{0}] {1} NOT NULL,",
                        column.Name,
                        column.Type));
                }
            }
            var sql = sb.ToString().Substring(0, sb.Length - 1) + ")";

            // 执行方法
            ExecuteNonQuery(sql);

            Console.WriteLine("表" + tableName + "已被建立！");
        }

        /// <summary>
        /// 执行非查询SQL
        /// </summary>
        /// <param name="sql"></param>
        private void ExecuteNonQuery(string sql)
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();

                var cmd = new SqlCommand();
                cmd.CommandText = sql;
                cmd.Connection = conn;
                cmd.Transaction = trans;

                cmd.ExecuteNonQuery();

                trans.Commit();
            }
        }

        /// <summary>
        /// 判断表中主键为id的记录是否已经存在
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool HasExist(Type type, object id)
        {
            var pk = GetPrimaryKey(type);
            var tableName = GetTableName(type);
            var sql = "select * from " + tableName + " where " + pk + " = " + id;
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                var cmd = new SqlCommand();
                cmd.CommandText = sql;
                cmd.Connection = conn;

                var reader = cmd.ExecuteReader();
                return reader.HasRows;
            }
        }

        public void DeleteTable(Type type)
        {
            var tableName = GetTableName(type);
            var sql = "drop table " + tableName;
            ExecuteNonQuery(sql);

            Console.WriteLine("表" + tableName + "已被删除！");
        }

        /// <summary>
        /// 插入
        /// </summary>
        /// <param name="newObject"></param>
        public void Insert(object newObject)
        {
            var type = newObject.GetType();
            var tableName = GetTableName(type);
            var pk = GetPrimaryKey(type);
            object newObjectPKValue = new object();

            // 从传入的对象中反射出pk的值 
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (property.Name == pk)
                {
                    // 通过GetValue获得值
                    newObjectPKValue = property.GetValue(newObject);
                    break;
                }
            }

            // 搜索表中是否含有相同PK的记录
            if (HasExist(type, newObjectPKValue))
            {
                Console.WriteLine("表中已有相同PK的记录，不能重复插入！");
                return;
            }

            var sb = new StringBuilder(200);
            sb.Append("insert into " + tableName + " values (");

            // 遍历传入的对象的属性，并获得它们的值，用以拼凑INSERT语句
            foreach (var property in type.GetProperties())
            {
                sb.Append("'");
                sb.Append(property.GetValue(newObject).ToString());
                sb.Append("',");
            }

            var sql = sb.ToString().Substring(0, sb.Length - 1) + ")";
            ExecuteNonQuery(sql);
        }

        /// <summary>
        /// 统计表中有多少记录
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public int Count(Type type)
        {
            var pk = GetPrimaryKey(type);
            var tableName = GetTableName(type);
            var sql = "select count(" + pk + ") from " + tableName;
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                var cmd = new SqlCommand();
                cmd.CommandText = sql;
                cmd.Connection = conn;

                var reader = cmd.ExecuteReader();
                reader.Read();
                return reader.GetInt32(0);
            }
        }

        /// <summary>
        /// 根据主键返回对应的记录
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public object SelectById(Type type, int id)
        {
            // 获得类型的实例
            var obj = Activator.CreateInstance(type);

            var pk = GetPrimaryKey(type);
            var tableName = GetTableName(type);
            var sql = "select * from " + tableName + " where " + pk + " = " + id;
            using (SqlConnection conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                var cmd = new SqlCommand();
                cmd.CommandText = sql;
                cmd.Connection = conn;

                var reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    int i = 0;

                    // 所有属性，包括私有的
                    foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                    {
                        // 为属性赋值
                        property.SetValue(obj, reader.GetValue(i), null);
                        i++;
                    }
                    return obj;
                }
                else
                {
                    Console.WriteLine("表中不存在id为" + id + "的记录！");
                    return null;
                }
            }
        }
    }
}
