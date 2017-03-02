using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SeatSaver.Models
{
    public class Order
    {
        public Order()
        {
            OrderSeats = new HashSet<OrderSeat>();
        }

        [Key]
        public int ID { get; set; }

        public int EventID { get; set; }
        public int CustomerID { get; set; }

        public virtual Event Event { get; set; }
        public virtual Customer Customer { get; set; }

        public virtual ICollection<OrderSeat> OrderSeats { get; set; }
    }
}