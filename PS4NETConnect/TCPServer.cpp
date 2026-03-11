#include "TCPServer.h"
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




// ---------------- StartServer ----------------
bool TCPServer::StartServer(int Port)
{
    int sockfd = socket(AF_INET, SOCK_STREAM, 0);
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
    std::thread listener(&TCPServer::HandleClientListening, this, sockfd);
    listener.detach();

    DEBUGLOG << "Server started on port " << Port;
    return true;
}

// ---------------- HandleClientListening ----------------
void TCPServer::HandleClientListening(int sockfd)
{
    while (IsServerRunning)
    {
        sockaddr_in clientAddr;
        socklen_t addrLen = sizeof(clientAddr);
        int connfd = accept(sockfd, (sockaddr*)&clientAddr, &addrLen);

        if (connfd < 0)
        {
            DEBUGLOG << "Failed to accept client: " << strerror(errno);
            continue;
        }

        // Start a detached thread for the connected client
        std::thread clientThread(&TCPServer::HandleClientConnected, this, connfd, clientAddr);
        clientThread.detach();
    }

    close(sockfd); // Close the listening socket when server stops
}

// ---------------- HandleClientConnected ----------------
void TCPServer::HandleClientConnected(int connfd, const sockaddr_in& clientAddr)
{
    char buffer[1024];
    ssize_t bytesRead;
    bytesRead = read(connfd, buffer, sizeof(buffer));
    //while ((bytesRead = read(connfd, buffer, sizeof(buffer))) > 0 && IsServerRunning)
    //{
    //    // Echo or send your message
    //    UTF8Writer::sendMessage(connfd, "testing\n");
    //}

    const char response[] =
        "HTTP/1.1 200 OK\r\n"
        "Content-Type: text/plain\r\n"
        "Content-Length: 7\r\n"
        "\r\n"
        "bla bla bla";
    write(connfd, response, sizeof(response) - 1);
    
    close(connfd); // Close client socket
}