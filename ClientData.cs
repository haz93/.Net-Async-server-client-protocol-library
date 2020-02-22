using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
namespace MySocketLibrary
{
    public class ClientData
    {

       public  byte[] recvbuffer;
        public Queue<byte> byteque = new Queue<byte>();

        public bool dataphase = false;                //For data included in message
        public int datasize;
        
        public object sendlock = new object();
        public object disconnectlock = new object();

        public string info = "";                          //info

                   
       public Socket s;                     //The client socket

       public bool authenicated = false;             //Handshake done or not       

       public System.Diagnostics.Stopwatch timeout;  //For timeout implementation
     //  public bool pinged = false;
    
    }
}
