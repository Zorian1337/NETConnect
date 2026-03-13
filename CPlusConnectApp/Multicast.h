#pragma once

#include "UDPServer.h"
#include <guiddef.h>
#include <vector>
#include <cstdio>
#include "guid.hpp"
#include "Event.h"
#include "UTF8Helper.h"
#include "MulticastPacket.h"



class Multicast
{
public:
	xg::Guid SenderId; // Needs to defined per platform in preprocessers (current: GUID_WINDOWS=1) 
	UDPServer Server;
	struct sockaddr_in multicastAddr;

	Multicast() {
		SenderId = xg::newGuid();
	}


	// Make sure this signature matches your Event exactly
	void OnDataReceived(UDPClient& Client, std::vector<uint8_t>& data) {
		printf("%s received ->\n	%s\n", SenderId.str().c_str(), UTF8Helper::ToString(data).c_str());

		// Attempt to parse from MulticastPacket as this is a Multicast, so all data should be in this form anyway.
		MulticastPacket packet;
		if (!MulticastPacket::TryFromJson(UTF8Helper::ToString(data), packet)) {
			//printf("packet failed to be read\n");
			return;
		}

		// Ignore packets from self
		if (SenderId == packet.SenderId) return;


		//printf("Packet: %s\n", packet.ToJson().c_str());
		//printf("data received as a test\n");

		return; // blocked for debugging

		printf("[%s] -> sent packet data\n", packet.SenderId.str().c_str());

		switch (packet.Action) 
		{
			// JOIN
			case 0: {
				// Make sure this client is new before we add them to our list
				if (false) return; // fake out 

				// Parse our data for our client (IP:Port)
				std::string Data = UTF8Helper::ToString(packet.Data);
				std::vector<std::string> Addr = UTF8Helper::Split(Data, ':');

				// Only allow the size of 2 as we NEED these two pieces
				if (Addr.size() == 2) {
					// Handle our new peer
					std::string IP = Addr[0];
					std::string Port = Addr[1];

					// Log new peer and print they joined for now
					printf("Peer [%s:%s] joined the system.\n", IP.c_str(), Port.c_str());
				}




				break;
			}

			// LEAVE
			case 1: break;
			case 2: break;
		}
	}

	int SentToAll(const std::string Message) {
		return Server.SendTo((struct sockaddr*)&multicastAddr, Message.c_str());
	}

	bool StartMulticastServer(const char* IP, int Port) {
		// Wire on data received
		Server.OnUDPDataReceived.Subscribe(this, &Multicast::OnDataReceived);

		//SenderId = xg::newGuid();


		bool IsStarted = Server.StartMulticastServer(IP, Port, multicastAddr);

		if (IsStarted) {
			int bytesSent = SentToAll("THIS IS A TEST!");
		}
		return IsStarted;
	}
};