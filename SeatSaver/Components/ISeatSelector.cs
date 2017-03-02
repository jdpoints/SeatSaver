using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SeatSaver.Components
{
    interface ISeatSelector
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        Responses.ReservationResponse ReserveBestSeats(int customerID, int eventID, int numberOfSeats, int maxRows);
    }
}
