﻿/*
 * 2017年5月25日 13:48:29 郑少宝
 * 
 * 在这个什么都善变的人间
 * 我想和你
 * 看一看永远
 * 
 */
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace zsbApps.DBHelper
{
	public class MsSql : DBHelper, IDBClassify
	{
		/// <summary>
		/// 连接字符串
		/// </summary>
		private string _connStr;

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="connStr">连接字符串</param>
		public MsSql(string connStr)
		{
			this._connStr = connStr;
		}

		/// <summary>
		/// 执行简单的 sql 语句
		/// </summary>
		/// <param name="exsql">执行的 sql 语句</param>
		/// <returns>影响行数</returns>
		public override int ExecuteSql(string exsql)
		{
			using (SqlConnection connection = new SqlConnection(this._connStr))
			{
				using (SqlCommand cmd = new SqlCommand(exsql, connection))
				{
					try
					{
						connection.Open();
						int rows = cmd.ExecuteNonQuery();
						return rows;
					}
					catch (SqlException ex)
					{
						connection.Close();
						throw ex;
					}
				}
			}
		}

		/// <summary>
		/// 批量执行 sql 语句
		/// 遇到执行错误就抛出异常，已经成功的不会回滚
		/// </summary>
		/// <param name="listSql">执行的 sql 语句集合</param>
		/// <returns>影响行数</returns>
		public override (int count, string error) ExecuteSql(List<string> listSql)
		{
			using (SqlConnection connection = new SqlConnection(this._connStr))
			{
				int count = 0;
				string error = null;
				try
				{ 
					connection.Open();
					SqlCommand cmd = new SqlCommand();
					cmd.Connection = connection;
					foreach (var item in listSql)
					{
						cmd.CommandText = item;
						int x = cmd.ExecuteNonQuery();
						count += x >= 0 ? x : 0;
					}
				}
				catch (SqlException ex)
				{
					connection.Close();
					error = ex.Message;
				}
				return (count: count, error: error);
			}
		}

		/// <summary>
		/// 事务执行 sql 语句
		/// </summary>
		/// <param name="listSql">执行的 sql 语句集合</param>
		/// <returns>影响行数</returns>
		public override int ExecuteTran(List<string> listSql)
		{
			return ExecuteTran(listSql, IsolationLevel.ReadCommitted);
		}

		/// <summary>
		/// 事务执行 sql 语句
		/// </summary>
		/// <param name="listSql">执行的 sql 语句集合</param>
		/// <param name="level">指定连接的事务锁定行为</param>
		/// <returns>影响行数</returns>
		public override int ExecuteTran(List<string> listSql, IsolationLevel level)
		{
			using (SqlConnection connection = new SqlConnection(this._connStr))
			{
				connection.Open();
				using (SqlCommand cmd = connection.CreateCommand())
				{
					SqlTransaction tran = connection.BeginTransaction(level);
					cmd.Transaction = tran;
					try
					{
						int x = 0;
						foreach (var item in listSql)
						{
							cmd.CommandText = item;
							x += cmd.ExecuteNonQuery();
						}
						tran.Commit();
						return x;
					}
					catch (SqlException ex)
					{
						tran.Rollback();
						connection.Close();
						throw ex;
					}
				}
			}
		}

		/// <summary>
		/// 查询
		/// </summary>
		/// <param name="sql">查询语句</param>
		/// <returns>DataSet</returns>
		public override DataSet Query(string sql)
		{
			return fill(sql, 0);
		}

		/// <summary>
		/// 查询
		/// </summary>
		/// <param name="sql">查询语句</param>
		/// <returns>DataTable</returns>
		public override DataTable QueryTable(string sql)
		{
			return fill(sql, 1);
		}

		/// <summary>
		/// 返回第一行第一列 返回类型为 object
		/// </summary>
		/// <param name="sql">查询语句</param>
		/// <returns>返回第一行第一列</returns>
		public override object GetSingle(string sql)
		{
			using (SqlConnection connection = new SqlConnection(this._connStr))
			{
				using (SqlCommand cmd = new SqlCommand(sql, connection))
				{
					try
					{
						connection.Open();
						return cmd.ExecuteScalar();
					}
					catch (SqlException ex)
					{
						connection.Close();
						throw ex;
					}
				}
			}
		}

		#region 私有函数
		/// <summary>
		/// 填充数据集
		/// </summary>
		/// <param name="sql">查询语句</param>
		/// <param name="modal">查询模式（0 DataSet，1 DataTable）</param>
		/// <returns></returns>
		private dynamic fill(string sql, int modal)
		{
			dynamic result;
			switch (modal)
			{
				case 0:
					result = new DataSet();
					break;
				case 1:
					result = new DataTable();
					break;
				default:
					return null;
			}

			using (SqlConnection connection = new SqlConnection(this._connStr))
			{
				try
				{
					connection.Open();
					new SqlDataAdapter(sql, connection).Fill(result);
					return result;
				}
				catch (SqlException ex)
				{
					connection.Close();
					throw ex;
				}
			}
		}
		#endregion

		#region IDBClassify 接口实现
		/// <summary>
		/// 获取类型
		/// </summary>
		/// <returns>数据库类型</returns>
		DBClassify IDBClassify.GetClassify()
		{
			return DBClassify.MsSql;
		}
		#endregion
	}
}
