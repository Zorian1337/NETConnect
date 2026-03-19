#pragma once

#include "UDPServer.h"

#include <vector>
#include <cstdio>
#include "Event.h"
#include "_Debugging.h"
#include "UTF8Helper.h"
#include "MulticastPacket.h"

#include <guiddef.h>
#include "guid.hpp"

//#include "_Peer.h"



class Node; // This is called a forward declaration


class Multicast
{
public:
	Node* Self;
	UDPServer Server; // { Self };
	struct sockaddr_in multicastAddr;
	std::atomic<bool> IsServerRunning{ false };

	explicit Multicast() : Self(nullptr) {}
	explicit Multicast(Node* Peer) : Self(Peer), Server(Peer) {}

	// Implementation moved to .cpp file to avoid circular dependency issues with Node.h (we want to access Self)
	void OnDataReceived(UDPClient Client, std::vector<uint8_t> data); 

	// Implementation moved to .cpp file to avoid circular depency issues with Node.h (we want to access Self)
	void CheckForPackets(); 

	//void ProcessPacket(std::vector<uint8_t> data, sockaddr_in clientAddr) {
	//	Debugger::WriteLine("Im handling the packet!!!");
	//}


	int SendToAll(const std::string Message) {
		return Server.SendTo((struct sockaddr*)&multicastAddr, Message.c_str());
	}

	int SendPacket(const std::vector<uint8_t>& _data, MulticastAction _actionType);

	//bool BindMulticastSocket

	bool StartMulticastServer(const char* IP, int Port, bool IsBlocking = true) {
		IsServerRunning = Server.StartMulticastServer(IP, Port, multicastAddr, IsBlocking);

		if (IsServerRunning) {
			int bytesSent = SendToAll("THIS IS A TEST!");
		}
		return IsServerRunning;
	}
};