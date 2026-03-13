#pragma once

#include "_Network.h" // Include common network headers, and defines SocketHandler for each platform



class NetUtil
{
public:

	// Returns bool True | False based on if the socket was successfully created.
	// Outputs: Socket for each platform (SOCKET for Windows, int for Linux)
	static bool TryCreateSocket(int af, int type, int protocol, SocketHandler& Socket) {
		#ifdef _WIN32
				WSADATA wsa;
				if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0) {
					printf("WSAStartup failed\n");
					return false;
				}
		#endif

		Socket = CreateSocket(af, type, protocol);
		
		if (Socket == -1) return false;
		else return true;
	}

	// Returns a socket for the current platform (SOCKET for Windows, int for Linux)
	static SocketHandler CreateSocket(int af, int type, int protocol) {
		return socket(af, type, protocol);
	}

	static bool GetIPv4Address(const char* IP, in_addr& addr) {
		if (inet_pton(AF_INET, IP, &addr) != 1) return false;
		else return true;
	}

	static bool GetIPv6Address(const char* IP, struct in6_addr& IPv6) {
		if (inet_pton(AF_INET6, IP, &IPv6) != 1) return false;
		else return true;
	}
};