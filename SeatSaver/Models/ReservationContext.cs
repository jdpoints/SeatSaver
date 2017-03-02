using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace SeatSaver.Models
{
    public class ReservationContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderSeat> OrderSeats { get; set; }
        public DbSet<Row> Rows { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Venue> Venues { get; set; }

        
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

            //modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();

            modelBuilder.Entity<OrderSeat>()
               .HasRequired(r => r.Order)
               .WithMany(w => w.OrderSeats)
               .HasForeignKey(k => k.OrderID)
               .WillCascadeOnDelete(false);

            modelBuilder.Entity<OrderSeat>()
               .HasRequired(r => r.Seat)
               .WithMany(w => w.OrderSeats)
               .HasForeignKey(k => k.SeatID)
               .WillCascadeOnDelete(false);

        }
    }
}