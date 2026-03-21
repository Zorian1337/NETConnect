#pragma once

//#include "cryptopp890/rsa.h"
//#include "cryptopp890/queue.h"
//#include "cryptopp890/osrng.h"
#include "cryptopp890/rsa.h"
#include "cryptopp890/osrng.h"
#include "cryptopp890/pssr.h"
#include "cryptopp890/oaep.h"
#include "cryptopp890/files.h"
#include "cryptopp890/filters.h"
#include "cryptopp890/base64.h"
#include <vector>
#include <stdexcept>
#include <cstdint>
#include "CryptUtils.h"

// Global
#include "_Debugging.h"

#pragma comment(lib, "cryptlib.lib") // only works for windows, need linux/ps4 version to include aswell

using namespace CryptoPP;

namespace RSACrypt {
	enum class RSAKeySize : uint32_t
	{
		Deprecated = 1024,
		Standard = 2048,
		Strong = 3072,
		VeryStrong = 4096,
		VerySecure = 8192
	};

	// Created a key manager class to prevent recreation of rng and our encryptor/decryptors

	class RSAKeyManager {
	private:
		AutoSeededRandomPool& rng = CryptUtils::GetRNG();
		RSA::PrivateKey PrivateKey;
		RSA::PublicKey PublicKey;


		// Encrypts our data with SHA256 - created variable named encryptor to handle encryption
		RSAES<OAEP<SHA256>>::Encryptor encryptor;

		// decrypts our data with SHA256 - created variable named decryptor to handle decryption
		RSAES<OAEP<SHA256>>::Decryptor decryptor;

	public:

		// Creates our Keys and encryptors immediately for future use
		RSAKeyManager(RSAKeySize KeySize = RSAKeySize::VeryStrong) {

			// Sets private and public keys, then inits our encryptor/decryptor
			if (CreateKeys(KeySize, PrivateKey, PublicKey)) {
				encryptor = RSAES<OAEP<SHA256>>::Encryptor(PublicKey);
				decryptor = RSAES<OAEP<SHA256>>::Decryptor(PrivateKey);
			}
			// Output some error saying keys werent generated properly 
			else {}
		};

		RSAKeyManager(const std::vector<uint8_t>& RSAKey) {

			bool IsPrivateKey = false;

			try {
				RSA::PrivateKey _privKey;
				StringSource ss(RSAKey.data(), RSAKey.size(), true);
				_privKey.Load(ss);

				if (_privKey.Validate(rng, 3)) { 
					PrivateKey = _privKey; 
					IsPrivateKey = true; 

					// Creates our PublicKey using our PrivateKey
					CryptoPP::RSA::PublicKey publicKey(PrivateKey);
					PublicKey = publicKey;

					encryptor = RSAES<OAEP<SHA256>>::Encryptor(PublicKey);
					decryptor = RSAES<OAEP<SHA256>>::Decryptor(PrivateKey);
					return;
				}
			}
			catch (...) {}

			if (IsPrivateKey) return;

			try {
				RSA::PublicKey _pubKey;
				StringSource ss(RSAKey.data(), RSAKey.size(), true);
				_pubKey.Load(ss);

				if (_pubKey.Validate(rng, 3)) { 
					// Sets our PublicKey but all we can do is Encrypt - Make sure to prevent any sort of Decrypting if our PrivateKey isnt valid!!
					PublicKey = _pubKey; 
					encryptor = RSAES<OAEP<SHA256>>::Encryptor(PublicKey);
					Debugger::WriteLine("Set only our RSAPublicKey");

					return; 
				}
			}
			catch (...) {}

			throw std::runtime_error("Failed to load RSA key: Invalid format or corrupted key");
		}

		// Creates, validates and outputs our keys for use later on
		bool CreateKeys(RSAKeySize KeySize, RSA::PrivateKey& PrivateKey, RSA::PublicKey& PublicKey) {
			PrivateKey.GenerateRandomWithKeySize(rng, static_cast<unsigned int>(KeySize));
			PublicKey = RSA::PublicKey(PrivateKey);

			// Validate both of our keys
			if (!PrivateKey.Validate(rng, 3) || !PublicKey.Validate(rng, 3)) return false;
			else return true;
		};

		std::vector<uint8_t> Encrypt(std::vector<uint8_t> data) {
			std::vector<uint8_t> ciphertext;

			// Encrypts our data right here, and outputs as ciphertext
			VectorSource(data, true, new PK_EncryptorFilter(rng, encryptor, new VectorSink(ciphertext)));

			return ciphertext;
		};

		std::vector<uint8_t> Decrypt(std::vector<uint8_t> ciphertext) {
			std::vector<uint8_t> plaintext;

			// Decrypts our data right here, and outputs as plainttext -copied from Encrypt
			VectorSource(ciphertext, true, new PK_DecryptorFilter(rng, decryptor, new VectorSink(plaintext))); //new PK_EncryptorFilter(rng, encryptor, new VectorSink(plaintext)

			return plaintext;
		};
	};
};
