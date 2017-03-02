using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SeatSaver.Models
{
    public class OrderSeat
    {
        [Key]
        public int ID { get; set; }
        public int OrderID { get; set; }
        public int SeatID { get; set; }

        public virtual Order Order { get; set; }
        public virtual Seat Seat { get; set; }
    }
}