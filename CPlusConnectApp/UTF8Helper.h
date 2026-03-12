#pragma once
#include <string>
#include <vector>
#include "json.hpp"
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

