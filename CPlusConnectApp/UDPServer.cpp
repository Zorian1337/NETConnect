#include "UDPServer.h"

//#include <winsock2.h> -included in _Network.h
//#include <ws2tcpip.h> -included in _Network.h
//#include "NetUtil.h" - included in _Network.h
//#pragma comment(lib, "Ws2_32.lib") - included in _Network.h

#include "_Network.h" // Includes (winsock, ws2tcpip, NetUtil, or any other net headers)

#include <iostream>
#include <thread>
#include "MulticastPacket.h"
#include "UTF8Helper.h"






bool UDPServer::StartMulticastServer(const char* IP, int Port, sockaddr_in& multicastAddr) {
    struct in_addr addr;
	if (!NetUtil::GetIPv4Address(IP, addr)) return false; // Testing NetUtil for getting IPv4 address, should be able to replace the inet_pton call with this
    
	if (!NetUtil::TryCreateSocket(AF_INET, SOCK_DGRAM, 0, sock)) { // Attempt to use NetUtil for creating socket, should be able to replace the socket call with this
        std::cout << "Failed to create socket: " << WSAGetLastError() << std::endl;
        return false;
    }

    // Allow address reuse for multiple listeners!
    BOOL reuse = TRUE;
    if (setsockopt(sock, SOL_SOCKET, SO_REUSEADDR, (const char*)&reuse, sizeof(reuse)) == SOCKET_ERROR) {
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

    // Stores multicast address 
    memset(&multicastAddr, 0, sizeof(multicastAddr));
    multicastAddr.sin_family = AF_INET;
    multicastAddr.sin_port = htons(Port);
    multicastAddr.sin_addr.S_un.S_addr = addr.S_un.S_addr;

    IsServerRunning = true;

    printf("Joined Multicast group [%s:%i]\n", IP, Port);
    //std::cout << "Joined Multicast group" << Port << std::endl;

    // Run the listener in a detached thread
    std::thread listener(&UDPServer::HandleListening, this, sock);
    listener.detach();

    return true;
}

bool UDPServer::StartServer(const char* IP, int Port) {
    struct in_addr addr;
    if (!NetUtil::GetIPv4Address(IP, addr)) return false;

    if (!NetUtil::TryCreateSocket(AF_INET, SOCK_DGRAM, 0, sock)) {
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
    if (!NetUtil::TryCreateSocket(AF_INET, SOCK_DGRAM, 0, sock)) {
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
        }
    }
}

int UDPServer::ReceiveUDPData(SOCKET sock, char* buffer, int bufferSize, UDPClient& client) {

    struct sockaddr_in clientAddr;
    socklen_t addrLen = sizeof(clientAddr);

    //struct sockaddr* addr = (struct sockaddr*)&clientAddr;
    int bytesRead = recvfrom(sock, buffer, bufferSize, 0, (struct sockaddr*)&clientAddr, &addrLen);

    if (bytesRead > 0) {
        // Corrects network order so we can read the IP and port
        inet_ntop(AF_INET, &clientAddr.sin_addr, client.IP, 16);
        client.Port = ntohs(clientAddr.sin_port);
        client.addr = clientAddr;
    }

    return bytesRead;
}