using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.TripDay
{
    public class TripDayResponse
    {
        public Guid Id { get; set; }
        public Guid TripId { get; set; }
        public string? Title { get; set; }
        public DateTime DayDate { get; set; }
        public int DateIndex { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
