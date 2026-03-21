#pragma once

#include "cryptopp890/chachapoly.h"
#include "cryptopp890/osrng.h"
#include "cryptopp890/hex.h"
#include <vector>
#include <string>
#include <stdexcept>
#include "CryptUtils.h"


#pragma comment(lib, "cryptlib.lib") // only works for windows, need linux/ps4 version to include aswell

using namespace CryptoPP;


class ChaChaKeys {
public:
    std::vector<uint8_t> Key;
    std::vector<uint8_t> nonce;
    std::vector<uint8_t> tag;

    ChaChaKeys(std::vector<uint8_t> Key, std::vector<uint8_t> nonce, std::vector<uint8_t> tag) : Key(Key), nonce(nonce), tag(tag) {}
    ChaChaKeys(std::vector<uint8_t> nonce, std::vector<uint8_t> tag) : nonce(nonce), tag(tag) {}
};

// We are using ChaCha20Poly1305
class ChaCha {
public:

    static const size_t KEY_SIZE = 32;      // 256-bit key
    static const size_t NONCE_SIZE = 12;    // 96-bit nonce (IETF standard)
    static const size_t TAG_SIZE = 16;      // 128-bit authentication tag


    AutoSeededRandomPool& rng = CryptUtils::rng;

    static std::vector<uint8_t> Encrypt(const std::vector<uint8_t>& Key, const std::vector<uint8_t>& plaintext, ChaChaKeys& Keys, const std::vector<uint8_t>& aad = {}) {
        //std::vector<uint8_t> tag = CryptUtils::GenerateRandom(TAG_SIZE); - generated automatically
        std::vector<uint8_t> nonce = CryptUtils::GenerateRandom(NONCE_SIZE);
        
        ChaCha20Poly1305::Encryption Encryptor;
        Encryptor.SetKeyWithIV(Key.data(), Key.size(), nonce.data(), nonce.size());

        // Prepare buffers
        std::vector<uint8_t> ciphertext(plaintext.size());
        std::vector<uint8_t> tag(TAG_SIZE);

        // Encrypts our data and stores it into ciphertext
        Encryptor.EncryptAndAuthenticate(ciphertext.data(), tag.data(), tag.size(), nonce.data(), nonce.size(), aad.data(), aad.size(), plaintext.data(), plaintext.size());

        // Check to see if our data is valid
        if (ciphertext.empty() || ciphertext.size() != plaintext.size()) {
            // Check our tag data for zeros 

            // right now I dont care, if it gets here its basically dead to me anyway
            
            throw std::runtime_error("Failed to encrypt data with ChaCha20Poly-1305");
        }

        // Setup our output data 
        Keys = ChaChaKeys(tag, nonce); 
        return ciphertext;
    }

    static std::vector<uint8_t> Decrypt(const std::vector<uint8_t>& Key, const std::vector<uint8_t>& ciphertext, const std::vector<uint8_t>& nonce, const std::vector<uint8_t>& tag, const std::vector<uint8_t>& aad = {}) {
        ChaCha20Poly1305::Decryption decryptor;
        decryptor.SetKeyWithIV(Key.data(), Key.size(), nonce.data(), nonce.size());

        // Prepare plaintext buffer
        std::vector<uint8_t> plaintext(ciphertext.size());

        bool verified = decryptor.DecryptAndVerify(plaintext.data(), tag.data(), tag.size(), nonce.data(), nonce.size(), aad.data(), aad.size(), ciphertext.data(), ciphertext.size());

        if (!verified) throw std::runtime_error("Decryption failed: Authentication tag mismatch - data may be corrupted or tampered");

        return plaintext;
    }
};