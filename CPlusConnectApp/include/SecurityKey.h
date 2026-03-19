#pragma once

#include "RSA.h"
#include <memory>
#include "PacketHeader.h"


using namespace RSACrypt;
class SecurityKey {
public:

	// Creates LocalRSAKey by default
	SecurityKey() {
	
		GenerateLocalRSAKeys(RSAKeySize::VeryStrong);
	}

	std::unique_ptr<KeyManager> LocalRSAKeys;
	std::unique_ptr<KeyManager> RemoteRSAPubKey;

	void GenerateLocalRSAKeys(RSAKeySize RSAKeySize) {

		// Init our LocalRSAKey
		LocalRSAKeys = std::make_unique<KeyManager>(RSAKeySize);
	};

	void SetRemoteRSAKey() {
	
	}

	KeyManager* GetSecurityKey(PacketEncryptionType EncryptionType, bool IsRemote = false, bool IsPrivate = false) {
		return nullptr;
	}
};