# .Net-Async-server-client-protocol
Async multi-client TCP/IP server written in C# .Net 4.5

Server declaration
```
server = new MySocketLibrary.Server("IP:Port", OnClientConnected, OnClientDisconnected, OnDataRecieved, TimeoutInms);
server.listen();
```
The server expects a fixed 4 byte integer containing size of message followed by message and upon successful receiving of entire message, the server will trigger OnDataRecieved with message contents size and size and immediately clear the receive buffers.

Server object contains a public array of connected clients but it is adivisable to manage your own seperate client array using OnClientConnected and OnClientDisconnected. 

There's a built-in timeout function that will automatically disconnect un-responsive clients but it is advisable to implement your own aside from the built in routine. A simple keep-alive message that the client sends every interval of time to server should do.

Gzip class in library is included but never used. If messages you're sending between client and server are large or requires a substantial time of transfer between server and client, you should probably modify the code to automatically compress and decompress data using gzip and/or append received data immediately into a file if you're transfering big files, because current implementation queues all incoming data in heap and last refrence to the data after it is been assembled is passed with OnDataRecieved() which GC should automatically collect if you don't make further use of the refrence. 


