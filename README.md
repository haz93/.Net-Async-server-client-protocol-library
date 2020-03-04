# .Net-Async-server-client-protocol
Async multi-client server written in .Net 4.5

Server declaration
```
server = new MySocketLibrary.Server("IP:Port", OnClientConnected, OnClientDisconnected, OnDataRecieved, TimeoutInms);
server.listen();
```
The server expects a fixed 4 byte integer containing size of message followed by message and upon successful receiving of entire message, the server will trigger OnDataRecieved with message contents size and size and immediately clear the receive buffers.

You can modify 
