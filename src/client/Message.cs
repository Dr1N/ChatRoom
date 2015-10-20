using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyChat
{
    class Message
    {
        public string Time { get; private set; }
        private string login;
        public string Login 
        { 
            get { return login;}
            private set
            {
                this.login = value;
            }
        }
        public string Msg { get; private set; }

        public Message(string time, string user, string message)
        {
            Time = time;
            Login = user;
            Msg = message;
        }

        public override string ToString()
        {
            return String.Format("|{0}|{1}|{2}", Time, Login, Msg);
        }
    }
}