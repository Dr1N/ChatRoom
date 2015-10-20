using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatServer
{
    class UserState
    {
        private int counter;
        public int Counter
        {
            get 
            {
                return counter;
            }
            set
            {
                Interlocked.Exchange(ref counter, value);
            }
        }
        private TcpClient tcp;
        public TcpClient TcpClient
        {
            get
            {
                return tcp;
            }
        }
        public bool IsOnline { get; set; }
        public ManualResetEvent E { get; private set; }

        public UserState(TcpClient client, int sec)
        {
            this.tcp = client;
            this.counter = sec;
            this.IsOnline = true;
            this.E = new ManualResetEvent(false);
        }

        public void DecrementCounter()
        {
            Interlocked.Decrement(ref counter);
        }

        public void StartWatch()
        {
            this.E.Set();
        }

        public void StopWatch()
        {
            this.E.Reset();
        }

        public void Wait()
        {
            this.E.WaitOne();
        }
    }
}