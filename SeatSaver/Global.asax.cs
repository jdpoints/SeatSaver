using SeatSaver.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace SeatSaver
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Database.SetInitializer<ReservationContext>(new DropCreateDatabaseAlways<ReservationContext>());
            MockData.Create();
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
