using System;
using System.Data;
using System.Data.SqlClient;
using MainProject.Web.Util;

namespace MainProject.Web.Repositories.Shared
{
    /// <summary>
    /// Contains database connectionstring and additional related database setup options. 
    /// </summary>
    [Serializable]
    public class ConnectionFactory
    {
        // Default Connection
        /// <summary>
        /// Default connection string populated by connection string source. 
        /// Source may come from .config or .json file.
        /// </summary>
        public string DefaultConnectionString { get; set; }

        public string TenantConnectionString { get; set; }

        public string TenantDbName { get; set; }

        /// <summary>
        /// Assigns a Tenant-specific ConnectionString or handle the error in the case
        /// that DBName is not found in the configuration database.
        /// </summary>
        /// <param name="tenantKey">The Tenant's API-Key used to find their DB name.</param>
        public void SetCurrentTenantDbName(string tenantKey)
        {
            if (!string.IsNullOrEmpty(tenantKey))
            {
                // Get Tenant DB using/Key
                SqlConnection conn = new SqlConnection(this.TenantConnectionString);
                using (SqlDataAdapter da = new SqlDataAdapter(@"SELECT DBName
                                                                FROM usv_GetCustomerDB WITH (NOLOCK)
                                                                WHERE APIKey = @key OPTION(RECOMPILE)", conn))
                {
                    // Params
                    da.SelectCommand.Parameters.Add(new SqlParameter("@key", tenantKey));
                    try
                    {
                        conn.Open();
                        // Get DBname using scalar
                        this.TenantDbName = da.SelectCommand.ExecuteScalar().ToString();
                    }
                    catch (Exception)
                    {
                        // Coming soon with an exception near you!
                    }
                    finally
                    {
			if (conn.State == ConnectionState.Open)
			{
				conn.Close();
			}
		    }
                }

                if (!string.IsNullOrWhiteSpace(this.TenantDbName))
                {
                    this.SetTenantConnection();
                }
                else
                {
                    this.DefaultConnectionString = "";
                }
            }
            else
            {
                //Sets the an empty default to handle the On-Site API deployments.
                this.DefaultConnectionString = "";
            }
        }

        public void SetCurrentTenantDbNameFromSubKey(string tenantKey)
        {
            if (!string.IsNullOrEmpty(tenantKey))
            {
                this.TenantDbName = string.Empty;
                string ApiKeyFromSubKey = string.Empty;
                try
                {
                    ApiKeyFromSubKey = ProductLicensing.decrypt(tenantKey);
                }
                catch
                {
                    ApiKeyFromSubKey = string.Empty;
                }

                if (!string.IsNullOrEmpty(ApiKeyFromSubKey))
                {
                    SqlConnection conn = new SqlConnection(this.TenantConnectionString);
                    using (SqlDataAdapter da = new SqlDataAdapter(@"SELECT DBName
                                                                    FROM usv_GetCustomerDB
                                                                    WHERE APIKey = @key", conn))
                    {
                        // Params
                        da.SelectCommand.Parameters.Add(new SqlParameter("@key", ApiKeyFromSubKey));
                        try
                        {
                            conn.Open();
                            // Get DBname using scalar
                            this.TenantDbName = da.SelectCommand.ExecuteScalar().ToString();
                        }
                        catch (Exception)
                        {
                            // Coming soon with an exception near you!
                        }
                        finally
                        { if (conn.State == ConnectionState.Open) { conn.Close(); } }

                    }
                }

                if (!string.IsNullOrWhiteSpace(this.TenantDbName))
                {
                    this.SetTenantConnection();
                }
                else
                {
                    //Sets an empty defaultstring to handle On-Site API deployments.
                    this.DefaultConnectionString = "";
                }
            }
            else
            {
                this.DefaultConnectionString = "";
            }
        }


        public bool ValidPath(string Path, string key)
        {
            if (!string.IsNullOrEmpty(this.DefaultConnectionString))
            {
                SqlConnection conn = new SqlConnection(this.DefaultConnectionString);
                using (SqlDataAdapter da = new SqlDataAdapter(@"SELECT ITD.APIRoute
                                                                FROM tblIntegrationKeys IK
                                                                LEFT JOIN tblIntegrationTypeDetail ITD ON IK.IntegrationTypeDetailID = ITD.ID
                                                                WHERE IK.IsDeleted = 0 and IK.[Key] = @key AND GETUTCDATE() < IK.ExpirationDate", conn))
                {
                    // Params
                    da.SelectCommand.Parameters.Add(new SqlParameter("@key", key));
                    try
                    {
                        conn.Open();
                        // Get DBname using scalar
                        return Path.ToLower().Contains(da.SelectCommand.ExecuteScalar().ToString().ToLower());
                    }
                    catch (Exception)
                    {
                        // Coming soon with an exception near you!
                    }
                    finally
                    { if (conn.State == ConnectionState.Open) { conn.Close(); } }

                }
            }

            return false;
        }

        /// <summary>
        /// Inserts the current Tenant's DBName into their DefaultConnectionString.
        /// </summary>
        private void SetTenantConnection()
        {
            this.DefaultConnectionString = this.DefaultConnectionString.Replace("{tenant}", this.TenantDbName);
        }

    }
}
