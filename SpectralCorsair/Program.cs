using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;

namespace SpectralCorsair
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = null;
            string text = null;

            if (args is null || args.Length < 2)
            {
                throw new ArgumentNullException("path", "Please define path to the image and text.");
            }

            path = args[0];
            text = args[1];

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path), "Path to file must not be empty.");
            }

            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException(nameof(path), "Text must not be empty.");
            }

            var font = SystemFonts.CreateFont("Arial", 26);
            var options = new TextGraphicsOptions(true)
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            using (Image image = Image.Load(path))
            {
                var position = new PointF(image.Width / 2, image.Height - 30);

                image.Mutate(x => x
                     .DrawText(options, text, font, Color.Black, position)
                     //.Resize(image.Width / 2, image.Height / 2)
                     //.Grayscale()
                     );

                image.Save("out.jpg"); // Automatic encoder selected based on extension.
            }
        }
    }
}
