using SkiaSharp;

namespace FileManagerLite
{
    public static class FileManipulation
    {
        public static byte[] CropImage(string filePath, int width = 0, int height = 0)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Destination size must be positive");

            using (var originalBitmap = SKBitmap.Decode(filePath))
            {
                if (originalBitmap == null)
                    throw new InvalidOperationException("Failed to decode the source image");

                // Calculate the center crop coordinates
                int cropWidth = Math.Min(width, originalBitmap.Width);
                int cropHeight = Math.Min(height, originalBitmap.Height);

                int left = (originalBitmap.Width - cropWidth) / 2;
                int top = (originalBitmap.Height - cropHeight) / 2;

                var cropRectangle = new SKRect(left, top, left + cropWidth, top + cropHeight);

                // Create and draw the cropped image
                using (var croppedBitmap = new SKBitmap(cropWidth, cropHeight))
                using (var canvas = new SKCanvas(croppedBitmap))
                {
                    canvas.DrawBitmap(
                        originalBitmap,
                        cropRectangle,  // Source rectangle (from original image)
                        new SKRect(0, 0, cropWidth, cropHeight)  // Destination rectangle (in cropped image)
                    );

                    // Determine the output format
                    var extension = Path.GetExtension(filePath).ToLowerInvariant();
                    var imageFormat = extension switch
                    {
                        ".png" => SKEncodedImageFormat.Webp,
                        ".jpeg" or ".jpg" => SKEncodedImageFormat.Webp,
                        ".webp" => SKEncodedImageFormat.Webp,
                        _ => throw new NotSupportedException($"Unsupported file extension: {extension}")
                    };

                    // Encode and save to the output stream
                    using (var image = SKImage.FromBitmap(croppedBitmap))
                    using (var encodedData = image.Encode(imageFormat, 75))
                    {
                        if (encodedData == null)
                            throw new InvalidOperationException("Failed to encode the cropped image");

                        return encodedData.ToArray();
                    }
                }
            }
        }

        public static byte[] StretchImage(string filePath, int width = 0, int height = 0)
        {
            var fileName = Path.GetExtension(filePath);
            using var original = SKBitmap.Decode(filePath);

            if (original == null) return null;

            using var resized = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(resized))
            {
                var destRect = new SKRect(0, 0, width, height);
                canvas.DrawBitmap(original, destRect);
            }

            using var image = SKImage.FromBitmap(resized);
            string extension = Path.GetExtension(filePath);
            SKData sKData = extension.ToLower() switch
            {
                ".png" => image.Encode(SKEncodedImageFormat.Png, 75),
                ".jpeg" => image.Encode(SKEncodedImageFormat.Jpeg, 75),
                ".jpg" => image.Encode(SKEncodedImageFormat.Jpeg, 75),
                ".webp" => image.Encode(SKEncodedImageFormat.Webp, 75),
                _ => throw new NotSupportedException("Unsupported file extension")
            };

            return sKData.ToArray();

        }


    }
}
