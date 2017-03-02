using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SeatSaver.Models
{
    public class Venue
    {
        [Key]
        public int ID { get; set; }

        public string Name { get; set; }

        public virtual List<Row> Rows { get; set; }
        public virtual List<Event> Events { get; set; }
    }
}