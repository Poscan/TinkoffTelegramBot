using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using telegramBot.Services;

namespace telegramBot
{
    class Program
    {
        private static TinkoffService _tinkoffService;
        private static Dictionary<long, string> _userTokenDictionary;

        static void Main(string[] args)
        {
            _userTokenDictionary = new Dictionary<long, string>();
            var bot = new TelegramBotClient("1967725772:AAE-kXZm5clBSJz7Fo5tQI_XGwqHIsCXG9s");

            using var cancellationTokenSource = new CancellationTokenSource();

            bot.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, ErrorHandler), cancellationTokenSource.Token);

            Console.WriteLine($"Start listening");
            Console.ReadLine();

            cancellationTokenSource.Cancel();
        }

        private static Task ErrorHandler(ITelegramBotClient telegramBot, Exception exception, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                Console.Write("Возникли проблемы" + exception.Message + "\n");
            }
        }

        private static async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            if (_userTokenDictionary.ContainsKey(message.From.Id) == false)
            {
                _tinkoffService = new TinkoffService(message.Text);

                if (await _tinkoffService.ValidateTokenAsync(message.Text))
                {
                    _userTokenDictionary.Add(message.From.Id, message.Text);
                    await TokenValid(botClient, message);
                    await SendReplyKeyboard(botClient, message);
                    return;
                }
                else
                {
                    await TokenNotValid(botClient, message);
                    return;
                }
            }

            _tinkoffService = new TinkoffService(_userTokenDictionary[message.From.Id]);

            Console.WriteLine($"Receive message type: {message.Type}");
            if (message.Type != MessageType.Text)
                return;

            var action = (message.Text.Split(' ').First()) switch
            {
                "/keyboard" => SendReplyKeyboard(botClient, message),
                "/remove" => RemoveKeyboard(botClient, message),
                "Акции" => GetStocks(botClient, message),
                "Баланс" => GetCurrencies(botClient, message),
                "Облигации" => GetBonds(botClient, message),
                _ => Usage(botClient, message)
            };
            var sentMessage = await action;
            Console.WriteLine($"The message was sent with id: {sentMessage.MessageId}");

            static async Task<Message> SendReplyKeyboard(ITelegramBotClient botClient, Message message)
            {
                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                    new KeyboardButton[][]
                    {
                        new KeyboardButton[] { "Акции", "Баланс" },
                        new KeyboardButton[] { "Облигации" },
                    })
                {
                    ResizeKeyboard = true
                };

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                            text: "Choose",
                                                            replyMarkup: replyKeyboardMarkup);
            }

            static async Task<Message> TokenNotValid(ITelegramBotClient botClient, Message message)
            {
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                    text: "Введите токен");
            }

            static async Task<Message> TokenValid(ITelegramBotClient botClient, Message message)
            {
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                    text: "Токен валидный");
            }

            static async Task<Message> RemoveKeyboard(ITelegramBotClient botClient, Message message)
            {
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                            text: "Removing keyboard",
                                                            replyMarkup: new ReplyKeyboardRemove());
            }

            static async Task<Message> GetStocks(ITelegramBotClient botClient, Message message)
            {
                var stocksString = "";
                await foreach (var stock in _tinkoffService.GetStocksAsync())
                {
                    stocksString += stock.Name + ": " + stock.CurrentPrice + "\n";
                }

                if (string.IsNullOrWhiteSpace(stocksString)) stocksString = "В вашем портфеле нет акций";

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                            text: stocksString);
            }

            static async Task<Message> GetBonds(ITelegramBotClient botClient, Message message)
            {
                var bondsString = "";

                await foreach (var bond in _tinkoffService.GetBondsAsync())
                {
                    bondsString += bond.Name + ": " + bond.CurrentPrice + "\n";
                }

                if (string.IsNullOrWhiteSpace(bondsString)) bondsString = "В вашем портфеле нет облигаций";

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                            text: bondsString);
            }

            static async Task<Message> GetCurrencies(ITelegramBotClient botClient, Message message)
            {
                var stocksString = "";
                await foreach (var stock in _tinkoffService.GetCurrencies())
                {
                    stocksString += GetCurrency(stock.Currency) + ": " + stock.Balance + "\n";
                }

                if (string.IsNullOrWhiteSpace(stocksString)) stocksString = "В вашем портфеле нет валюты";

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                    text: stocksString);
            }

            static string GetCurrency(int currencyEnum)
            {
                switch (currencyEnum)
                {
                    case 0:
                        return "RUB";
                    case 1:
                        return "USD";
                    case 2:
                        return "EUR";
                    case 3:
                        return "GBP";
                    case 4:
                        return "HKD";
                    case 5:
                        return "CHF";
                    case 6:
                        return "JPY";
                    case 7:
                        return "CNY";
                    case 8:
                        return "TRY";
                    default:
                        return "Незнакомая валюта";
                }
            }

            static async Task<Message> Usage(ITelegramBotClient botClient, Message message)
            {


                const string usage = "Usage:\n" +
                                     "/keyboard - send custom keyboard\n" +
                                     "/remove   - remove custom keyboard\n";

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                            text: usage,
                                                            replyMarkup: new ReplyKeyboardRemove());
            }
        }

        private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }
    }
}
