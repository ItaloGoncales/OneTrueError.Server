﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using OneTrueError.Infrastructure;
using OneTrueError.Web.Infrastructure;

[assembly: PreApplicationStartMethod(typeof(SchemaUpdateModule), "Register")]
namespace OneTrueError.Web.Infrastructure
{
    public class SchemaUpdateModule : IHttpModule
    {
        private static string _errorMessage;

        public static void Register()
        {
            DynamicModuleUtility.RegisterModule(typeof(SchemaUpdateModule));
        }
        public void Init(HttpApplication context)
        {
            if (ConfigurationManager.AppSettings["Configured"] != "true")
                return;

            if (!SetupTools.DbTools.CanSchemaBeUpgraded())
            {
                return;
            }

            try
            {
                SetupTools.DbTools.UpgradeDatabaseSchema();
                return;
            }
            catch (Exception ex)
            {
                int nest = 1;
                var msg = ex.Message + "\r\n";
                ex = ex.InnerException;
                while (ex != null)
                {
                    msg += " ".PadLeft(nest*2) + ex.Message + "\r\n";
                    ex = ex.InnerException;
                }
                _errorMessage = msg;
            }

            context.BeginRequest += OnRequest;
        }

        private void OnRequest(object sender, EventArgs e)
        {
            var app = (HttpApplication) sender;
            app.Response.StatusCode = 500;
            app.Response.ContentType = "text/plain";
            var sw = new StreamWriter(app.Response.OutputStream);
            sw.WriteLine("Database schema upgrade failed");
            sw.WriteLine();
            sw.WriteLine("Failed to update the database schema. Sorry for that. We do however promise to help you as fast as we can.");
            sw.WriteLine("Email the contents below to support@onetrueerror.com. Include your MS SQL server version.");
            sw.WriteLine();
            sw.WriteLine("============================");
            sw.WriteLine(_errorMessage);
            sw.WriteLine("============================");
            sw.Flush();
            app.Response.TrySkipIisCustomErrors = true;
            app.Response.End();
        }

        public void Dispose()
        {
            
        }
    }
}