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

	std::unique_ptr<RSAKeyManager> LocalRSAKeys;
	std::unique_ptr<RSAKeyManager> RemoteRSAPubKey;

	void GenerateLocalRSAKeys(RSAKeySize RSAKeySize) {

		// Init our LocalRSAKey
		LocalRSAKeys = std::make_unique<RSAKeyManager>(RSAKeySize);
	};

	void SetRemoteRSAKey() {
	
	}

	// use a more dynamic return type, RSAKeyManager isnt going to be the only security key!!!
	RSAKeyManager* GetSecurityKey(PacketEncryptionType EncryptionType, bool IsRemote = false, bool IsPrivate = false) {
		return nullptr;
	}
};