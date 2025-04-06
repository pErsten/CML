using System.ComponentModel.DataAnnotations;
using Common.Data.Dtos;
using Common.Data.Enums;

namespace Common.Data.Entities
{
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
