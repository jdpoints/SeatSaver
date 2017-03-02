using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SeatSaver.Models
{
    public class Event
    {
        [Key]
        public int ID { get; set; }

        public int VenueID { get; set; }
        public string Name { get; set; }
        public DateTime DateTime { get; set; }

        public virtual Venue Venue { get; set; }
    }
}