using System.Data;
using System.Data.SqlClient;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;

namespace NHibernate.SqlAzure
{
    /// <summary>
    /// An <see cref="IDbCommand"/> implementation that wraps a <see cref="SqlCommand"/> object such that any
    /// queries that are executed are executed via a <see cref="ReliableSqlConnection"/>.
    /// </summary>
    /// <remarks>
    /// Note: For this to work it requires that the Connection property be set with a <see cref="ReliableSqlConnection"/> object.
    /// </remarks>
    public class ReliableSqlCommand : IDbCommand
    {
        /// <summary>
        /// The underlying <see cref="SqlCommand"/> being proxied.
        /// </summary>
        public System.Data.SqlClient.SqlCommand Current { get; private set; }

        /// <summary>
        /// The <see cref="ReliableSqlConnection"/> that has been assigned to the command via the Connection property.
        /// </summary>
        public ReliableSqlConnection ReliableConnection { get; set; }

        /// <summary>
        /// Constructs a <see cref="ReliableSqlCommand"/>.
        /// </summary>
        public ReliableSqlCommand()
        {
            Current = new System.Data.SqlClient.SqlCommand();
        }

        /// <summary>
        /// Explicit type-casting between a <see cref="ReliableSqlCommand"/> and a <see cref="SqlCommand"/>.
        /// </summary>
        /// <param name="command">The <see cref="ReliableSqlCommand"/> being casted</param>
        /// <returns>The underlying <see cref="SqlCommand"/> being proxied.</returns>
        public static explicit operator System.Data.SqlClient.SqlCommand(ReliableSqlCommand command)
        {
            return command.Current;
        }

        /// <summary>
        /// Returns the underlying <see cref="SqlConnection"/> and expects a <see cref="ReliableSqlConnection"/> when being set.
        /// </summary>
        public IDbConnection Connection
        {
            get { return Current.Connection; }
            set
            {
                ReliableConnection = ((ReliableSqlDbConnection)value).ReliableConnection;
                Current.Connection = ReliableConnection.Current;
            }
        }

        #region Wrapping code
        public void Dispose()
        {
            Current.Dispose();
        }

        public void Prepare()
        {
            Current.Prepare();
        }

        public void Cancel()
        {
            Current.Cancel();
        }

        public IDbDataParameter CreateParameter()
        {
            return Current.CreateParameter();
        }

        public int ExecuteNonQuery()
        {
            return ReliableConnection.ExecuteCommand(Current);
        }

        public IDataReader ExecuteReader()
        {
            return ReliableConnection.ExecuteCommand<IDataReader>(Current);
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            return ReliableConnection.ExecuteCommand<IDataReader>(Current, behavior);
        }

        public object ExecuteScalar()
        {
            return ReliableConnection.ExecuteCommand<int>(Current);
        }
        
        public IDbTransaction Transaction
        {
            get { return Current.Transaction; }
            set { Current.Transaction = (SqlTransaction)value; }
        }

        public string CommandText
        {
            get { return Current.CommandText; }
            set { Current.CommandText = value; }
        }

        public int CommandTimeout
        {
            get { return Current.CommandTimeout; }
            set { Current.CommandTimeout = value; }
        }

        public CommandType CommandType
        {
            get { return Current.CommandType; }
            set { Current.CommandType = value; }
        }

        public IDataParameterCollection Parameters
        {
            get { return Current.Parameters; }
        }

        public UpdateRowSource UpdatedRowSource
        {
            get { return Current.UpdatedRowSource; }
            set { Current.UpdatedRowSource = value; }
        }
        #endregion
    }
}