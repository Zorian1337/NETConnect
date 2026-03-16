#include "UDPServer.h"


#include <iostream>
#include <thread>
//#include "MulticastPacket.h"
#include "UTF8Helper.h"


#include <sys/socket.h>    // For socket functions
#include <netinet/in.h>     // For sockaddr_in
#include <arpa/inet.h>      // For inet_pton
#include <unistd.h>         // For close()
#include <cstring>          // For memset





bool UDPServer::StartMulticastServer(const char* IP, int Port) {

    struct in_addr addr;
    if (inet_pton(AF_INET, IP, &addr) != 1) return false;

    sockfd = socket(AF_INET, SOCK_DGRAM, 0);

    if (sockfd < 0) {

        std::cout << "Failed to create socket: " << strerror(errno) << std::endl;
        return false;
    }

    // Allow address reuse for multiple listeners!
    bool reuse = true;
    if (setsockopt(sockfd, SOL_SOCKET, SO_REUSEADDR,
        (const char*)&reuse, sizeof(reuse)) < 0) {
        std::cout << "Failed to set SO_REUSEADDR: " << strerror(errno) << std::endl;
    }

    // Sets TTL
    int ttl = 1;
    if (setsockopt(sockfd, IPPROTO_IP, IP_MULTICAST_TTL, (const char*)&ttl, sizeof(ttl)) < 0) {
        std::cout << "Failed to set TTL" << std::endl;
        close(sockfd);
        return false;
    }

    memset(&serverAddr, 0, sizeof(serverAddr));
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_addr.s_addr = htonl(INADDR_ANY); // bind to any IP not the multicast group (cannot bind to multicast IP)
    serverAddr.sin_port = htons(Port);

    if (bind(sockfd, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) != 0) {
        std::cout << "Failed to bind: " << strerror(errno) << std::endl;
        close(sockfd);
        return false;
    }

    // Join the multicast group
    struct ip_mreq mreq;
    mreq.imr_multiaddr.s_addr = addr.s_addr;
    mreq.imr_interface.s_addr = htonl(INADDR_ANY);  

    if (setsockopt(sockfd, IPPROTO_IP, IP_ADD_MEMBERSHIP, (const char*)&mreq, sizeof(mreq)) < 0) {
        std::cout << "Failed to join group: " << strerror(errno) << std::endl;
        close(sockfd);
        return false;
    }

    IsServerRunning = true;

    printf("Joined Multicast group [%s:%i]\n", IP, Port);
    //std::cout << "Joined Multicast group" << Port << std::endl;

    // Run the listener in a detached thread
    std::thread listener(&UDPServer::HandleListening, this, sockfd);
    listener.detach();

    return true;
}

bool UDPServer::StartServer(const char* IP, int Port) {

    struct in_addr addr;
    if (inet_pton(AF_INET, IP, &addr) != 1) return false;

    sockfd = socket(AF_INET, SOCK_DGRAM, 0);

    if (sockfd < 0) {

        std::cout << "Failed to create socket: " << strerror(errno) << std::endl;
        return false;
    }

    memset(&serverAddr, 0, sizeof(serverAddr));
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_addr.s_addr = addr.s_addr;
    serverAddr.sin_port = htons(Port);

    if (bind(sockfd, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) != 0) {
        std::cout << "Failed to bind: " << strerror(errno) << std::endl;
        close(sockfd);
        return false;
    }

    IsServerRunning = true;

    std::cout << "Server started on port " << Port << std::endl;

    // Run the listener in a detached thread
    std::thread listener(&UDPServer::HandleListening, this, sockfd);
    listener.detach();

    return true;
}

bool UDPServer::StartServer(int Port) {

    sockfd = socket(AF_INET, SOCK_DGRAM, 0);

    if (sockfd < 0) {
    
        std::cout << "Failed to create socket: " << strerror(errno) << std::endl;
        return false;
    }

    memset(&serverAddr, 0, sizeof(serverAddr));
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_addr.s_addr = htonl(INADDR_ANY);
    serverAddr.sin_port = htons(Port);

    if (bind(sockfd, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) != 0) {
        std::cout << "Failed to bind: " << strerror(errno) << std::endl;
        close(sockfd);
        return false;
    }

    IsServerRunning = true;

    std::cout << "Server started on port " << Port << std::endl;

    // Run the listener in a detached thread
    std::thread listener(&UDPServer::HandleListening, this, sockfd);
    listener.detach();

    return true;
}

void UDPServer::HandleListening() {
    // Reusables
    UDPClient client;
    int bytesRead;
    char Buffer[1024];

    while (IsServerRunning) {

        bytesRead = ReceiveUDPData(Buffer, sizeof(Buffer), client);

        if (bytesRead > 0) {
            // Packages utf8 data for event handling
            std::vector<uint8_t> data(Buffer, Buffer + bytesRead);
            OnUDPDataReceived.Invoke(client, data);

            //MulticastPacket packet;
            //if (!MulticastPacket::TryFromJson(UTF8Helper::ToString(data), packet)) {
            //    printf("packet failed to be read\n");
            //    continue;
            //}

            //printf("Packet: %s\n", packet.ToJson().c_str());
        }
    }
}

int UDPServer::ReceiveUDPData(char* buffer, int bufferSize, UDPClient& client) {

    struct sockaddr_in clientAddr;
    socklen_t addrLen = sizeof(clientAddr);

    //struct sockaddr* addr = (struct sockaddr*)&clientAddr;
    int bytesRead = recvfrom(sockfd, buffer, bufferSize, 0, (struct sockaddr*)&clientAddr, &addrLen);

    if (bytesRead > 0) {
        // Corrects network order so we can read the IP and port
        inet_ntop(AF_INET, &clientAddr.sin_addr, client.IP, 16);
        client.Port = ntohs(clientAddr.sin_port);
        client.addr = clientAddr;

        client.sockfd = sockfd;
    }

    return bytesRead;
}