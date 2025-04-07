using System.ComponentModel.DataAnnotations;
using Common.Data.Enums;
using Common.Data.Models;

namespace Common.Data.Entities
{
    /// <summary>
    /// Represents an application-level event with a timestamp, type, and serialized event-specific data.
    /// </summary>
    public class AppEvent
    {
        [Key]
        public int Id { get; set; }
        public DateTime UtcCreated { get; set; }
        public EventTypeEnum EventType { get; set; }
        public string EventJsonData { get; set; }

        public AppEvent() { }

        public AppEvent(EventDto dto, string eventJsonData)
        {
            UtcCreated = dto.UtcCreated;
            EventType = dto.EventType;
            EventJsonData = eventJsonData;
        }
    }
}
