#pragma once


#include <iostream>

#include <atomic>
#include "Event.h"
#include <vector>


//#include <Stdint.h>
//#include <netinet/in.h>
#include <sys/socket.h>    // For socket functions
#include <netinet/in.h>     // For sockaddr_in
//#include <arpa/inet.h>      // For inet_pton
//#include <unistd.h>         // For close()
//#include <cstring>          // For memset






class UDPClient {
public:
    int sockfd;
    char IP[16];
    int Port;
    //sockaddr* addr;
    struct sockaddr_in addr; // stores address not just ptr

    int SendTo(const char* Message) {
        return sendto(sockfd, Message, strlen(Message), 0,
            (struct sockaddr*)&addr, sizeof(addr));
    }
};


class UDPServer
{
public:
    int sockfd = 0;
    // Atomic makes this thread safe (from what I've read)
    std::atomic<bool> IsServerRunning{ false };

    Event<UDPClient&, std::vector<uint8_t>&> OnUDPDataReceived;

    // Starts the server on the given port
    bool StartServer(int Port);
    bool StartServer(const char* IP, int Port);
    bool StartMulticastServer(const char* IP, int Port);

    void StopServer();

    int SendTo(const sockaddr* addr, const char* Message) {
        return sendto(sockfd, Message, strlen(Message), 0, addr, sizeof(addr));
    }
private:
    sockaddr_in serverAddr{};
    void HandleListening();
    int ReceiveUDPData(char* buffer, int bufferSize, UDPClient& client);
};
