#pragma once

//#include <winsock2.h> -included in _Network.h
//#include <ws2tcpip.h> -included in _Network.h
//#pragma comment(lib, "Ws2_32.lib") -included in _Network.h
#include "_Network.h" // Includes (winsock, ws2tcpip, NetUtil, or any other net headers)
#include "NetUtil.h"

#include <iostream>
#include <atomic>
#include "Event.h"


// Used to store clients that connect to our server, and send data to them.
class TCPServerClient {
public:
    SocketHandler sock;
    char IP[16];
    int Port;

    struct sockaddr_in addr; // stores address not just ptr

    int SendTo(const char* Message) {
        return sendto(sock, Message, strlen(Message), 0,
            (struct sockaddr*)&addr, sizeof(addr));
    }
};

class TCPServer
{
public:
    std::vector<SocketHandler> clients;

    TCPServer() {
        // Register events on init
        OnClientConnected.Subscribe(this, &TCPServer::HandleClientConnected);
		OnTCPDataReceived.Subscribe(this, &TCPServer::HandleTCPDataReceived);
    }
    
    sockaddr_in serverAddr{};
	std::string BoundIP; // Store the IP we bound to for future use (IE sending to clients, etc)
	int BoundPort; // Store the port we bound to for future use (IE sending to clients, etc)

    // Atomic makes this thread safe (from what I've read)
    std::atomic<bool> IsServerRunning{ false };

	Event<SocketHandler, TCPServerClient> OnClientConnected;
    Event<TCPServerClient, std::vector<uint8_t>> OnTCPDataReceived;


    // Starts the server on the given port
    bool StartServer(int Port);
    bool StartServer(const char* IP, int Port);

    std::string GetHostIPPort() const {
        std::string IP = NetUtil::GetLocalIPAddress();
        return IP + ":" + std::to_string(BoundPort);
	}

    void StopServer();

    int SendTo(const sockaddr* addr, const char* Message) {
        return sendto(sock, Message, strlen(Message), 0, addr, sizeof(sockaddr_in));
    }


    void HandleClientConnected(SocketHandler sock, TCPServerClient client);
	void HandleTCPDataReceived(TCPServerClient client, std::vector<uint8_t> data);

private:
    // Handles the socket for each platform (SOCKET for Windows, int for Linux)
    SocketHandler sock = INVALID_SOCKET;
    
    void HandleListening(SocketHandler sock);
    
    //int ReceiveTCPData(SocketHandler sock, char* buffer, int bufferSize, TCPClient& client);
};

