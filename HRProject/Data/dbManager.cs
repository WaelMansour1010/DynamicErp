using System.Data;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace EazyCash.Data
{
    public class dbManager : IDisposable
    {
        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                try


                {
                    Connection.Close();
                    Connection.Dispose();
                }
                catch
                {
                }
            }

            disposed = true;
        }

        public SqlConnection Connection { get; set; }
        private SqlTransaction tran { get; set; }


        public bool CheckConnection()
        {
            if (Connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (Connection.State != ConnectionState.Open)
            {
                try
                {

                    Connection.Open();

                }
                catch
                {
                    return false;
                }
            }
            return true;

        }

        public void BeginTransAction()
        {
            CheckConnection();
            tran = Connection.BeginTransaction();
        }

        public void RollbackTransaction()
        {
            try
            {
                tran.Rollback();
            }
            catch
            {
            }


        }

        public void CommitTransAction()
        {
            tran.Commit();
        }

        public dbManager( IDbConnection cnn  )
        {
            Connection = (SqlConnection)cnn; // new (cnn);
        }
        public dbManager ()
        {
            
            var connectionString = CurrentSession.Configuration.GetConnectionString("myconnection");
            Connection = new SqlConnection(connectionString);
        }

        public dbManager(IConfiguration configuration )
        {
            var connectionString = configuration.GetConnectionString("myconnection");
            Connection = new SqlConnection(connectionString);
        }

        private SqlCommand GetCommand()
        {
            SqlCommand cmd = Connection.CreateCommand();
            CheckConnection();
            cmd.Transaction = tran;
            return cmd;
        }



        public async Task<int> ExecuteAsync(string query, object prm = null  , CommandType cmdType = CommandType.Text)
        {
            CheckConnection();
            return await Connection.ExecuteAsync(sql:query , param:prm, transaction:tran,commandType: cmdType);

        }

        public int Execute(string query, object prms = null)
        {
            CheckConnection();
            return Connection.Execute(query, prms, tran);

        }
        public async Task<object> ExecuteScalarAsync(string stmt, object prms = null)
        {
            CheckConnection();
            return await Connection.ExecuteScalarAsync(stmt, prms, tran);
        }

        public async Task<object> ExecuteScalarAsync(string stmt, object prms = null, CommandType? commndType = null)
        {
            CheckConnection();
            return await Connection.ExecuteScalarAsync(stmt, prms, tran, null, commndType);
        }
        public async Task<T> GetFirstOrDefaultasync<T>(string query, object prms = null) where T : class
        {
            CheckConnection();
            return await Connection.QueryFirstOrDefaultAsync<T>(query, prms, tran);
        }
        public dynamic GetFirstOrDefault(string query, object prms = null)
        {
            CheckConnection();
            return this.Connection.QueryFirstOrDefault(query, prms, tran);
        }
        public async Task<dynamic> GetFirstOrDefaultAsync(string query, object prms = null)
        {
            CheckConnection();
            return await Connection.QueryFirstOrDefaultAsync(query, prms, tran);
           
        }
        public async Task<T> GetFirstOrDefaultAsync<T>(string query, object prms = null) where T : class
        {
            CheckConnection();
            return await Connection.QueryFirstOrDefaultAsync<T>(query, prms, tran);
        }
      
        //public async Task<ChefLib.CallCenterOrder?> GetCallCenterOrderAsync(Guid  rowid)  
        //{
        //    CheckConnection();
        //    return await Connection.GetAsync< ChefLib.CallCenterOrder?>(rowid,tran);
        //}
        public async Task<T> GetByKey<T>(object  key) where T:class 
        {
            CheckConnection();
            return await Connection.GetAsync<T>(key, tran);
        }


        public IEnumerable<dynamic> Query(string query, object prms = null)
        {
            CheckConnection();
            return Connection.Query(query, prms, tran);
        }
        public List<T> Query<T>(string query, object prms = null)
        {
            CheckConnection();
            return Connection.Query<T>(query, prms, tran).ToList();
        }
        public string GetFirstOrDefaultAsJson<T>(string query, object prms = null) where T : class
        {
            CheckConnection();
            T obj = Connection.QueryFirstOrDefault<T>(query, prms, tran);
            return JsonConvert.SerializeObject(obj);
        }








        public object ExecuteScalar(string stmt, object prms = null)
        {
            CheckConnection();
            return Connection.ExecuteScalar(stmt, prms, tran);
        }



        public long Insert<T>(T obj) where T : Bindable
        {
            CheckConnection();
            return Connection.Insert<T>(obj, tran);
        }

      
        public async Task<int> Insertasync<T>(T obj) where T : Bindable
        {
            CheckConnection();
            return await Connection.InsertAsync<T>(obj, tran);
        }
       
        
        public bool UpdateObj<T>(T obj) where T : class
        {
            CheckConnection();
            return Connection.Update<T>(obj, tran);
        }
        public async Task<bool> UpdateObjAsync<T>(T obj) where T :Bindable //class
        {
            CheckConnection();
            return await Connection.UpdateAsync<T>(obj, tran);
        }
        public bool UpdateObj<T>(List<T> obj) where T : Bindable
        {
            CheckConnection();
            return Connection.Update<List<T>>(obj, tran);
        }



        public string GetJson<T>(string query, object prms = null, CommandType type = CommandType.Text) where T : class
        {
            CheckConnection();
            List<T> obj = Connection.Query<T>(query, prms, tran, true, null, type).ToList();
            return JsonConvert.SerializeObject(obj);
        }
        //public List<T> Query<T>(string query, object prms = null, CommandType? type = null) where T : class
        //{
        //    CheckConnection();
        //    return Connection.Query<T>(query, prms, tran, true, null, type).ToList();
        //}

        public async Task<List<T>> QueryAsync<T>(string query, object prms = null, CommandType type = CommandType.Text) where T : class
        {
            CheckConnection();
            return (await Connection.QueryAsync<T>(query, prms, tran, null, type)).ToList();
        }

      

       

        public async Task<List<dynamic>> QueryAsync(string query, object prms = null, CommandType? type = null)
        {
            CheckConnection();
            return (await Connection.QueryAsync(query, prms, tran, null, type)).ToList();
        }





        public void Close()
        {
            try
            {
                Connection.Close();
                Connection.Dispose();
            }
            catch { }
        }


        public object QuerySingle<T>(string s, object o)
        {
            CheckConnection();
            return Connection.QuerySingle<T>(s, o, tran);
        }
        public DataTable Get(string Query)
        {
            return GetDataTable(Query, "", true);
        }

        public DataTable Get(string Query, string tblName, bool ignorSchima = false)
        {
            return GetDataTable(Query, tblName, ignorSchima);
        }
        public DataRow GetDatarow(string Query, string tblName, bool ignorSchima = false)
        {

            DataTable tbl = GetDataTable(Query, tblName, ignorSchima);
            if (tbl == null)
            {
                return null;
            }

            if (tbl.Rows.Count == 0)
            {
                return null;
            }

            return tbl.Rows[0];

        }




        private DataTable GetDataTable(string query, string tblName, bool ignorSchima = false)
        {

            DataTable dt = new DataTable(tblName);
            SqlCommand cmd = GetCommand();
            CheckConnection();
            cmd.CommandText = query;
            using (SqlDataAdapter adp = new SqlDataAdapter(cmd))

            {
                if (!ignorSchima)
                {

                    adp.FillSchema(dt, SchemaType.Source);
                }


                adp.Fill(dt);
                if (tblName != "")
                {
                    foreach (DataColumn c in dt.Columns)
                    {
                        c.ReadOnly = false;
                    }
                }


            }
            return dt;

        }
    }
}
