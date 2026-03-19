//#include "PacketHelper.h"
//
//int PacketHelper::SendUTF8Packet(std::string UTF8Data, PacketActionType Type, bool IsEncryptionEnabled, PacketEncryptionType EncryptionType)
//{
//	int bytesSent = -1;
//
//	if (IsEncryptionEnabled) {
//		// Doesnt exist yet
//	}
//	else
//	{
//		auto header = new PacketHeader(UTF8Data.length(), Type, EncryptionType);
//		auto by = header->Serialize();
//		send(sock, reinterpret_cast<const char*>(by.data()), by.size(), 0);
//	}
//	
//	return bytesSent;
//}
