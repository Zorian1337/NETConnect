#pragma once
#include <cstdint> // used to get our uint16_t types


class Endian {

public:
    static int32_t ReadInt32LittleEndian(const uint8_t* data, size_t length) {
        // if not size of an interger return 0xFFFFFFFF as error (all bits set to 1)
        if (length != 4) return 0xFFFFFFFF;

        int32_t result = static_cast<int32_t>(data[0]) |
            static_cast<int32_t>(data[1]) << 8 |
            static_cast<int32_t>(data[2]) << 16 |
            static_cast<int32_t>(data[3]) << 24;
        return result;

    };

    static uint16_t ReadUInt16LittleEndian(const uint8_t* data, size_t length) {
        if (length < 2 || !data) return 0xFFFF;  // 0xFFFF = all bits set for uint16_t

        uint16_t result =
            (static_cast<uint16_t>(data[0]) << 0) |
            (static_cast<uint16_t>(data[1]) << 8);

        return result;
    };

    static int64_t ReadInt64LittleEndian(const uint8_t* data, size_t length) {
        if (length < 8 || !data) return 0xFFFFFFFFFFFFFFFF;  // All bits set for 64-bit

        int64_t result =
            (static_cast<int64_t>(data[0]) << 0) |
            (static_cast<int64_t>(data[1]) << 8) |
            (static_cast<int64_t>(data[2]) << 16) |
            (static_cast<int64_t>(data[3]) << 24) |
            (static_cast<int64_t>(data[4]) << 32) |
            (static_cast<int64_t>(data[5]) << 40) |
            (static_cast<int64_t>(data[6]) << 48) |
            (static_cast<int64_t>(data[7]) << 56);

        return result;
    };



};