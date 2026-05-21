namespace ResQ.API.DTOs.Reviews;

public class ReviewResponse
{
    public int Id { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
