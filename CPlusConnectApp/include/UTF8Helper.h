#pragma once
#include <string>
#include <vector>
#include "json.hpp"
#include <initializer_list>
#include <sstream>

class UTF8Helper
{
public:
	static std::string ToString(const char* UTF8bytes, int length) {
		std::string str(UTF8bytes, length);
		printf("string: %s\n", str.c_str());
		return str;
	}

	static std::string ToString(const std::vector<uint8_t>& bytes) {
		return std::string(bytes.begin(), bytes.end());
	}


	struct KeyValuePair {
		std::string key;
		nlohmann::json value;

		template <typename Value>
		KeyValuePair(const std::string& k, const Value& v) : key(k), value(v) {}
	};

	static std::string ToJSON(std::initializer_list<KeyValuePair> pairs) {
		nlohmann::ordered_json j;
		for (const auto& pair : pairs) { j[pair.key] = pair.value; }
			
		// Example 
		//std::string json = UTF8Helper::ToJSON({
		//	{"PeerId", Peer.Peer->PeerId.str()},
		//	{"PacketAction", static_cast<int>(PacketActionType::ACK)},
		//	{"PacketAction2", static_cast<int>(PacketActionType::ACK)}
		//});

		return j.dump().c_str(); // Added c_str() as its included with others to make it print out right
	}

	static nlohmann::json ToParsedJSON(std::string& UTF8String) {
		return nlohmann::json::parse(UTF8String);
	}

	static nlohmann::json ToParsedJSON(const char* UTF8bytes, int length) {
		return nlohmann::json::parse(UTF8Helper::ToString(UTF8bytes, length));
	}

	static std::vector<uint8_t> ToBytes(std::string& UTF8String) {
		return std::vector<uint8_t>(UTF8String.begin(), UTF8String.end());
	}

	
	static std::vector<std::string> Split(const std::string& str, char delimiter) {
		std::vector<std::string> tokens;
		std::stringstream ss(str);
		std::string token;

		while (std::getline(ss, token, delimiter)) {
			tokens.push_back(token);
		}

		return tokens;
	}
};

