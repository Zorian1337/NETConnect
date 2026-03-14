#pragma once

#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>

#pragma comment(lib, "Ws2_32.lib")

#include "Event.h"
#include <atomic> // Thread safe 



class TCPServer
{
public:
    // Atomic makes this thread safe (from what I've read)
    std::atomic<bool> IsServerRunning{ false };

    //Event<UDPClient&, std::vector<uint8_t>&> OnUDPDataReceived;

    // Starts the server on the given port
    bool StartServer(int Port);
    bool StartServer(const char* IP, int Port);

    void StopServer();

    int SendTo(const sockaddr* addr, const char* Message) {
        return sendto(sock, Message, strlen(Message), 0, addr, sizeof(sockaddr_in));
    }

private:

    SOCKET sock = INVALID_SOCKET;
    sockaddr_in serverAddr{};
    void HandleListening(SOCKET sock);
    //int ReceiveUDPData(SOCKET sock, char* buffer, int bufferSize, UDPClient& client);
};

