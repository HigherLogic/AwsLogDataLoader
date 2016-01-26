namespace AwsLogDataLoader
{

    public class Location
    {
        public LocationType Type { get; set; }
        public string Path { get; set; }
        public bool IsSelected { get; set; }
    }

    public enum LocationType
    {
        Unknown = 0,
        File = 1,
        Folder = 2
    }
}
