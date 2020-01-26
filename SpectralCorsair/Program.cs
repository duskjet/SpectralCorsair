﻿using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace SpectralCorsair
{
    class Program
    {
        static TelegramBotClient bot;

        static async Task Main(string[] args)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true; // prevent the process from terminating.
                cts.Cancel();
            };

            string access_token = args[0];
            bot = new TelegramBotClient(access_token);

            var me = await bot.GetMeAsync(cts.Token);

            bot.OnMessage += Bot_OnMessage;
            bot.StartReceiving(new[] { UpdateType.Message }, cts.Token);
            Console.WriteLine(
              $"Hello, World! I am user {me.Id} and my name is {me.FirstName}."
            );

            while (true)
            {
                if (cts.IsCancellationRequested)
                    break;
            }
        }

        private static async void Bot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            string caption = e.Message.Caption;
            var photos = e.Message.Photo;
            string pictures = string.Join(", ", e.Message.Photo?.Select(p => p.FileSize.ToString()) ?? new string[0]);
            Console.WriteLine($"New command from '{e.Message.From.Username}': {e.Message.Text}.\nCaption: {caption}. Pictures: {pictures}");

            using (FileStream stream = new FileStream("in.jpg", FileMode.OpenOrCreate))
            {
                var img = await bot.GetInfoAndDownloadFileAsync(photos.Last().FileId, stream);
            }

            await DrawText("in.jpg", caption);
        }

        private static async Task DrawText(string path, string text)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path must not be empty.", nameof(path));
            }

            if (String.IsNullOrEmpty(text))
            {
                throw new ArgumentException("Text must not be empty", nameof(text));
            }

            var font = SystemFonts.CreateFont("Arial", 32);
            var options = new TextGraphicsOptions(true)
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            using (Image image = Image.Load(path))
            {
                var position = new PointF(image.Width / 2, image.Height - 30);

                image.Mutate(x => x
                        .DrawText(options, text, font, Brushes.Solid(Rgba32.Black), Pens.Solid(Rgba32.White, 1), position)
                        //.Resize(image.Width / 2, image.Height / 2)
                        //.Grayscale()
                        );

                image.Save("out.jpg"); // Automatic encoder selected based on extension.
            }
        }

        private static async Task DrawText(Stream stream, string text)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (String.IsNullOrEmpty(text))
            {
                throw new ArgumentException("Text must not be empty", nameof(text));
            }

            var font = SystemFonts.CreateFont("Arial", 26);
            var options = new TextGraphicsOptions(true)
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            using (Image image = Image.Load(stream, new JpegDecoder()))
            {
                var position = new PointF(image.Width / 2, image.Height - 30);

                image.Mutate(x => x
                        .DrawText(options, text, font, Color.Black, position)
                        //.Resize(image.Width / 2, image.Height / 2)
                        //.Grayscale()
                        );

                image.Save("out.jpg", new JpegEncoder()); // Automatic encoder selected based on extension.
            }
        }
    }
}
