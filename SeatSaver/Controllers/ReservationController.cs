using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data.Entity;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace SeatSaver.Controllers
{
    public class ReservationController : ApiController
    {
        [HttpPost]
        [Route("api/reservation")]
        public Responses.ReservationResponse ReserveSeats([FromBody] ApiReservationRequest request)
        {
            SeatSelector selector = new SeatSelector();

            return selector.ReserveBestSeats(request.CustomerID, request.EventID, request.NumberOfSeats, request.MaxRows);
        }
    }

    public class ApiReservationRequest
    {
        public int CustomerID { get; set; }
        public int EventID { get; set; }
        public int NumberOfSeats { get; set; }

        public int MaxRows { get; set; } = 1;
    }
}
