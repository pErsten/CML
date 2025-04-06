using System.Diagnostics;
using Common.Data.Enums;

namespace Common.Data.Dtos
{
    public class EventDto
    {
        public EventTypeEnum EventType { get; set; }
        public object EventBody { get; set; }
        public DateTime UtcCreated { get; set; }
    }
}
