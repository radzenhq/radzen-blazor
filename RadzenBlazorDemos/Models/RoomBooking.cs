using System;
using System.Collections.Generic;

namespace RadzenBlazorDemos
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Staff
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class RoomBooking
    {
        public IList<int> RoomIds { get; set; } = new List<int>();
        public int StaffId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool AllDay { get; set; }
        public string Text { get; set; }
    }
}
