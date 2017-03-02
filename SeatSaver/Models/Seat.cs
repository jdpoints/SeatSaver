using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SeatSaver.Models
{
    public class Seat
    {
        [Key]
        public int ID { get; set; }

        public int RowID { get; set; }
        public int SeatNumber { get; set; }

        [ForeignKey("RowID")]
        public virtual Row Row { get; set; }

        public virtual ICollection<OrderSeat> OrderSeats { get; set; }
    }
}