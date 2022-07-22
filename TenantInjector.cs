using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MainProject.Web.Repositories.Shared;

namespace MainProject.Web.API.Shared
{
    /// <summary>
    /// Middleware for identifying the current Tenant to set
    /// the Current Tenant's Default Connection String.
    /// </summary>
    public class TenantInjector
    {
        #region Class Variables

        private readonly RequestDelegate _next;
        private readonly IConfiguration _config;        

        #endregion

        #region Constructors

        // Default Constructor
        public TenantInjector(RequestDelegate next, IConfiguration configuration)
        {
            this._next = next;
            this._config = configuration;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets called when any request hits the API then assigns a Default ConnectionString to use
        /// for the current user's session based on an apiKey.
        /// </summary>
        /// <param name="httpContext">The current <see cref="HttpContext"/> for the request.</param>
        /// <param name="factory">The injected Connection Factory.</param>
        /// <returns></returns>
        public async Task Invoke(HttpContext httpContext, ConnectionFactory factory)
        {
            factory.TenantConnectionString = this._config.GetSection("ConnectionStrings:TenantConnection")?.Value;
            factory.DefaultConnectionString = this._config.GetSection("ConnectionStrings:DefaultConnection")?.Value;
			factory.SetCurrentTenantDbName(httpContext.Request.Headers["apiKey"].ToString());
			
            if (string.IsNullOrEmpty(factory.TenantConnectionString) && (string.IsNullOrEmpty(factory.DefaultConnectionString)))
            {
                factory.DefaultConnectionString = this._config.GetSection("ConnectionStrings:OnSiteConnection")?.Value;
            }
            else if (string.IsNullOrEmpty(factory.DefaultConnectionString))
            {
                factory.DefaultConnectionString = this._config.GetSection("ConnectionStrings:DefaultConnection")?.Value;

                factory.SetCurrentTenantDbNameFromSubKey(httpContext.Request.Headers["apiKey"].ToString());

                if (string.IsNullOrEmpty(factory.DefaultConnectionString))
                {
                    httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    byte[] errorMessage = Encoding.ASCII.GetBytes("Invalid API key. Please contact Support.");
                    await httpContext.Response.Body.WriteAsync(errorMessage, 0, errorMessage.Length);
                    await httpContext.Response.CompleteAsync();
                }
                else
                {
                    if (!factory.ValidPath(httpContext.Request.Path.Value, httpContext.Request.Headers["apiKey"].ToString()))
                    {
                        httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        byte[] errorMessage = Encoding.ASCII.GetBytes("Invalid API key for Route. Please contact Support.");
                        await httpContext.Response.Body.WriteAsync(errorMessage, 0, errorMessage.Length);
                        await httpContext.Response.CompleteAsync();
                    }
                }
            }

            try
            {
                await this._next(httpContext);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    #endregion

    #region Child Classes

    /// <summary>
    /// Extension method used to add the middleware to the HTTP request pipeline.
    /// </summary>
    public static class TenantInjectorExtensions
    {
        public static IApplicationBuilder UseTenantInjector(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantInjector>();
        }
    }

    #endregion
}
