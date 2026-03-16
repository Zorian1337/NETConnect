#pragma once

#include "UDPServer.h"
//#include <guiddef.h>
#include <vector>
#include <cstdio>
#include "guid.hpp"
#include "Event.h"
#include "UTF8Helper.h"
#include "MulticastPacket.h"

//#include "../samples/_common/log.h"
class Multicast
{
public:
	xg::Guid SenderId;
	UDPServer Server;

	void Send() {
		MulticastPacket packet;
		packet.SenderId = xg::newGuid();

		std::string message = "192.168.68.5:6969";
		packet.Data.assign(message.begin(), message.end());
		packet.Action = MulticastAction::Join;

		if (Server.IsServerRunning) {
		
			Server.s
		}
	}

	// Make sure this signature matches your Event exactly
	static void OnDataReceived(UDPClient& Client, std::vector<uint8_t>& data) {


		printf("multicast message received!!\n");
		// Attempt to parse from MulticastPacket as this is a Multicast, so all data should be in this form anyway.
		MulticastPacket packet;
		if (!MulticastPacket::TryFromJson(UTF8Helper::ToString(data), packet)) {
			printf("packet failed to be read\n");
			return;
		}

		//printf("Packet: %s\n", packet.ToJson().c_str());
		//printf("data received as a test\n");

		printf("[%s] -> sent packet data\n", packet.SenderId.str().c_str());
		//DEBUGLOG << packet.SenderId.str().c_str() << "-> sent packet data\n";
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
					//DEBUGLOG << "Peer [" << IP.c_str() << ":" << Port.c_str() << "] joined the system.\n";
				}




				break;
			}

			// LEAVE
			case 1: break;
			case 2: break;
		}
	}

	bool StartMulticastServer(const char* IP, int Port) {
		// Wire on data received
		Server.OnUDPDataReceived += OnDataReceived;
		return Server.StartMulticastServer(IP, Port);
	}
};