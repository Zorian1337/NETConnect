#pragma once
#include "cryptopp890/osrng.h"

#pragma comment(lib, "cryptlib.lib") // only works for windows, need linux/ps4 version to include aswell

using namespace CryptoPP;


class CryptUtils {
public:

	// Original RNG seeded pool so we can pass this to everything that needs it 
	static AutoSeededRandomPool rng;

	static std::vector<uint8_t> GenerateRandom(int Length) {
		std::vector<uint8_t> nonce(Length);
		rng.GenerateBlock(nonce.data(), nonce.size());
		return nonce;
	}
};