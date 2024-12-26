using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace IhorTheBot
{
    public class InlineKeyboardMenu
    {


        public static InlineKeyboardButton[] BuildMenu(string selected = "")
        {
            List<InlineKeyboardButton> menu = new List<InlineKeyboardButton>();
            menu.Add(InlineKeyboardButton.WithCallbackData(text: selected == "new" ? ">REPORT<" : "REPORT", callbackData: "new"));
            menu.Add(InlineKeyboardButton.WithCallbackData(text: selected == "status" ? ">STATUS<" : "STATUS", callbackData: "status"));
            return menu.ToArray();
        }

        public static InlineKeyboardButton[] BuildSubMenu(string selected = "")
        {
            List<InlineKeyboardButton> menu = new List<InlineKeyboardButton>();


            switch (selected)
            {
                case "new":
                case "add":
                case "fix":
                case "remove":
                    menu.Add(InlineKeyboardButton.WithCallbackData(text: selected == "add" ? ">ADD<" : "ADD", callbackData: "add"));
                    menu.Add(InlineKeyboardButton.WithCallbackData(text: selected == "fix" ? ">FIX<" : "FIX", callbackData: "fix"));
                    menu.Add(InlineKeyboardButton.WithCallbackData(text: selected == "remove" ? ">REMOVE<" : "REMOVE", callbackData: "remove"));
                    break;

                case "status":
                case "mail":
                case "xls":
                    menu.Add(InlineKeyboardButton.WithCallbackData(text: selected == "xls" ? ">XLS<" : "XLS", callbackData: "xls"));
                    menu.Add(InlineKeyboardButton.WithCallbackData(text: selected == "mail" ? ">MAIL<" : "MAIL", callbackData: "mail"));
                    break;
            }


            return menu.ToArray();
        }
    }
}
