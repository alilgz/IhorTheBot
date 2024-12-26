using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace IhorTheBot
{
    class Program
    {
        static IConfigurationRoot config = LoadConfig("IhorBot.config");
        static ITelegramBotClient bot = new TelegramBotClient(config["BotKey"]);
        static long mainChatId =  long.Parse(config["Chat"]);
        static long FirstHumanAdmin = 0;
        public enum Message_Status { nothing, Step1, Step2, Step3, done };
        static Dictionary<long, Message_Status> userStatus = new Dictionary<long, Message_Status>();
        static Dictionary<long, List<string>> requests = new Dictionary<long, List<string>>();
        static List<long> ValidUsers = new List<long>();
        private static IConfigurationRoot LoadConfig(string fileName) => new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddXmlFile(fileName).Build();

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));

            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                var userId = message.From.Id;

                if (string.IsNullOrEmpty(message.Text))
                    return;

                if (!ValidUsers.Contains(userId))
                {
                    var valid = await ValidateUser(userId);
                    if (valid)
                    {
                        ValidUsers.Add(message.From.Id);
                    }
                    else return;
                }

                var messageStatus = userStatus.ContainsKey(message.From.Id) ? userStatus[message.From.Id] : Message_Status.nothing;
                bool sendMenu = false;

                switch (messageStatus)
                {
                    case Message_Status.Step1:
                        StartNewRequest(message.From.Id);
                        updateLastRequest(message.From.Id, " ROOM : " + message.Text);
                        await botClient.SendTextMessageAsync(message.Chat, config["Step2Question"]);
                        userStatus[message.From.Id] = Message_Status.Step2;
                        break;
                    case Message_Status.Step2:
                        string room = message.Text;
                        updateLastRequest(message.From.Id, " ITEM : " + message.Text);
                        await botClient.SendTextMessageAsync(message.Chat, config["Step3Question"]); 
                        userStatus[message.From.Id] = Message_Status.Step3;
                        break;
                    case Message_Status.Step3:
                        updateLastRequest(message.From.Id, " DETAILS : " + message.Text);
                        await botClient.SendTextMessageAsync(message.Chat, config["StepsDoneMessage"]); 
                        userStatus[message.From.Id] = Message_Status.nothing;
                        await botClient.SendTextMessageAsync(FirstHumanAdmin, CreateReportMessage());
                        break;
                    default:
                        sendMenu = true;
                        break;
                }
                //check for keyboard menu answers

                if (ReplyKeyboardMenu.HasItem(message.Text))
                {
                    await SendReplyKeyboardMenu(botClient, message.Chat, cancellationToken, message.Text);
                    if (message.Text == "ADD")
                    {
                        StartNewRequest(message.Chat.Id);
                        await botClient.SendTextMessageAsync(message.Chat, config["Step1Question"]);
                        userStatus[message.Chat.Id] = Message_Status.Step1;
                    }

                    return;
                }


                if (message.Text.ToLower() == "/start1")
                {
                    await SendInlineKeybordMenu(botClient, message.Chat, cancellationToken);
                    return;
                }
                if (message.Text.ToLower() == "/start" || message.Text.ToLower() == "/menu")
                {
                    await SendReplyKeyboardMenu(botClient, message.Chat, cancellationToken);
                    return;
                }

                /*if (sendMenu)
                {
                    await SendReplyKeyboardMenu(botClient, message.Chat, cancellationToken);
                }*/
            }

            if (update.Type ==UpdateType.CallbackQuery)
            {

                var cq = update.CallbackQuery;
                //await botClient.DeleteMessageAsync(cq.Message.From.Id, cq.Message.MessageId);
                switch (cq.Data)
                {
                    case "add":
                        StartNewRequest(cq.Message.Chat.Id);
                        await botClient.SendTextMessageAsync(cq.Message.Chat, "Enter room number");
                        userStatus[cq.Message.Chat.Id] = Message_Status.Step1;
                        break;
                    case "xls":
                        await botClient.SendTextMessageAsync(cq.Message.Chat, CreateReportMessage());
                        break;

                    default:
                        await SendInlineKeybordMenu(botClient, cq.Message.Chat, cancellationToken, cq.Data);
                        break;
                }

                //await botClient.SendTextMessageAsync(cq.ChatInstance, "CallbackQuery");
            }
            if (update.Type == UpdateType.ChannelPost)
            {
                var msg = update.ChannelPost;
                Console.WriteLine($"Update config with channel ID: {msg.Chat.Id}");
                // bot didn't answer in channel 
                return;
            }

        }

        private static string CreateReportMessage()
        {
            if (requests == null || requests.Count == 0)
                return "EMPTY";
            StringBuilder response = new StringBuilder();
            foreach (var src in requests.Values)
            {
                response.Append(string.Join("; ", src));
            }
            return response.ToString() ?? "EMPTY";
        }

        private static void updateLastRequest(long userId, string message)
        {
            if (!requests.ContainsKey(userId))
            {
                requests.Add(userId, new List<string>() { message });
            }
            else
            {
                if (requests[userId] == null || requests[userId].Count == 0)
                {
                    requests[userId] = new List<string>() { "" };
                }
                requests[userId][requests[userId].Count - 1] += message;
            }
        }


        private static void StartNewRequest(long userId)
        {
            if (!requests.ContainsKey(userId))
            {
                requests.Add(userId, new List<string>() { "" });
            }
            else
            {
                if (requests[userId] == null || requests[userId].Count == 0)
                {
                    requests[userId] = new List<string>() { "" };
                }
                else
                {
                    requests[userId].Add("");
                }
            }

        }


        public static async Task SendInlineKeybordMenu(ITelegramBotClient botClient, Chat chat, CancellationToken cancellationToken, string selected = "")
        {

            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
              {
                InlineKeyboardMenu.BuildMenu(selected), InlineKeyboardMenu.BuildSubMenu(selected)
            });

            var response = await botClient.SendTextMessageAsync(chat.Id, text: "TEST MENU", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);

        }



        static async Task<Message> SendReplyKeyboardMenu(ITelegramBotClient botClient, Chat chat, CancellationToken cancellationToken, string selected = "")
        {

            return await botClient.SendTextMessageAsync(
                chatId: chat.Id,
                text: "Choose",
                replyMarkup: ReplyKeyboardMenu.buildMainMenu(selected),
                cancellationToken: cancellationToken);
        }


        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("!ERROR!");
            Console.ForegroundColor = oldColor;
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        public async static Task<bool> ValidateUser(long userId)   
        {
            var user = await bot.GetChatMemberAsync(mainChatId, userId);
            if (user.Status != null && user.User!=null)
            {
                Console.WriteLine($"{user.User.FirstName}\t{user.Status}");
            }
            return user.Status == ChatMemberStatus.Member || user.Status == ChatMemberStatus.Administrator || user.Status == ChatMemberStatus.Creator;
        }

        static async Task Main(string[] args)
        {
            //CheckConfiguration

            Console.WriteLine("Ihor started" + bot.GetMeAsync().Result.FirstName);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.CallbackQuery, UpdateType.Message, UpdateType.ChannelPost }, // receive all update types
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );

            //load admins 

            if (true)
            {
                try
                {
                    // get verified users

                    Chat channel = await bot.GetChatAsync(mainChatId);

                    // Check if the channel is a channel type
                    if (channel.Type == ChatType.Channel)
                    {
                        // Get the list of users in the channel
                        ChatMember[] users = await bot.GetChatAdministratorsAsync(mainChatId);
                        foreach (var user in users)
                        {
                            Console.WriteLine($"User: Bot: {user.User.IsBot} | ID:{user.User.Id} | Name:{user.User.Username}");
                            if (!user.User.IsBot && FirstHumanAdmin == 0)
                            {
                                FirstHumanAdmin = user.User.Id;
                            }
                        }

                        /*var me = await bot.GetMeAsync();
                        ChatMember[] users2 = await bot.GetChatMemberAsync(chatId, me.Id);
                        foreach (var user in users2)
                        {
                            Console.WriteLine($"User: Bot: {user.User.IsBot} | ID:{user.User.Id} | Name:{user.User.Username}");
                        }
                        */

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

                Console.ReadLine();
        }
    }

}
