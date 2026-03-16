#pragma once

//#include <winsock2.h> -included in _Network.h
//#include <ws2tcpip.h> -included in _Network.h
//#pragma comment(lib, "Ws2_32.lib") -included in _Network.h
#include "_Network.h" // Includes (winsock, ws2tcpip, NetUtil, or any other net headers)

#include <iostream>
#include <atomic>
#include "Event.h"

#include "Node.h"
class UDPClient {
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

class Node;  // Forward declaration

class UDPServer
{
public:
    Node& Self;

    // Handles the socket for each platform (SOCKET for Windows, int for Linux)
    SocketHandler sock = INVALID_SOCKET;
    sockaddr_in serverAddr{};

    std::atomic<bool> IsServerRunning{ false };

    explicit UDPServer(Node& Peer); //: Self(Peer){
    


    Event<UDPClient, std::vector<uint8_t>> OnUDPDataReceived;

    // Starts the server on the given port
    bool StartServer(int Port, bool IsBlocking = true);
    bool StartServer(const char* IP, int Port, bool IsBlocking = true);
    bool StartMulticastServer(const char* IP, int Port, sockaddr_in& multicastAddr, bool IsBlocking = true);

    void StopServer();

    int SendTo(const sockaddr* addr, const char* Message) {
        return sendto(sock, Message, strlen(Message), 0, addr, sizeof(sockaddr_in));
    }

private:
    void HandleListening(SocketHandler sock);
    int ReceiveUDPData(SocketHandler sock, char* buffer, int bufferSize, UDPClient& client);
};
