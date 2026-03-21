#pragma once

#include "PacketHeader.h"
#include <vector>
#include "json.hpp"
#include "simple_base64.h"
#include "UTF8Helper.h"

class PacketAuthentication {

public:
	PacketEncryptionType EncryptionType;
	std::vector<uint8_t> KeyData;



	std::string ToJson() {
		return UTF8Helper::ToJSON({
			{"EncryptionType", static_cast<uint16_t>(EncryptionType)},
			{"KeyData", base64_encode(UTF8Helper::ToString(KeyData))}
		});
	}
	
	//std::string ToJson() {
	//	return nlohmann::ordered_json{
	//	{"EncryptionType", static_cast<uint16_t>(EncryptionType)},
	//	{"KeyData", base64_encode(UTF8Helper::ToString(KeyData))}  // Convert data to base64 as its sent that way on our c# application (when byte[] gets serialized)
	//	}.dump().c_str();
	//}

	// Reused from MulticastPacket - both are very copy pastable

	static bool TryFromJson(const std::string& Json, PacketAuthentication& Packet) {

		auto* result = FromJson(Json);

		if (result == nullptr) return false;
		else {

			// * Dereferences the ptr
			Packet = *result; // Returns packet here as a refernce not a pointer
			delete result; // Deletes the ptr
			return true;
		}
	}

	static PacketAuthentication* FromJson(const std::string& Json) {
		using ordered = nlohmann::ordered_json;

		PacketAuthentication* p = new PacketAuthentication();

		try 
		{
			ordered parsed = ordered::parse(Json);

			//p->Action = static_cast<MulticastAction>(parsed.at("Action").get<int>()); - example
			//parsed.at("EncryptionType").get<static_cast<PacketEncryptionType>();
			p->EncryptionType = static_cast<PacketEncryptionType>(parsed.at("EncryptionType").get<uint16_t>()); // need to parse as ushort uint16

			// Decodes base64 as for some reason c++ auto converted to that -probaly should make an autodecoder
			std::string base64Data = parsed.at("KeyData").get<std::string>();
			std::string decodedStr = base64_decode(base64Data);
			std::vector<uint8_t> decodedData(decodedStr.begin(), decodedStr.end());
			p->KeyData = std::move(decodedData);

			return p;
		}
		catch (const std::exception& e) { return nullptr; }
	}
};