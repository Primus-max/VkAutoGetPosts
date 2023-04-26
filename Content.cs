using System.Collections.Generic;

public class Content
{
    public long MessageId { get; set; }
    public string? Text { get; set; }
    public List<string>? ImagePaths { get; set; }
    public bool ImageViewed { get; set; } = false;
    public long? MediaAlbumId { get; set; } = 0;
    public bool? IsForPosting { get; set; } = false;
}