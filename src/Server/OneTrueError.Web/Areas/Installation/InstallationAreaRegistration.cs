﻿using System.Configuration;
using System.Web.Mvc;
using OneTrueError.App.Configuration;
using OneTrueError.Infrastructure.Configuration;

namespace OneTrueError.Web.Areas.Installation
{
    public class InstallationAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Installation";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            if (ConfigurationManager.AppSettings["Configured"] == "true")
                return;

            context.MapRoute(
                "Installation_default",
                "Installation/{controller}/{action}/{id}",
                new { action = "Index", controller = "Setup", id = UrlParameter.Optional }
            );
        }
    }
}