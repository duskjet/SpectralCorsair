using Microsoft.Extensions.Configuration;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
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
        private const string AccessTokenKey = "Telegram:AccessToken";
        static TelegramBotClient bot;

        static async Task Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddCommandLine(args, new Dictionary<string, string> { 
                    ["-t"] = AccessTokenKey, 
                    ["--access-token"] = AccessTokenKey 
                })
                .AddUserSecrets<Program>()
                .Build();

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true; // prevent the process from terminating.
                cts.Cancel();
            };

            string access_token = config.GetValue<string>(AccessTokenKey);
            if (string.IsNullOrEmpty(access_token))
            {
                throw new InvalidOperationException("Telegram access token must be set.");
            }

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

            using (MemoryStream stream = new MemoryStream())
            {
                var img = await bot.GetInfoAndDownloadFileAsync(photos.Last().FileId, stream);
                stream.Position = 0;

                using (var output = await DrawText(stream, caption))
                {
                    await bot.SendPhotoAsync(e.Message.Chat.Id, new Telegram.Bot.Types.InputFiles.InputOnlineFile(output));
                }
            }
        }

        private static async Task<Stream> DrawText(Stream stream, string text)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (String.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException(nameof(text));
            }

            var font = SystemFonts.CreateFont("Arial", 32);
            var options = new TextGraphicsOptions(true)
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            MemoryStream output = new MemoryStream();
            using (Image image = Image.Load(stream, new JpegDecoder()))
            {
                var position = new PointF(image.Width / 2, image.Height - 30);

                image.Mutate(x => x.DrawText(options, text, font, Brushes.Solid(Rgba32.White), Pens.Solid(Rgba32.Black, 1), position));

                image.Save(output, new JpegEncoder());
                output.Position = 0;
            }

            return output;
        }
    }
}
