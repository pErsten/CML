using System.ComponentModel.DataAnnotations;

namespace Common.Data.Entities
{
    public class AppEvent
    {
        [Key]
        public int Id { get; set; }
        public DateTime UtcCreated { get; set; }
        public string EventJsonData { get; set; }

        public AppEvent() { }

        public AppEvent(DateTime utcCreated, string eventJsonData)
        {
            UtcCreated = utcCreated;
            EventJsonData = eventJsonData;
        }
    }
}
