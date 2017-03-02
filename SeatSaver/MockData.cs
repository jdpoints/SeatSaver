using SeatSaver.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity.Infrastructure; // namespace for the EdmxWriter class
using System.Xml;
using System.Text;

namespace SeatSaver
{
    public static class MockData
    {
        public static void Create()
        {
            //MockData.CreateEDMX();

            try
            {
                using (var db = new ReservationContext())
                {
                    // Create Venue
                    Venue newVenue = new Venue { Name = "Bud Walton Arena" };
                    db.Venues.Add(newVenue);

                    // Create 5 Rows with 5 Seats each
                    for (int i = 1; i <= 5; i++)
                    {
                        Row newRow = new Row { Venue = newVenue, RowNumber = i };
                        db.Rows.Add(newRow);
                        //newVenue.Rows.Add(newRow);

                        for (int j = 1; j <= 5; j++)
                        {
                            Seat newSeat = new Seat { Row = newRow, SeatNumber = j };
                            db.Seats.Add(newSeat);
                            //newRow.Seats.Add(newSeat);
                        }
                    }

                    // Create Event 1
                    Event event1 = new Event { Name = "Basketball Game", DateTime = DateTime.Now.AddDays(-60), Venue = newVenue };
                    db.Events.Add(event1);

                    // Create Event 2
                    Event event2 = new Event { Name = "Graduation", DateTime = DateTime.Now.AddDays(15), Venue = newVenue };
                    db.Events.Add(event2);

                    // Create Customer 1
                    Customer customer1 = new Customer { FirstName = "John", LastName = "Smith", Address = "123 Sesame St." };
                    db.Customers.Add(customer1);

                    // Create Customer 2
                    Customer customer2 = new Customer { FirstName = "Jane", LastName = "Doe", Address = "456 Winding Way" };
                    db.Customers.Add(customer2);

                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private static void CreateEDMX()
        {
            using (var ctx = new ReservationContext())
            {
                using (var writer = new XmlTextWriter(@"C:\Users\Josh\Documents\visual studio 2015\Projects\SeatSaver\Model.edmx", Encoding.Default))
                {
                    EdmxWriter.WriteEdmx(ctx, writer);
                }
            }
        }
    }
}