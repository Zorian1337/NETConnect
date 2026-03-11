#include "UDPServer.h"
#include <thread>
#include <string>
#include <cerrno>
#include <cstring>
#include "UTF8Writer.h"

// Wrap C headers in extern "C" to prevent C++ mangling
extern "C" {
#include <stdio.h>
#include <string.h>
#include <errno.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include "../common/log.h"
#include <orbis/libkernel.h>  // PS4 SDK
}

bool UDPServer::StartServer(int Port) {

    int sockfd = socket(AF_INET, SOCK_DGRAM, 0);
    if (sockfd < 0)
    {
        DEBUGLOG << "Failed to create socket: " << strerror(errno);
        return false;
    }

    memset(&serverAddr, 0, sizeof(serverAddr));
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_addr.s_addr = htonl(INADDR_ANY);
    serverAddr.sin_port = htons(Port);

    if (bind(sockfd, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) != 0)
    {
        DEBUGLOG << "Failed to bind: " << strerror(errno); // log.h sets DEBUGLOG
        close(sockfd);
        return false;
    }

    if (listen(sockfd, 5) != 0)
    {
        DEBUGLOG << "Failed to listen: " << strerror(errno);
        close(sockfd);
        return false;
    }

    IsServerRunning = true;

    // Run the listener in a detached thread
    std::thread listener(&UDPServer::HandleClientListening, this, sockfd);
    listener.detach();

    DEBUGLOG << "Server started on port " << Port;
    return true;
}

void UDPServer::HandleClientListening(int sockfd) {

    while (IsServerRunning) {


    }
}