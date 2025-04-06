using Common.Data.Enums;

namespace Common.Data.Models
{
    public class EventDto
    {
        public EventTypeEnum EventType { get; set; }
        public object EventBody { get; set; }
        public DateTime UtcCreated { get; set; }

        public EventDto() { }

        public EventDto(EventTypeEnum eventType, DateTime utcCreated, object eventBody)
        {
            EventType = eventType;
            UtcCreated = utcCreated;
            EventBody = eventBody;
        }
    }
}
