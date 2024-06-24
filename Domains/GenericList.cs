namespace MyApplication.Domain.Models
{
    public class KeyValueItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class KeyValueList
    {
        public string Type { get; set; }
        public List<KeyValueItem> Items { get; set; }
    }
}
