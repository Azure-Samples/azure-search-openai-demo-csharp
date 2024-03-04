namespace Shared.Models;

public record class ImageResponse(
    DateTimeOffset Created,
    List<Uri> ImageUrls);
