using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;

namespace MySocketLibrary
{
    public class Client
    {
        private int Port;
        private string Ip;
        private string hostname;

        private delegate void voidwithparams(byte[] data, int length);
        private delegate void voidnoparams();
        private delegate void endpointnoparams(System.Net.EndPoint remote_end_point);

        private ClientData client;

        private bool stopsignal = false;

        private endpointnoparams Connected;
        private voidnoparams Disconnected;
        private voidwithparams datareceived;


        Client(string ip, int port, endpointnoparams onConnected, voidnoparams onDisconnect, voidwithparams onDatareceived)
        { 
            Port = port;
            Ip = ip;
            Connected = onConnected;
            Disconnected = onDisconnect;
            datareceived = onDatareceived;
        }

        private void CallDisconnected()
        {
            if (Disconnected != null)
            {
                Task task = new Task(() => Disconnected());
                task.Start();
            }
        }

        private void CallConnected()
        {
            if (Connected != null)
            {
                Task task = new Task(() => Connected(client.s.RemoteEndPoint));
                task.Start();
            }
        }

        private void CallDatareceived(byte[] data, int length)
        { 
           if(datareceived != null)
           {
               Task task = new Task(() => datareceived(data, length));
               task.Start();
           }
        
        }



        public void Connect()
        {
             client = new ClientData();
             client.s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
             client.s.ReceiveTimeout = 30000;
             client.s.Connect(Ip, Port);
         

             if (client.s.Connected == true)
             {
                 CallConnected();
                 Thread t = new Thread(listenloop);
                 t.Start();

             //Connected(client.s.RemoteEndPoint);
            // listenloop();
             }
             else
             {
               return;
             }

        }
        
        private void listenloop()
        {
            while (client.s.Connected && !stopsignal)
            {
                client.recvbuffer = new byte[8192];
                int n;
                try
                {
                    n = client.s.Receive(client.recvbuffer);
               }
                catch
                {
                    stopsignal = true;
                    n = 0;
                    CallDisconnected();
                    return;
                }

                for (int i = 0; i < n; i++)
                {
                    client.byteque.Enqueue(client.recvbuffer[i]);
                }
                parse();

            }
            disconnect();

        }


        private void parse()
        {
              if (client.byteque.Count == 0)
               { return; }

                if (client.dataphase)
                {
                    if (((client.datasize - client.byteque.Count) == 0))
                    {
                        client.dataphase = false;

                        byte[] payload = new byte[client.datasize];

                        for (int i = 0; i < client.datasize; i++)
                        {
                            payload[i] = client.byteque.Dequeue();
                        }
                        int n = client.datasize;
                        CallDatareceived(payload, n);

                    }
                    else if (((client.datasize - client.byteque.Count) < 0))
                    {
                        client.dataphase = false;

                        byte[] payload = new byte[client.datasize];

                        for (int i = 0; i < client.datasize; i++)
                        {
                            payload[i] = client.byteque.Dequeue();
                        }
                        int n = client.datasize;
                        CallDatareceived(payload, n);
                        parse();
                    }
                    else
                    {
                        client.dataphase = true; //useless
                    }
                }
                else
                {
                    if (client.byteque.Count >= 4)
                    {
                        client.dataphase = true;
                        byte[] sizeinbytes = new byte[4];
                        for (int i = 0; i < 4; i++)
                        {
                            sizeinbytes[i] = client.byteque.Dequeue();
                        }
                        int size = BitConverter.ToInt32(sizeinbytes, 0);

                        if (size == 0)
                        {
                            client.dataphase = false;
                            byte[] empty = new byte[1];
                            empty[0] = 0x00;
                            CallDatareceived(empty, 0);
                            if (client.byteque.Count > 0)
                            {
                                parse();
                            }
                            else
                            {
                                return;
                            }

                        }

                        if (client.byteque.Count == size)
                        {
                            client.dataphase = false;

                            byte[] payload = new byte[size];

                            for (int i = 0; i < size; i++)
                            {
                                payload[i] = client.byteque.Dequeue();
                            }
                            int n = size;
                            CallDatareceived(payload, n);
                        }
                        else if (client.byteque.Count > size)
                        {
                            client.dataphase = false;

                            byte[] payload = new byte[size];
                            for (int i = 0; i < size; i++)
                            {
                                payload[i] = client.byteque.Dequeue();
                            }
                            int n = size;
                            CallDatareceived(payload, n);
                            parse();
                        }
                        else
                        {
                            client.datasize = size;
                            client.dataphase = true; //useless

                        }


                    }

                }

            

        }



        public void Send(byte[] data)
        {
            lock (client.sendlock)
            {
                if (client.s.Connected)
                {
                    int n = data.Length;
                    byte[] sizeinbytes = BitConverter.GetBytes(n);
                    client.s.Send(sizeinbytes);
                    client.s.Send(data);
                }

            }
        }

        public void disconnect()
        {
            CallDisconnected();
            lock (client.disconnectlock)
            {
                client.s.Disconnect(true);
            }
 
        }


    }
}
