using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace IhorTheBot
{
    public class ReplyKeyboardMenu
    {

        static readonly string[] mainMenu =  { "REPORT", "STATUS" };
        static readonly string[] submenuReport = { "ADD", "FIX", "REMOVE" };
        static readonly string[] submenuStatus= { "XLS", "MAIL" };
        public static ReplyKeyboardMarkup buildMainMenu(string selected)
        {
            if (string.IsNullOrEmpty(selected))
                return buildmenu(mainMenu);

            switch (selected)
            {
                case "REPORT": return SubMenu1();
                case "STATUS": return SubMenu2();
                 default: return buildmenu(mainMenu);
            }
        }

        public static bool HasItem(string s)
        {
            return !string.IsNullOrEmpty(s)  && (mainMenu.Contains(s) || submenuReport.Contains(s) || submenuStatus.Contains(s));
        }
        private static ReplyKeyboardMarkup buildmenu(string[] src)
        {
            List<KeyboardButton> kbd = new List<KeyboardButton>();
            foreach (var s in src)
            {
                kbd.Add(new KeyboardButton(s));
            }
            return new ReplyKeyboardMarkup(kbd)
            {
                ResizeKeyboard = true
            };
        }

        public static ReplyKeyboardMarkup SubMenu1() => buildmenu(submenuReport);
        public static ReplyKeyboardMarkup SubMenu2() => buildmenu(submenuStatus);

    }
}
