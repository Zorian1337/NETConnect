#pragma once

#include "PacketHeader.h"
//#include "PacketHelper.h" // Removed due to PacketHelper including this header
#include <vector>
#include "ChaCha.h"
#include "UTF8Helper.h"
#include "simple_base64.h"

class PacketEncrypted {
public:
	std::vector<uint8_t> encryptedData;
	std::vector<uint8_t> Nonce;
	std::vector<uint8_t> Tag;
	PacketEncryptionType EncryptionType;


	explicit PacketEncrypted() = default;
	PacketEncrypted(std::vector<uint8_t> encryptedData, std::vector<uint8_t> Nonce, std::vector<uint8_t> Tag, PacketEncryptionType EncryptionType) : encryptedData(encryptedData), Nonce(Nonce), Tag(Tag), EncryptionType(EncryptionType) {};

	//std::vector<uint8_t> Decrypt(const PacketHelper& Packer, bool IsRemote = false, bool IsPrivate = false) {
	//	switch (EncryptionType) {
	//		case PacketEncryptionType::RSA: return Packer.EncryptionKeys.LocalRSAKeys->Decrypt(encryptedData);
	//		case PacketEncryptionType::ChaCha20Poly1305: return ChaChaCrypt::Decrypt(Packer.EncryptionKeys.ChaChaKey, encryptedData, Nonce, Tag);
	//	}

	//	return std::vector<uint8_t>();
	//}

	bool TryDecrypt(const SecurityKey& SecurityKey, std::vector<uint8_t>& decrypted, bool IsRemote = false, bool IsPrivate = false) {
		decrypted = Decrypt(SecurityKey, IsRemote, IsPrivate);

		if (!decrypted.empty()) return true;
		else return false;
	}

	std::vector<uint8_t> Decrypt(const SecurityKey& SecurityKey, bool IsRemote = false, bool IsPrivate = false) {
		switch (EncryptionType) {
		case PacketEncryptionType::RSA: return SecurityKey.LocalRSAKeys->Decrypt(encryptedData);
		case PacketEncryptionType::ChaCha20Poly1305: return ChaChaCrypt::Decrypt(SecurityKey.ChaChaKey, encryptedData, Nonce, Tag);
		}

		return std::vector<uint8_t>();
	}

	std::string ToJson() {
		return UTF8Helper::ToJSON({
			{"encryptedData", base64_encode(UTF8Helper::ToString(encryptedData)) },
			{"Nonce", base64_encode(UTF8Helper::ToString(Nonce)) },
			{"Tag", base64_encode(UTF8Helper::ToString(Tag)) },
			{"EncryptionType", static_cast<uint16_t>(EncryptionType)}
		});
	}

	static bool TryFromJson(const std::string& Json, PacketEncrypted& Packet) {

		auto* result = FromJson(Json);

		if (result == nullptr) return false;
		else {

			// * Dereferences the ptr
			Packet = *result; // Returns packet here as a refernce not a pointer
			delete result; // Deletes the ptr
			return true;
		}
	}

	static PacketEncrypted* FromJson(const std::string& Json) {
		using ordered = nlohmann::ordered_json;

		PacketEncrypted* p = new PacketEncrypted();

		try
		{
			ordered parsed = ordered::parse(Json);

			p->EncryptionType = static_cast<PacketEncryptionType>(parsed.at("EncryptionType").get<uint16_t>()); // need to parse as ushort uint16

			std::vector<uint8_t> encryptedData = UTF8Helper::ToVector(base64_decode(parsed.at("encryptedData").get<std::string>()));
			p->encryptedData = std::move(encryptedData);

			std::vector<uint8_t> Nonce = UTF8Helper::ToVector(base64_decode(parsed.at("Nonce").get<std::string>()));
			p->Nonce = std::move(Nonce);

			std::vector<uint8_t> Tag = UTF8Helper::ToVector(base64_decode(parsed.at("Tag").get<std::string>()));
			p->Tag = std::move(Tag);

			return p;
		}
		catch (const std::exception& e) { return nullptr; }
	}

	//static std::vector<uint8_t> Encrypt(const PacketHelper& Packer, std::vector<uint8_t> data, PacketEncryptionType EncryptionType, bool IsRemote) {
	//	PacketEncrypted encryptedPacket;
	//	std::vector<uint8_t> encryptedData;
	//	
	//	switch (EncryptionType) {
	//		case PacketEncryptionType::RSA: 
	//			if (IsRemote) encryptedData = Packer.EncryptionKeys.RemoteRSAPubKey->Encrypt(data);
	//			else encryptedData = Packer.EncryptionKeys.LocalRSAKeys->Encrypt(data);
	//			break;
	//		case PacketEncryptionType::ChaCha20Poly1305: 
	//			ChaChaKeys Keys;
	//			ChaChaCrypt::Encrypt(Packer.EncryptionKeys.ChaChaKey, data, Keys);
	//			encryptedPacket.Nonce = Keys.nonce;
	//			encryptedPacket.Tag = Keys.tag;
	//			break;
	//	}

	//	encryptedPacket.EncryptionType = EncryptionType;
	//	encryptedPacket.encryptedData = encryptedData;
	//	
	//	// Convert this packet to JSON, then into a vector
	//	return UTF8Helper::ToVector(encryptedPacket.ToJson());
	//}

	static std::vector<uint8_t> Encrypt(const SecurityKey& SecurityKey, std::vector<uint8_t> data, PacketEncryptionType EncryptionType, bool IsRemote) {
		PacketEncrypted encryptedPacket;
		std::vector<uint8_t> encryptedData;

		switch (EncryptionType) {
		case PacketEncryptionType::RSA:
			if (IsRemote) encryptedData = SecurityKey.RemoteRSAPubKey->Encrypt(data);
			else encryptedData = SecurityKey.LocalRSAKeys->Encrypt(data);
			break;
		case PacketEncryptionType::ChaCha20Poly1305:
			ChaChaKeys Keys;
			encryptedData = ChaChaCrypt::Encrypt(SecurityKey.ChaChaKey, data, Keys);
			encryptedPacket.Nonce = Keys.nonce;
			encryptedPacket.Tag = Keys.tag;
			break;
		}

		encryptedPacket.EncryptionType = EncryptionType;
		encryptedPacket.encryptedData = encryptedData;

		// Convert this packet to JSON, then into a vector
		return UTF8Helper::ToVector(encryptedPacket.ToJson());
	}
};