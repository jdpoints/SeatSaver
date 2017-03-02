using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web;
using SeatSaver.Models;
using System.Runtime.CompilerServices;

namespace SeatSaver
{
    public class SeatSelector
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        public Responses.ReservationResponse ReserveBestSeats(int customerID, int eventID, int numberOfSeats, int maxRows)
        {
            Responses.ReservationResponse response = new Responses.ReservationResponse();

            List<Seat> seats = FindSeatsInVenue(numberOfSeats, eventID, maxRows);
            Order order = this.CreateOrder(customerID, eventID, seats);

            response.Order = order;

            if (order != null)
            {
                response.Success = true;
                response.Message = "Your seats were successfully reserved.";
            }
            else
            {
                response.Success = false;
                response.Message = "Unable to reserve the requested seats. Try a smaller order or allow seats to be split across more rows.";
            }

            return response;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private List<Seat> FindSeatsInVenue(int seats, int eventID, int maxRows)
        {
            List<Seat> selectedSeats = new List<Seat>();

            using (var db = new ReservationContext())
            {
                List<Row> rows = db.Events
                    .Include(i => i.Venue.Rows.Select(s => s.Seats))
                    .FirstOrDefault(f => f.ID.Equals(eventID))
                    .Venue
                    .Rows
                    .OrderBy(o => o.RowNumber)
                    .ToList();

                int[] takenSeats = db.Orders
                    .Where(w => w.EventID.Equals(eventID))
                    .SelectMany(s => s.OrderSeats)
                    .Select(s => s.Seat)
                    .Select(s => s.ID)
                    .ToArray();

                //int[] takenSeats = db.Orders
                //    .Where(w => w.EventID.Equals(eventID))
                //    .Select(s => s.Seats)
                //    .SelectMany(m => m.Select(s => s.ID))
                //    .ToArray();

                int seatsNeeded = seats;
                int rowsRemaining = maxRows;

                foreach(Row row in rows)
                {
                    List<Seat> newSeats = this.FindSeatsInRow(seatsNeeded, row, takenSeats);

                    if (newSeats.Count > 0)
                    {
                        seatsNeeded -= newSeats.Count;
                        rowsRemaining--;
                        selectedSeats.AddRange(newSeats);
                    }
                    else
                    {
                        seatsNeeded += this.RemoveNearestRow(ref selectedSeats);
                    }

                    if (seatsNeeded == 0)
                    {
                        break;
                    }
                    else if (rowsRemaining == 0)
                    {
                        seatsNeeded += this.RemoveNearestRow(ref selectedSeats);
                    }
                }

                if (seatsNeeded > 0)
                {
                    selectedSeats.Clear();
                }

                return selectedSeats;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private int RemoveNearestRow(ref List<Seat> selectedSeats)
        {
            // Remove Seats from row nearest front and keep looking
            int rowID = selectedSeats
                .Select(s => s.RowID)
                .OrderBy(o => o)
                .First();

            List<Seat> dropSeats = selectedSeats
                .Where(w => w.RowID.Equals(rowID))
                .ToList();

            selectedSeats.RemoveAll(r => dropSeats.Contains(r));

            return dropSeats.Count;
        }

        /// <summary>
        /// Attempt to locate seats in a row
        /// </summary>
        /// <param name="seats">Number of seats needed</param>
        /// <param name="row">Entity Row to search for seats in</param>
        /// <param name="takenSeats">Array of SeatIDs for taken seats</param>
        /// <returns>Returns a list of available seats or an empty list if no valid seats were found</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private List<Seat> FindSeatsInRow(int seats, Row row, int[] takenSeats)
        {
            int neededSeats = seats;
            List<Seat> selectedSeats = new List<Seat>();

            foreach (Seat seat in row.Seats.OrderBy(o => o.SeatNumber))
            {
                if (takenSeats.Contains(seat.ID))
                {
                    selectedSeats.Clear();
                }
                else
                {
                    selectedSeats.Add(seat);
                    neededSeats--;
                    if (neededSeats == 0) { break; }
                }
            }

            return selectedSeats;
        }

        //[MethodImpl(MethodImplOptions.Synchronized)]
        private Order CreateOrder(int customerID, int eventID, List<Seat> seats)
        {
            if (seats.Count > 0)
            {
                using (var db = new ReservationContext())
                using (DbContextTransaction transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        Order newOrder = new Order();
                        newOrder.CustomerID = customerID;
                        newOrder.EventID = eventID;

                        db.Orders.Add(newOrder);

                        foreach(Seat seat in seats)
                        {
                            OrderSeat newSeat = new OrderSeat();
                            newSeat.Order = newOrder;
                            newSeat.SeatID = seat.ID;
                            
                            newOrder.OrderSeats.Add(newSeat);
                            db.OrderSeats.Add(newSeat);
                            //db.SaveChanges();

                            //newOrder.Seats.Add(seat);
                        }

                        db.SaveChanges();
                        transaction.Commit();

                        return db.Orders
                            //.Include(i => i.Seats)
                            .Include(i => i.OrderSeats)
                            .Include(i => i.OrderSeats.Select(s => s.Seat))
                            .Where(w => w.ID.Equals(newOrder.ID))
                            .FirstOrDefault();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                    }
                }
            }

            return null;
        }
    }
}