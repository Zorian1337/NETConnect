#pragma once

#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>

#pragma comment(lib, "Ws2_32.lib")

#include <atomic>
#include "Event.h"


class UDPClient {
public:
    SOCKET sock;
    char IP[16];
    int Port;
    //sockaddr* addr;
    struct sockaddr_in addr; // stores address not just ptr

    int SendTo(const char* Message) {
        return sendto(sock, Message, strlen(Message), 0,
            (struct sockaddr*)&addr, sizeof(addr));
    }
};


class UDPServer
{
public:
    // Atomic makes this thread safe (from what I've read)
    std::atomic<bool> IsServerRunning{ false };

    Event<UDPClient&, std::vector<uint8_t>&> OnUDPDataReceived;

    // Starts the server on the given port
    bool StartServer(int Port);
    bool StartServer(const char* IP, int Port);
    bool StartMulticastServer(const char* IP, int Port, sockaddr_in& multicastAddr);

    void StopServer();

    int SendTo(const sockaddr* addr, const char* Message) {
        return sendto(sock, Message, strlen(Message), 0, addr, sizeof(sockaddr_in));
    }

private:

    SOCKET sock = INVALID_SOCKET;
    sockaddr_in serverAddr{};
    void HandleListening(SOCKET sock);
    int ReceiveUDPData(SOCKET sock, char* buffer, int bufferSize, UDPClient& client);
};
