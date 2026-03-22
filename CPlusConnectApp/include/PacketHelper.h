#pragma once
#include "_Network.h"
#include "_Debugging.h"

#include <string>
#include "Node.h"
#include "PacketHeader.h"
#include "SecurityKey.h"
#include "PacketEncrypted.h"
#include "UTF8Helper.h"


// Based on our C# version this was stuffed into every type of server/client instance and included security key data so we could always have the same structure everywhere
class PacketHelper {

public:
	Node* Self;
	SocketHandler sock;
	SecurityKey EncryptionKeys{};

	PacketHelper() = default;
	PacketHelper(Node* Peer, SocketHandler sock) : Self(Peer), sock(sock) { }

	int SendPacket(std::vector<uint8_t> data, PacketActionType Type = PacketActionType::PacketData, bool IsEncryptionEnabled = false, PacketEncryptionType EncryptionType = PacketEncryptionType::NONE) {
		int bytesSent = -1;



		// If encryption is enabled, we will auto encrypt and decrypt our messages
		if (IsEncryptionEnabled) bytesSent = SendEncryptedPacket(data, EncryptionType, Type, false);
		else // unencrypted data by default
		{
			Debugger::WriteLine("SendPacket -> Creating header");
			PacketHeader header = PacketHeader::CreateHeader(data.size(), Type, EncryptionType);

			// Packs the data with the header
			std::vector<uint8_t> packet = header.Serialize();
			packet.insert(packet.end(), data.begin(), data.end());
			Debugger::WriteLine("SendPacket -> Packing data");

			bytesSent = send(sock, reinterpret_cast<const char*>(packet.data()), packet.size(), 0);

			if (bytesSent > 0) {

				for (char c : packet) {
					std::cout << std::hex << std::setw(2) << std::setfill('0')
						<< static_cast<int>(static_cast<unsigned char>(c)) << " ";
				}
				std::cout << std::endl;
				Debugger::WriteLine("[SendPacket] Sent: " + header.ToJSON() + "\n");
			}
			Debugger::WriteLine("SendPacket -> Sent Data");
		}

		return bytesSent;
	}

	// Change IsEncryptionEnabled back to true after we add encryption
	int SendUTF8Packet(std::string UTF8Data, PacketActionType Type = PacketActionType::PacketData, bool IsEncryptionEnabled = false, PacketEncryptionType EncryptionType = PacketEncryptionType::NONE){
		// Returns base send packet as everything in this should be the same or at least handled similarly
		return SendPacket(UTF8Helper::ToVector(UTF8Data), Type, IsEncryptionEnabled, EncryptionType);
	}
	


	int SendEncryptedPacket(std::vector<uint8_t> data, PacketEncryptionType EncryptionType, PacketActionType ActionType, bool IsAlreadyEncryptedData = true) {
		int bytesSent = -1;

		if (data.size() == 0) return bytesSent;

		
		// I havent even tested IsAlreadyEncryptedData yet I dont think
		// Basically if our data is already encrypted, just send the normal packet saying its not encrypted - might need to make a new SendPacket() where we can pass an existing header
		if (IsAlreadyEncryptedData) {
			bytesSent = SendPacket(data, ActionType, false, EncryptionType);
			Debugger::WriteLine("SendEncryptedPacket -> Sent to SendPacket");
		}
		else 
		{
			// We cant have a 1 to 1 version of the C# version as we are using different types per keys rather than just bytes

			// Handle encryption just inside of here* - Reuse PacketEncrypted::Encrypt as we already built this LOL
			const auto& encrypted = PacketEncrypted::Encrypt(EncryptionKeys, data, EncryptionType, true);
			Debugger::WriteLine("SendEncryptedPacket -> encrypted");


			// Ship off our encrypted packet - and have SendPacket handle the header
			bytesSent = SendPacket(encrypted, ActionType, false, EncryptionType);
			Debugger::WriteLine("SendEncryptedPacket -> Sent to SendPacket");
		}

		return bytesSent;
	}


	//std::vector<uint8_t> data(Packet.begin(), Packet.end());
	std::vector<char> ReceivePacket(PacketHeader& Header) {
		return PacketHeader::ReceivePacketTCP(sock, Header);
	}

	std::vector<uint8_t> ReceiveUTF8Packet(PacketHeader& Header) {
		const auto& ch = PacketHeader::ReceivePacketTCP(sock, Header);
		std::vector<uint8_t> data(ch.begin(), ch.end());
		return data;
	}
};