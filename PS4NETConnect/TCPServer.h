#pragma once

#include <atomic>
#include <netinet/in.h>

class TCPServer
{
public:
    std::atomic<bool> IsServerRunning{ false };
    struct sockaddr_in serverAddr;

    // Starts the server on the given port
    bool StartServer(int Port);

private:
    void HandleClientListening(int sockfd);
    void HandleClientConnected(int connfd, const sockaddr_in& clientAddr);
};