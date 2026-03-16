#pragma once

#include "UDPServer.h"

#include <vector>
#include <cstdio>
#include "Event.h"
#include "UTF8Helper.h"
#include "MulticastPacket.h"

#include <guiddef.h>
#include "guid.hpp"

//#include "_Peer.h"



class Node; // This is called a forward declaration


class Multicast
{
public:
	Node& Self;
	UDPServer Server { Self };
	struct sockaddr_in multicastAddr;
	std::atomic<bool> IsServerRunning{ false };

	explicit Multicast(Node& Peer) : Self(Peer) 
	{
		// Wire events on init
		Server.OnUDPDataReceived.Subscribe(this, &Multicast::OnDataReceived);
	}

	// Implementation moved to .cpp file to avoid circular dependency issues with Node.h
	void OnDataReceived(UDPClient Client, std::vector<uint8_t> data); // no clue why this is here and unused.

	void CheckForPackets() {
		if (!IsServerRunning || Server.sock == INVALID_SOCKET) return;

		char buffer[4096];
		sockaddr_in clientAddr;
		socklen_t addrLen = sizeof(clientAddr);

		// Non-blocking receive - returns immediately if no data
		int bytes = recvfrom(Server.sock, buffer, sizeof(buffer), 0,
			(sockaddr*)&clientAddr, &addrLen);

		if (bytes > 0) {
			// Got a packet! Queue processing to thread pool
			std::vector<uint8_t> data(buffer, buffer + bytes);

			// Queue to thread pool - don't process here!
			//Self.GetThreadPool().enqueue([this, data, clientAddr]() {
			//	this->ProcessPacket(data, clientAddr);
			//	});
		}
	}

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