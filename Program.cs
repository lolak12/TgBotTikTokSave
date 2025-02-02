// See https://aka.ms/new-console-template for more information

using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace tiktoktg
{
    class Program
    {
        private static readonly string BotToken = "7599612629:AAF9qzvCZdXvT5SEQ8aakrKp0zQa_jdXQGI";
        private static ITelegramBotClient botClient = new TelegramBotClient(BotToken);
        private static HttpClient httpClient = new HttpClient();

        static async Task Main()
        {
            Console.WriteLine("Бот запускается...");

            using var cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // Получать все обновления
            };

            // Запуск бота в режиме получения обновлений (polling)
            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await botClient.GetMeAsync();
            Console.WriteLine($"Бот запущен: {me.FirstName}");
            Console.ReadLine();

            cts.Cancel();
        }

        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message || message.Text is not { } text)
                return;

            long chatId = message.Chat.Id;

            if (text.StartsWith("/start"))
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Привет! Отправь мне ссылку на видео из TikTok, и я попробую его скачать.",
                    cancellationToken: cancellationToken
                );
            }
            else if (text.Contains("tiktok.com"))
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Обрабатываю ссылку... Подождите немного.",
                    cancellationToken: cancellationToken
                );

                string videoUrl = await GetTikTokVideoUrl(text);
                if (!string.IsNullOrEmpty(videoUrl))
                {
                    await botClient.SendVideoAsync(
                        chatId: chatId,
                        video: InputFile.FromUri(videoUrl),
                        caption: "Вот ваше видео 🎥",
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "Не удалось загрузить видео. Попробуйте другую ссылку.",
                        cancellationToken: cancellationToken
                    );
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Пожалуйста, отправьте ссылку на TikTok.",
                    cancellationToken: cancellationToken
                );
            }
        }

        static async Task<string> GetTikTokVideoUrl(string tiktokUrl)
        {
            try
            {
                string apiUrl = $"https://www.tikwm.com/api/?url={tiktokUrl}";
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                string responseBody = await response.Content.ReadAsStringAsync();

                Match match = Regex.Match(responseBody, @"""play"":""(https:[^""]+)""");
                return match.Success ? match.Groups[1].Value.Replace("\\/", "/") : null;
            }
            catch
            {
                return null;
            }
        }

        static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}


