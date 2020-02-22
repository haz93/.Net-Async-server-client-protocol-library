using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MySocketLibrary
{
    public class Server
    {
        private int Port;

        //private int pingeveryms;
       private int timeoutms;

        private List<ClientData> clientlist = new List<ClientData>();
        private TcpListener listener;



        public delegate void voidwithparams(System.Net.EndPoint fromclient,byte[] data, int length);
        public delegate void endpointnoparams(System.Net.EndPoint remote_end_point);

        private endpointnoparams Connected;
        private endpointnoparams Disconnected;
        private voidwithparams datareceived;

      public Server(int port, endpointnoparams onConnected, endpointnoparams onDisconnect, voidwithparams onDatareceived, int timeoutinms)
        {
          //  pingeveryms = pingeveryinms;
            timeoutms = timeoutinms;
            Port = port;
            Connected = onConnected;
            Disconnected = onDisconnect;
            datareceived = onDatareceived;
        }

        private void CallConnected(System.Net.EndPoint from)
        {
            if (Connected != null)
            {
                Task task = new Task(() => Connected(from));
                task.Start();
            }
        
        }
        private void CallDisconnected(System.Net.EndPoint from)
        {
            if (Disconnected != null)
            {
                Task task = new Task(() => Disconnected(from));
                task.Start();
            }
        }
        private void Calldatareceived(System.Net.EndPoint from, byte[] data, int n)
        {

            if (datareceived != null)
            {
                Task task = new Task(() => datareceived(from, data, n));
                task.Start();
            }
        }

        public void Listen()
        {
            listener = new TcpListener(IPAddress.Any, 666);
            listener.Start();
            if (timeoutms != 0)
            {
                TimerCallback tcb = pingloop;
                Timer t = new Timer(tcb);
                t.Change(1000, 1000);
            }
            
            listener.BeginAcceptTcpClient(HandleAsyncConnection, listener);
        }


        private void HandleAsyncConnection(IAsyncResult res)
        {
            listener.BeginAcceptTcpClient(HandleAsyncConnection, listener);
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client = listener.EndAcceptSocket(res);

            ClientData newclient = new ClientData();

            newclient.s = client;
            newclient.timeout = new System.Diagnostics.Stopwatch();
            newclient.recvbuffer = new byte[1024];
         // newclient.pinged = false;
            clientlist.Add(newclient);
            newclient.timeout.Start();

            CallConnected(newclient.s.RemoteEndPoint);

            try
            {
                client.BeginReceive(newclient.recvbuffer, 0, newclient.recvbuffer.Length, SocketFlags.None, new AsyncCallback(Recieve), newclient);
       
            }
            catch
            {
                handledisconnect(newclient);
            }

        }

        private void pingloop(object stateinfo)
        { 
      
            for(int i = 0; i < clientlist.Count; i++)
            {
                ClientData client = clientlist[i];
                if (client.timeout.ElapsedMilliseconds > timeoutms)
                {
                    handledisconnect(client);
               }

                //if (client.timeout.ElapsedMilliseconds > pingeveryms && client.timeout.ElapsedMilliseconds < timeoutms && client.pinged == false)
                //{
                //    //Ping(client);
                //    client.pinged = true;
                //}
                //else if(client.timeout.ElapsedMilliseconds > timeoutms && client.pinged == true)
                //{
                //    handledisconnect(client);

                //}
        
          }
        
        }



        private void Recieve(IAsyncResult iar)
        {
            ClientData newclient = (ClientData)iar.AsyncState;


            if (!newclient.s.Connected)
            {
                handledisconnect(newclient);
                return;
            }

            int n;

            try
            {
                n = newclient.s.EndReceive(iar);
            }
            catch
            {
                n = 0;
                handledisconnect(newclient);
                return;
            }


            if (n == 0)
            {
                newclient.recvbuffer = new byte[1024];
                newclient.s.BeginReceive(newclient.recvbuffer, 0, newclient.recvbuffer.Length, SocketFlags.None, new AsyncCallback(Recieve),newclient);
                return;
            }
            else
            {
                newclient.timeout.Restart();

                for (int i = 0; i < n; i++)
                {
                    newclient.byteque.Enqueue(newclient.recvbuffer[i]);
                }
            }

           // var t = new Thread(() => parse(newclient));
           // t.Start();
            parse(newclient);

            newclient.recvbuffer = new byte[1024];
            newclient.s.BeginReceive(newclient.recvbuffer, 0, newclient.recvbuffer.Length, SocketFlags.None, new AsyncCallback(Recieve), newclient);

        }




        private void parse(ClientData theclient)
        {
            //Handle Exceptions
          
                ClientData cls = theclient;
     

                if (cls.dataphase)
                {


                    if (((cls.datasize - cls.byteque.Count) == 0))
                    {
                        cls.dataphase = false;

                        byte[] payload = new byte[cls.datasize];

                        for (int i = 0; i < cls.datasize; i++)
                        {
                            payload[i] = cls.byteque.Dequeue();
                        }

                        int n = cls.datasize;
                        Calldatareceived(theclient.s.RemoteEndPoint, payload, n);


                    }
                    else if (((cls.datasize - cls.byteque.Count) < 0))
                    {
                        cls.dataphase = false;

                        byte[] payload = new byte[cls.datasize];

                        for (int i = 0; i < cls.datasize; i++)
                        {
                            payload[i] = cls.byteque.Dequeue();
                        }

                        int n = cls.datasize;
                        Calldatareceived(theclient.s.RemoteEndPoint,payload, n);
                        parse(cls);

                    }
                    else
                    {
                        cls.dataphase = true; //useless
                    }

                }
                else
                {
                    if (cls.byteque.Count >= 4)
                    {
                        cls.dataphase = true;
                        byte[] sizeinbytes = new byte[4];
                        for (int i = 0; i < 4; i++)
                        {
                            sizeinbytes[i] = cls.byteque.Dequeue();
                        }
                        int size = BitConverter.ToInt32(sizeinbytes, 0);

                        if (size == 0)
                        {
                            cls.dataphase = false;
                            cls.timeout.Restart();
                        //    if (cls.pinged == true)
                          //  {
                            //    cls.pinged = false;
                           // }
                           byte[] empty = new byte[1];
                           empty[0] = 0x00;
                           Calldatareceived(theclient.s.RemoteEndPoint, empty, 0);

                            if (cls.byteque.Count > 0)
                            {
                                parse(cls);

                            }
                            else
                            {
                                return;
                            }

                        }

                        if (cls.byteque.Count == size)
                        {
                            cls.dataphase = false;

                            byte[] payload = new byte[size];

                            for (int i = 0; i < size; i++)
                            {
                                payload[i] = cls.byteque.Dequeue();
                            }
                            int n = size;
                            Calldatareceived(theclient.s.RemoteEndPoint, payload, n);
                        }
                        else if (cls.byteque.Count > size)
                        {
                            cls.dataphase = false;

                            byte[] payload = new byte[size];
                            for (int i = 0; i < size; i++)
                            {
                                payload[i] = cls.byteque.Dequeue();
                            }

                            int n = size;
                            Calldatareceived(theclient.s.RemoteEndPoint, payload, n);
                            parse(cls);

                        }
                        else
                        {
                            cls.datasize = size;
                            cls.dataphase = true; //useless

                        }

                    }




                }



        }




        private void handledisconnect(ClientData disconnectedclient)
        {
            lock (disconnectedclient.disconnectlock)
            {
                if (disconnectedclient == null)
                {
                    return;
                }
                try
                {
                    disconnectedclient.s.Shutdown(SocketShutdown.Both);
                    disconnectedclient.s.Close();
                    clientlist.Remove(disconnectedclient);
                    CallDisconnected(disconnectedclient.s.RemoteEndPoint);

                }
                catch (ObjectDisposedException e)
                {
                    return;
                }
            }
            
        }

        //private void Ping(ClientData theclient)
        //{
        //    lock (theclient.sendlock)
        //    {
        //        byte[] ping = new byte[4] { 0x00, 0x00, 0x00, 0x00 };
        //        theclient.s.Send(ping);
            
        //    }

        //}

        public void SendTo(System.Net.EndPoint toclient, byte[] data)
        { 
        foreach(ClientData client in clientlist)
        {
            if(client.s.RemoteEndPoint == toclient)
            {
                lock(client.sendlock)
                {
                    int size = data.Length;
                    byte[] sizebytes = new byte[4];
                    sizebytes = BitConverter.GetBytes(size);
                    client.s.Send(sizebytes);
                    client.s.Send(data);
                }
            }

        }
        
        }

        public void ForceDisconnect(System.Net.EndPoint clienttodisconnect)
        { 
        foreach(ClientData client in clientlist)
        {
         if(clienttodisconnect == client.s.RemoteEndPoint)
         {
             handledisconnect(client);

         }
        
        }
        
        }










    }
}
