using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

using Rock.Plugin;

namespace com.shepherdchurch.DigitalSignage.Migrations
{
    public abstract class ExtendedMigration : Migration
    {
        protected void Sql( string sql, Dictionary<string, object> parameters = null )
        {
            if ( SqlConnection != null || SqlTransaction != null )
            {
                using ( SqlCommand sqlCommand = new SqlCommand( sql, SqlConnection, SqlTransaction ) )
                {
                    sqlCommand.CommandType = CommandType.Text;
                    if ( parameters != null )
                    {
                        foreach ( var key in parameters.Keys )
                        {
                            sqlCommand.Parameters.AddWithValue( key, parameters[key] );
                        }
                    }
                    sqlCommand.ExecuteNonQuery();
                }
            }
            else
            {
                throw new NullReferenceException( "The Plugin Migration requires valid SqlConnection and SqlTransaction values when executing SQL" );
            }
        }

        protected object SqlScalar( string sql, Dictionary<string, object> parameters = null )
        {
            if ( SqlConnection != null || SqlTransaction != null )
            {
                using ( SqlCommand sqlCommand = new SqlCommand( sql, SqlConnection, SqlTransaction ) )
                {
                    sqlCommand.CommandType = CommandType.Text;
                    if ( parameters != null )
                    {
                        foreach ( var key in parameters.Keys )
                        {
                            sqlCommand.Parameters.AddWithValue( key, parameters[key] );
                        }
                    }
                    return sqlCommand.ExecuteScalar();
                }
            }
            else
            {
                throw new NullReferenceException( "The Plugin Migration requires valid SqlConnection and SqlTransaction values when executing SQL" );
            }
        }

        protected void Sql( string sql, string key, object value )
        {
            Sql( sql, new Dictionary<string, object> { { key, value } } );
        }

        protected object SqlScalar( string sql, string key, object value )
        {
            return SqlScalar( sql, new Dictionary<string, object> { { key, value } } );
        }
    }
}
