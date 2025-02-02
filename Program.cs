// See https://aka.ms/new-console-template for more information
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
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

            // Создаем объект, реализующий IUpdateHandler
            var handler = new UpdateHandler();

            // Запуск бота в режиме получения обновлений (polling)
            botClient.StartReceiving(
                updateHandler: handler,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await botClient.GetMeAsync();
            Console.WriteLine($"Бот запущен: {me.FirstName}");
            Console.ReadLine();

            cts.Cancel();
        }
    }

    // Реализуем IUpdateHandler
    class UpdateHandler : IUpdateHandler
    {
        private static HttpClient httpClient = new HttpClient();

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
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
                            video: new InputOnlineFile(videoUrl), // Используем InputOnlineFile
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
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке обновления: {ex.Message}");
            }
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }

        // Метод для получения URL видео
        private async Task<string> GetTikTokVideoUrl(string tiktokUrl)
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

        // Реализация обязательного метода для обработки ошибок в процессе polling
        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка при polling: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}







