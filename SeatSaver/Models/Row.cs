using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SeatSaver.Models
{
    public class Row
    {
        [Key]
        public int ID { get; set; }

        public int VenueID { get; set; }

        public int RowNumber { get; set; }

        public virtual Venue Venue { get; set; }

        public virtual List<Seat> Seats { get; set; }
    }
}