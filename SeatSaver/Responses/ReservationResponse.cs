using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SeatSaver.Responses
{
    public class ReservationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int OrderID { get; set; }
        public List<ReservationSeat> Seats { get; set; }

        #region Named Responses
        public static ReservationResponse InvalidEventResponse
        {
            get
            {
                return new ReservationResponse
                {
                    Success = false,
                    Message = "You supplied an invalid EventID.",
                    OrderID = 0,
                    Seats = null
                };
            }
        }

        public static ReservationResponse ReservationCriteriaNotMet
        {
            get
            {
                return new ReservationResponse
                {
                    Success = false,
                    Message = "Unable to reserve the requested seats. Try a smaller order or allow seats to be split across more rows.",
                    OrderID = 0,
                    Seats = null
                };
            }
        }
        #endregion
    }

    public class ReservationSeat
    {
        public int RowNumber { get; set; }
        public int SeatNumber { get; set; }
    }
}