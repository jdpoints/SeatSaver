using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web;
using SeatSaver.Models;
using System.Runtime.CompilerServices;

namespace SeatSaver.Components
{
    public class SeatSelector : ISeatSelector
    {
        /// <summary>
        /// Reserves seats for an event and prepares a ReservationResponse to return to the requestor
        /// </summary>
        /// <param name="customerID">ID of the Customer placing the order</param>
        /// <param name="eventID">ID of the event to request seats for</param>
        /// <param name="numberOfSeats">Total number of seats needed</param>
        /// <param name="maxRows">Maximum number of rows seats can be split between</param>
        /// <returns>Returns a ReservationResponse to the calling controller</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public Responses.ReservationResponse ReserveBestSeats(int customerID, int eventID, int numberOfSeats, int maxRows)
        {
            Responses.ReservationResponse response = new Responses.ReservationResponse();

            // Check if valid event and customer
            bool invalidEvent = false;
            bool invalidCustomer = false;
            using (var db = new ReservationContext())
            {
                int eventCount = db.Events.Where(w => w.ID.Equals(eventID)).Count();
                invalidEvent = (eventCount != 1); // Should be exactly 1 event per EventID

                int customerCount = db.Customers.Where(w => w.ID.Equals(customerID)).Count();
                invalidCustomer = (customerCount != 1); // Should be exactly 1 cutomer per CustomerID
            }

            if (invalidEvent)
            {
                return Responses.ReservationResponse.InvalidEventResponse;
            }
            else if (invalidCustomer)
            {
                return Responses.ReservationResponse.InvalidCustomerResponse;
            }


            // Create list of seats that are reserved
            List<Seat> seats = FindSeatsInVenue(numberOfSeats, eventID, maxRows);

            // Create Order object and save in db, will be null if order cannot be saved
            Order order = this.CreateOrder(customerID, eventID, seats);

            // If a valid order is created prepare success ReservationResponse with seat information
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
            // If invalid order then return failure response
            else
            {
                response = Responses.ReservationResponse.ReservationCriteriaNotMetResponse;
            }

            return response;
        }

        #region Private Methods
        /// <summary>
        /// Locate seats in a venue for a given event
        /// </summary>
        /// <param name="seats">Number of seats needed for event</param>
        /// <param name="eventID">ID of the event</param>
        /// <param name="maxRows">Maximum number of rows seats can be split between</param>
        /// <returns>Returns a list of reserved seats or null if no seats are available for the given criteria</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private List<Seat> FindSeatsInVenue(int seats, int eventID, int maxRows)
        {
            List<Seat> selectedSeats = new List<Seat>();

            using (var db = new ReservationContext())
            {
                // Get list of Rows available in the Event Venue
                List<Row> rows = db.Events
                    .Where(w => w.ID.Equals(eventID))
                    .FirstOrDefault()
                    .Venue
                    .Rows
                    .ToList();

                // Get IDs of seats that have already been reserved for this event
                int[] takenSeats = db.OrderSeats
                    .Where(w => w.Order.EventID.Equals(eventID))
                    .Select(s => s.SeatID)
                    .ToArray();

                int seatsNeeded = seats;
                int rowsRemaining = maxRows;

                foreach(Row row in rows)
                {
                    // Get contiguous available seats in row
                    List<Seat> newSeats = this.FindSeatsInRow(seatsNeeded, row, takenSeats);

                    if (newSeats.Count > 0)
                    {
                        seatsNeeded -= newSeats.Count;
                        rowsRemaining--;
                        selectedSeats.AddRange(newSeats);
                    }

                    if (seatsNeeded == 0) // All requested seats have been reserved
                    {
                        break;
                    }
                    else if (rowsRemaining == 0) // Seats have been split between too many rows
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

        /// <summary>
        /// Removes the row closest to the front
        /// </summary>
        /// <param name="selectedSeats">List of seats already selected</param>
        /// <returns>Count of seats that were removed from selectedSeats</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private int RemoveNearestRow(ref List<Seat> selectedSeats)
        {
            // Get ID of row nearest front
            int rowID = selectedSeats
                .Select(s => s.RowID)
                .OrderBy(o => o)
                .First();

            // Get list of seats in nearest row
            List<Seat> dropSeats = selectedSeats
                .Where(w => w.RowID.Equals(rowID))
                .ToList();

            selectedSeats.RemoveAll(r => dropSeats.Contains(r));

            return dropSeats.Count;
        }

        /// <summary>
        /// Locate contiguous seats in given row
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

        /// <summary>
        /// Creates Order and saves to the database
        /// </summary>
        /// <param name="customerID">ID of Customer placing order</param>
        /// <param name="eventID">ID of event the order is for</param>
        /// <param name="seats">List of seats reserved with this order</param>
        /// <returns>Returns Order Entity or null if save fails or seats empty</returns>
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
                            newSeat.OrderID = newOrder.ID;
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
                    catch
                    {
                        transaction.Rollback();
                    }
                }
            }

            return null;
        }
        #endregion
    }
}