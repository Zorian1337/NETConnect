#pragma once

//#include <winsock2.h> -included in _Network.h
//#include <ws2tcpip.h> -included in _Network.h
//#pragma comment(lib, "Ws2_32.lib") -included in _Network.h
#include "_Network.h" // Includes (winsock, ws2tcpip, NetUtil, or any other net headers)
#include "NetUtil.h"

#include "Event.h"
class TCPClient
{
public:

	// EventHandlers 

	Event<SocketHandler> OnConnected;
	Event<SocketHandler, std::vector<uint8_t>> OnDataReceived;

	bool Connect(const char* IP, int Port) {

		//  Validate Port here
		if (!NetUtil::IsValidPort(Port)) {
			Debugger::WriteLine("Failed to validate port");
			return false;
		}

		// Create Socket
		if (!NetUtil::TryCreateSocket(AF_INET, SOCK_STREAM, 0, sock)) {
			Debugger::WriteError("Failed to create socket: ");
			return false;
		}

		// Init addrs
		// 
		sockaddr_in serverAddr{};

		// Validate IP then try connect
		auto ip = NetUtil::ParseIP(IP);
		switch (ip.type) {
			case NetUtil::IPType::IPv4:
				
					serverAddr.sin_family = AF_INET;
					serverAddr.sin_addr.s_addr = ip.v4.S_un.S_addr;
					serverAddr.sin_port = htons(Port);
					if (connect(sock, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) != 0) {
						Debugger::WriteError("Failed to connect: ");
						closesocket(sock);
						return false;
					}

					// Handle valid connection here (IE store server address for future use, set up recv thread, etc)
					OnConnected.Invoke(sock);
				break;
			case NetUtil::IPType::IPv6: 
				// Implement this later, for now just return false since we dont support IPv6 yet (using sockaddr_storage should be good)
					Debugger::WriteError("IPv6 is not supported yet");
					return false;
				break;
			case NetUtil::IPType::None:
				Debugger::WriteError("Invalid IP address: " + std::string(IP));
				return false;
		}

	}
private:
	// Handles the socket for each platform (SOCKET for Windows, int for Linux)
	SocketHandler sock = INVALID_SOCKET; 
};

