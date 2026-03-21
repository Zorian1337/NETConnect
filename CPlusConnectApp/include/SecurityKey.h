#pragma once

#include "RSA.h"
#include <memory>
#include "PacketHeader.h"
#include <vector>
#include "simple_base64.h"

using namespace RSACrypt;
class SecurityKey {
public:

	// Creates LocalRSAKey by default
	SecurityKey() {
	
		GenerateLocalRSAKeys(RSAKeySize::VeryStrong);
	}

	std::unique_ptr<RSAKeyManager> LocalRSAKeys;
	std::unique_ptr<RSAKeyManager> RemoteRSAPubKey;
	std::vector<uint8_t> ChaChaKey;

	void GenerateLocalRSAKeys(RSAKeySize RSAKeySize) {

		// Init our LocalRSAKey
		LocalRSAKeys = std::make_unique<RSAKeyManager>(RSAKeySize);
	};

	void SetRemoteRSAKey(std::vector<uint8_t> RSAPublicKey) {
		// Check if bytes are base64 or not

		// If they are not base64 convert it to base64

		// If it is then do nothing and continue
		

		// Im lazy so im just gonna convert the RSAKey back to base64 as its most likely not anyway
		
		std::vector<uint8_t> base64Key = FromBase64(ToBase64(RSAPublicKey));
		RemoteRSAPubKey = std::make_unique<RSAKeyManager>(base64Key);
	}

	// use a more dynamic return type, RSAKeyManager isnt going to be the only security key!!!
	RSAKeyManager* GetSecurityKey(PacketEncryptionType EncryptionType, bool IsRemote = false, bool IsPrivate = false) {
		return nullptr;
	}
};