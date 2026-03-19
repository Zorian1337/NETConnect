// This needs to support crossplatform via typedef and preprocessor directives, but for now I will just implement the Windows version and worry about PS4 later.

#include "TCPServer.h"

//#include <winsock2.h> -included in _Network.h
//#include <ws2tcpip.h> -included in _Network.h
//#include "NetUtil.h" - included in _Network.h
//#pragma comment(lib, "Ws2_32.lib") - included in _Network.h

#include "_Debugging.h"
#include "_Network.h" // Includes (winsock, ws2tcpip, NetUtil, or any other net headers)
#include "NetUtil.h" 

#include <iostream>
#include <thread>
#include "UTF8Helper.h"

bool TCPServer::StartServer(const char* IP, int Port, bool IsBlocking) {
	
	struct in_addr addr;
	if (!NetUtil::GetIPv4Address(IP, addr)) return false;

	if (!NetUtil::TryCreateSocket(AF_INET, SOCK_STREAM, 0, sock)) {
		Debugger::WriteError("Failed to create socket: ");
		//std::cout << "Failed to create socket: " << WSAGetLastError() << std::endl;
		return false;
	}

	memset(&serverAddr, 0, sizeof(serverAddr));
	serverAddr.sin_family = AF_INET;
	serverAddr.sin_addr.s_addr = addr.S_un.S_addr;
	serverAddr.sin_port = htons(Port);

	if (!IsBlocking) {
		// Set NON-BLOCKING mode
		#ifdef _WIN32
		u_long mode = 1;
		ioctlsocket(sock, FIONBIO, &mode);
		#else
		int flags = fcntl(udpSocket, F_GETFL, 0);
		fcntl(udpSocket, F_SETFL, flags | O_NONBLOCK);
		#endif
	}

	if (bind(sock, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) != 0) {
		Debugger::WriteError("Failed to bind: ");
		//std::cout << "Failed to bind: " << WSAGetLastError() << std::endl;
		closesocket(sock);
		return false;
	}

	// Get the actual bound address
	sockaddr_in boundAddr;
	socklen_t addrLen = sizeof(boundAddr);
	if (getsockname(sock, (sockaddr*)&boundAddr, &addrLen) == 0) {
		char ip[INET_ADDRSTRLEN];
		inet_ntop(AF_INET, &boundAddr.sin_addr, ip, sizeof(ip));
		BoundIP = ip;
		BoundPort = ntohs(boundAddr.sin_port);
	}

	// We can probably have more backlog I dont really care but we'll keep it at 5
	if (listen(sock, 5) != 0)
	{
		Debugger::WriteError("Failed to listen: ");
		//DEBUGLOG << "Failed to listen: " << strerror(errno);
		closesocket(sock);
		return false;
	}

	IsServerRunning = true;

	//std::cout << "Server started on port " << Port << std::endl;
	Debugger::WriteLine("Server started on port " + Port);

	if (IsBlocking) {
		// Run the listener in a detached thread
		std::thread listener(&TCPServer::HandleListening, this, sock);
		listener.detach();
	}

	return true;
}


bool TCPServer::StartServer(int Port, bool IsBlocking) {
	if (!NetUtil::TryCreateSocket(AF_INET, SOCK_STREAM, 0, sock)) {
		//std::cout << "Failed to create socket: " << WSAGetLastError() << std::endl;
		Debugger::WriteError("Failed to create socket: ");
		return false;
	}

	memset(&serverAddr, 0, sizeof(serverAddr));
	serverAddr.sin_family = AF_INET;
	serverAddr.sin_addr.s_addr = htonl(INADDR_ANY);
	serverAddr.sin_port = htons(Port);

	// Gets the local IP address for debugging purposes, not used for binding since we bind to INADDR_ANY
	std::string IPv4 = NetUtil::GetIPv4String(serverAddr.sin_addr);

	if (!IsBlocking) {
		// Set NON-BLOCKING mode
		#ifdef _WIN32
		u_long mode = 1;
		ioctlsocket(sock, FIONBIO, &mode);
		#else
		int flags = fcntl(udpSocket, F_GETFL, 0);
		fcntl(udpSocket, F_SETFL, flags | O_NONBLOCK);
		#endif
	}

	if (bind(sock, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) != 0) {
		//std::cout << "Failed to bind: " << WSAGetLastError() << std::endl;
		Debugger::WriteError("Failed to bind: ");
		closesocket(sock);
		return false;
	}

	// Get the actual bound address
	sockaddr_in boundAddr;
	socklen_t addrLen = sizeof(boundAddr);
	if (getsockname(sock, (sockaddr*)&boundAddr, &addrLen) == 0) {
		char ip[INET_ADDRSTRLEN];
		inet_ntop(AF_INET, &boundAddr.sin_addr, ip, sizeof(ip));
		BoundIP = ip;
		BoundPort = ntohs(boundAddr.sin_port);
	}

	// We can probably have more backlog I dont really care but we'll keep it at 5
	if (listen(sock, 5) != 0)
	{
		//DEBUGLOG << "Failed to listen: " << strerror(errno);
		printf("Failed to listen: %i", WSAGetLastError());
		//Debugger::WriteError("Failed to listen: "); // this isnt perfect yet
		closesocket(sock);
		return false;
	}

	IsServerRunning = true;

	//std::cout << "Server started on port " << Port << std::endl;
	//Debugger::WriteError("Server started on port " + Port);
	printf("TCPServer server started [%s:%i]\n", IPv4.c_str(), Port);

	if (IsBlocking) {
		// Run the listener in a detached thread
		std::thread listener(&TCPServer::HandleListening, this, sock);
		listener.detach();
	}
	return true;
}

void TCPServer::HandleListening(SocketHandler sock) {
	while (IsServerRunning) {
		TCPServerClient client{};
		sockaddr_in clientAddr{};
		socklen_t addrLen = sizeof(clientAddr);

		//std::cout << "Listening for clients..." << std::endl;
		Debugger::WriteLine("Listening for clients...");

		// Wait for client, Store client data, then handle client join..
		SocketHandler conn = accept(sock, (sockaddr*)&clientAddr, &addrLen);

		if (conn == INVALID_SOCKET) {
			Debugger::WriteLine("Client accept failed: Socket Invalid");
			closesocket(conn);
			continue;
		}

		client.Port = ntohs(clientAddr.sin_port);
		client.addr = clientAddr;
		inet_ntop(AF_INET, &clientAddr.sin_addr, client.IP, sizeof(client.IP));

		Debugger::WriteLine("Client Connected");

		// Sets up our connection event, and storing a piece of TCPServer for future use.
		OnClientConnected.InvokeAsync(conn, client); // Use subscribe to set handler
	}
}

//
void TCPServer::HandleClientConnected(SocketHandler sock, TCPServerClient client) {
	// Reusables

	int bytesRead;
	char Buffer[1024];

	Debugger::WriteLine("TCPClient connected, Handling Connection\n");

	while (IsServerRunning) {

		
		bytesRead = recv(sock, Buffer, sizeof(Buffer), 0);

		if (bytesRead > 0) {
			Debugger::WriteLine("Received data from client " + std::string(client.IP) + ":" + std::to_string(client.Port) + "\n ->"  + UTF8Helper::ToString(Buffer,bytesRead));

			// Packages utf8 data for event handling
			std::vector<uint8_t> data(Buffer, Buffer + bytesRead);
			//OnTCPDataReceived.InvokeAsync(client, data);
		}
	}
}

void TCPServer::HandleTCPDataReceived(TCPServerClient client, std::vector<uint8_t> data)
{
	Debugger::WriteLine("TCPData received from client!\n");
}
