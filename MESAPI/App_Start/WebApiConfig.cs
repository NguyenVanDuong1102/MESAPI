using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

namespace MES
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Enable CORS (Access-Control-Allow-Origin) - Add by Jackie Than
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            var enableCorsAttribute = new EnableCorsAttribute("*", "*", "*")
            {
                SupportsCredentials = true
            };

            config.EnableCors(enableCorsAttribute);
        }
    }
}
