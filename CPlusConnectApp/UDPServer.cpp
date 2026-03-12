#include "UDPServer.h"

#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <thread>
#include "MulticastPacket.h"
#include "UTF8Helper.h"

#pragma comment(lib, "Ws2_32.lib")


//bool GetSocket(SOCKET &sock) {
//
//
//}



bool UDPServer::StartMulticastServer(const char* IP, int Port) {
    // INIT winsock
    WSADATA wsa;

    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
    {
        std::cout << "WSAStartup failed\n";
        return false;
    }

    struct in_addr addr;
    if (inet_pton(AF_INET, IP, &addr) != 1) return false;

    sock = socket(AF_INET, SOCK_DGRAM, 0);

    if (sock == INVALID_SOCKET) {

        std::cout << "Failed to create socket: " << WSAGetLastError() << std::endl;
        return false;
    }

    // Allow address reuse for multiple listeners!
    BOOL reuse = TRUE;
    if (setsockopt(sock, SOL_SOCKET, SO_REUSEADDR,
        (const char*)&reuse, sizeof(reuse)) == SOCKET_ERROR) {
        std::cout << "Failed to set SO_REUSEADDR: " << WSAGetLastError() << std::endl;
    }

    // Sets TTL
    int ttl = 1;
    if (setsockopt(sock, IPPROTO_IP, IP_MULTICAST_TTL, (const char*)&ttl, sizeof(ttl)) < 0) {
        std::cout << "Failed to set TTL" << std::endl;
        closesocket(sock);
        return false;
    }

    memset(&serverAddr, 0, sizeof(serverAddr));
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_addr.s_addr = htonl(INADDR_ANY); // bind to any IP not the multicast group (cannot bind to multicast IP)
    serverAddr.sin_port = htons(Port);

    if (bind(sock, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) != 0) {
        std::cout << "Failed to bind: " << WSAGetLastError() << std::endl;
        closesocket(sock);
        return false;
    }

    // Join the multicast group
    struct ip_mreq mreq;
    mreq.imr_multiaddr.s_addr = addr.S_un.S_addr;
    mreq.imr_interface.s_addr = htonl(INADDR_ANY);  

    if (setsockopt(sock, IPPROTO_IP, IP_ADD_MEMBERSHIP, (const char*)&mreq, sizeof(mreq)) == SOCKET_ERROR) {
        std::cout << "Failed to join group: " << WSAGetLastError() << std::endl;
        closesocket(sock);
        return false;
    }

    IsServerRunning = true;

    printf("Joined Multicast group [%s:%i]\n", IP, Port);
    //std::cout << "Joined Multicast group" << Port << std::endl;

    // Run the listener in a detached thread
    std::thread listener(&UDPServer::HandleListening, this, sock);
    listener.detach();

    return true;
}

bool UDPServer::StartServer(const char* IP, int Port) {
    // INIT winsock
    WSADATA wsa;

    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
    {
        std::cout << "WSAStartup failed\n";
        return false;
    }

    struct in_addr addr;
    if (inet_pton(AF_INET, IP, &addr) != 1) return false;

    sock = socket(AF_INET, SOCK_DGRAM, 0);

    if (sock == INVALID_SOCKET) {

        std::cout << "Failed to create socket: " << WSAGetLastError() << std::endl;
        return false;
    }

    memset(&serverAddr, 0, sizeof(serverAddr));
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_addr.s_addr = addr.S_un.S_addr;
    serverAddr.sin_port = htons(Port);

    if (bind(sock, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) != 0) {
        std::cout << "Failed to bind: " << WSAGetLastError() << std::endl;
        closesocket(sock);
        return false;
    }

    IsServerRunning = true;

    std::cout << "Server started on port " << Port << std::endl;

    // Run the listener in a detached thread
    std::thread listener(&UDPServer::HandleListening, this, sock);
    listener.detach();

    return true;
}

bool UDPServer::StartServer(int Port) {

    // INIT winsock
    WSADATA wsa;

    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
    {
        std::cout << "WSAStartup failed\n";
        return false;
    }

    sock = socket(AF_INET, SOCK_DGRAM, 0);

    if (sock == INVALID_SOCKET) {
    
        std::cout << "Failed to create socket: " << WSAGetLastError() << std::endl;
        return false;
    }

    memset(&serverAddr, 0, sizeof(serverAddr));
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_addr.s_addr = htonl(INADDR_ANY);
    serverAddr.sin_port = htons(Port);

    if (bind(sock, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) != 0) {
        std::cout << "Failed to bind: " << WSAGetLastError() << WSAGetLastError() << std::endl;
        closesocket(sock);
        return false;
    }

    IsServerRunning = true;

    std::cout << "Server started on port " << Port << std::endl;

    // Run the listener in a detached thread
    std::thread listener(&UDPServer::HandleListening, this, sock);
    listener.detach();

    return true;
}

void UDPServer::HandleListening(SOCKET sock) {
    // Reusables
    UDPClient client;
    int bytesRead;
    char Buffer[1024];

    while (IsServerRunning) {

        bytesRead = ReceiveUDPData(sock, Buffer, sizeof(Buffer), client);

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

int UDPServer::ReceiveUDPData(SOCKET sock, char* buffer, int bufferSize, UDPClient& client) {

    struct sockaddr_in clientAddr;
    socklen_t addrLen = sizeof(clientAddr);

    struct sockaddr* addr = (struct sockaddr*)&clientAddr;
    int bytesRead = recvfrom(sock, buffer, bufferSize, 0, addr, &addrLen);

    if (bytesRead > 0) {
        // Corrects network order so we can read the IP and port
        inet_ntop(AF_INET, &clientAddr.sin_addr, client.IP, 16);
        client.Port = ntohs(clientAddr.sin_port);
        client.addr = addr;
    }

    return bytesRead;
}