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

            // Check if valid event
            bool invalidEvent = false;
            using (var db = new ReservationContext())
            {
                int eventCount = db.Events.Where(w => w.ID.Equals(eventID)).Count();
                invalidEvent = (eventCount != 1); // Should be exactly 1 event per EventID
            }

            if (invalidEvent) { return Responses.ReservationResponse.InvalidEventResponse; }

            List<Seat> seats = FindSeatsInVenue(numberOfSeats, eventID, maxRows);
            Order order = this.CreateOrder(customerID, eventID, seats);

            if (order != null)
            {
                response.Success = true;
                response.Message = "Your seats were successfully reserved.";

                response.OrderID = order.ID;
                response.Seats = new List<Responses.ReservationSeat>();

                foreach (OrderSeat seat in order.OrderSeats)
                {
                    Responses.ReservationSeat newSeat = new Responses.ReservationSeat();
                    newSeat.RowNumber = seat.Seat.Row.RowNumber;
                    newSeat.SeatNumber = seat.Seat.SeatNumber;

                    response.Seats.Add(newSeat);
                }
            }
            else
            {
                response = Responses.ReservationResponse.ReservationCriteriaNotMet;
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
                    .Where(w => w.ID.Equals(eventID))
                    .FirstOrDefault()
                    .Venue
                    .Rows
                    .ToList();

                int[] takenSeats = db.OrderSeats
                    .Where(w => w.Order.EventID.Equals(eventID))
                    .Select(s => s.SeatID)
                    .ToArray();

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

                    if (seatsNeeded == 0)
                    {
                        break;
                    }
                    else if (rowsRemaining == 0)
                    {
                        seatsNeeded += this.RemoveNearestRow(ref selectedSeats);
                        rowsRemaining++;
                    }
                    else if (rowsRemaining < 0)
                    {
                        throw new InvalidOperationException("Seats spread across too many rows.");
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

        [MethodImpl(MethodImplOptions.Synchronized)]
        private Order CreateOrder(int customerID, int eventID, List<Seat> seats)
        {
            if (seats.Count > 0)
            {
                Order newOrder = new Order();

                using (var db = new ReservationContext())
                using (DbContextTransaction transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        newOrder.CustomerID = customerID;
                        newOrder.EventID = eventID;

                        db.Orders.Add(newOrder);

                        foreach(Seat seat in seats)
                        {
                            OrderSeat newSeat = new OrderSeat();
                            //newSeat.Order = newOrder;
                            newSeat.OrderID = newOrder.ID;
                            //newSeat.Seat = seat;
                            newSeat.SeatID = seat.ID;
                            
                            newOrder.OrderSeats.Add(newSeat);
                            db.OrderSeats.Add(newSeat);
                        }

                        db.SaveChanges();
                        transaction.Commit();

                        newOrder = db.Orders
                            .Include(i => i.OrderSeats.Select(s => s.Seat.Row))
                            .Where(w => w.ID.Equals(newOrder.ID))
                            .FirstOrDefault();

                        return newOrder;
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