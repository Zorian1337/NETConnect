#pragma once

#include "_Debugging.h"
#include "_Network.h" // Include common network headers, and defines SocketHandler for each platform



class NetUtil
{
public:
	enum class IPType { None, IPv4, IPv6 };

	struct IPAddress {
		IPType type;
		union { // need to use more unions** - will need to return as sockaddr_storage either that or just have it in the connection areas specifically
			in_addr v4;
			in6_addr v6;
		};

		IPAddress() : type(IPType::None) {}
	};

	// Returns bool True | False based on if the port string was successfully parsed and valid.
	static bool TryParsePort(const char* PortStr, int& Port) {
		Port = ParsePort(PortStr);

		if (Port != -1) return true;
		else return false;
	}

	// Parses a port number from a string. Returns the port number if valid, or -1 if invalid.
	static int ParsePort(const char* Port) {
		int port = std::stoi(Port);
		Debugger::WriteLine("ParsedPort: \"" + std::to_string(port) + "\"");

		if (IsValidPort(port)) return port;
		else return -1;

		//return -1; // Implement this later, for now just return -1 to indicate invalid port
	}

	// Checks if port is within valid range (1-65535). Returns true if valid, false otherwise.
	static bool IsValidPort(int Port){
		return Port > 0 && Port <= 65535;
	}

	static IPAddress ParseIP(const char* IP) {
		IPAddress result;

		// Try IPv4
		if (inet_pton(AF_INET, IP, &result.v4) == 1) {
			result.type = IPType::IPv4;
			return result;
		}

		// Try IPv6
		if (inet_pton(AF_INET6, IP, &result.v6) == 1) {
			result.type = IPType::IPv6;
			return result;
		}

		return result; // type = None
	}

	static std::string GetLocalIPAddress() {

		#ifdef _WIN32
		std::string ip = "127.0.0.1";  // Default fallback
		DWORD dwSize = 0;

		// First call to get required buffer size
		GetAdaptersAddresses(AF_INET, GAA_FLAG_SKIP_ANYCAST |
			GAA_FLAG_SKIP_MULTICAST | GAA_FLAG_SKIP_DNS_SERVER,
			NULL, NULL, &dwSize);

		PIP_ADAPTER_ADDRESSES pAddresses = (IP_ADAPTER_ADDRESSES*)malloc(dwSize);
		if (!pAddresses) return ip;

		DWORD dwRetVal = GetAdaptersAddresses(AF_INET, GAA_FLAG_SKIP_ANYCAST |
			GAA_FLAG_SKIP_MULTICAST | GAA_FLAG_SKIP_DNS_SERVER,
			NULL, pAddresses, &dwSize);

		if (dwRetVal == NO_ERROR) {
			for (PIP_ADAPTER_ADDRESSES pCurr = pAddresses; pCurr; pCurr = pCurr->Next) {
				// Skip loopback and disconnected adapters
				if (pCurr->IfType != IF_TYPE_SOFTWARE_LOOPBACK &&
					pCurr->OperStatus == IfOperStatusUp) {

					PIP_ADAPTER_UNICAST_ADDRESS pUnicast = pCurr->FirstUnicastAddress;
					while (pUnicast) {
						sockaddr_in* addr = (sockaddr_in*)pUnicast->Address.lpSockaddr;
						char ipStr[INET_ADDRSTRLEN];
						inet_ntop(AF_INET, &addr->sin_addr, ipStr, sizeof(ipStr));

						// Skip 0.0.0.0 and 127.0.0.1
						if (strcmp(ipStr, "0.0.0.0") != 0 &&
							strcmp(ipStr, "127.0.0.1") != 0) {
							ip = ipStr;
							break;
						}
						pUnicast = pUnicast->Next;
					}
				}
			}
		}

		free(pAddresses);
		#endif	


		return ip;
	}

	static bool GetIPv4Address(const char* IP, in_addr& addr) {
		if (inet_pton(AF_INET, IP, &addr) != 1) return false;
		else return true;
	}

	static std::string GetIPv4String(const in_addr& addr) {
		char buffer[INET_ADDRSTRLEN];
		if (inet_ntop(AF_INET, &addr, buffer, sizeof(buffer)) == nullptr) return std::string();
		else return std::string(buffer); 
	}

	static bool GetIPv6String(const in6_addr& addr, char* buffer, size_t bufferSize) {
		if (inet_ntop(AF_INET6, &addr, buffer, bufferSize) == nullptr) return false;
		else return true;
	}

	static bool GetIPv6Address(const char* IP, struct in6_addr& IPv6) {
		if (inet_pton(AF_INET6, IP, &IPv6) != 1) return false;
		else return true;
	}


	// Returns bool True | False based on if the socket was successfully created.
	// Outputs: Socket for each platform (SOCKET for Windows, int for Linux)
	static bool TryCreateSocket(int af, int type, int protocol, SocketHandler& Socket) {
		#ifdef _WIN32
				WSADATA wsa;
				if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0) {
					Debugger::WriteLine("WSAStartup failed\n");
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

	// Checks if the stream has any available bytes by peaking and checking our required amount
	// Returns True when above or equal to our required amount
	static bool IsDataAvailable(SocketHandler sock, int requiredBytes) {
		if (sock == INVALID_SOCKET) return false;

		char peekBuffer[1]; // Just check if ANY data exists
		int result = recv(sock, peekBuffer, 1, MSG_PEEK);

		if (result > 0) {
			// Data exists - now check how much with FIONREAD
			#ifdef _WIN32
			unsigned long available;
			if (ioctlsocket(sock, FIONREAD, &available) == 0) {
				return available >= requiredBytes;
			}
			#else
			int available;
			if (ioctl(sock, FIONREAD, &available) == 0) {
				return available >= requiredBytes;
			}
			#endif
		}
		return false;
	}

};