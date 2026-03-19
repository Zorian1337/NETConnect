#pragma once
#include <cstdint> // used to get our byte types


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
            (static_cast<int64_t>(data[0]) >> 0) |
            (static_cast<int64_t>(data[1]) >> 8) |
            (static_cast<int64_t>(data[2]) >> 16) |
            (static_cast<int64_t>(data[3]) >> 24) |
            (static_cast<int64_t>(data[4]) >> 32) |
            (static_cast<int64_t>(data[5]) >> 40) |
            (static_cast<int64_t>(data[6]) >> 48) |
            (static_cast<int64_t>(data[7]) >> 56);

        return result;
    };

    static std::array<uint8_t, 4> WriteInt32LittleEndian(int32_t data) {
        return {{
                static_cast<uint8_t>(data >> 0),
                static_cast<uint8_t>(data >> 8),
                static_cast<uint8_t>(data >> 16),
                static_cast<uint8_t>(data >> 24)
        }};
        //std::vector<uint8_t> buffer(4);
        //buffer[0] = static_cast<uint8_t>(data >> 0);
        //buffer[1] = static_cast<uint8_t>(data >> 8);
        //buffer[2] = static_cast<uint8_t>(data >> 16);
        //buffer[3] = static_cast<uint8_t>(data >> 24);
        //return buffer;
    }

    static std::array<uint8_t, 2> WriteUInt16LittleEndian(uint16_t data) {
        //uint8_t* buffer;
        //buffer[0] = static_cast<uint8_t>(data >> 0);
        //buffer[1] = static_cast<uint8_t>(data >> 8);
        //return buffer;
        return {{
            static_cast<uint8_t>(data >> 0),
            static_cast<uint8_t>(data >> 8),
        }};
    }

    static std::array<uint8_t, 8> WriteInt64LittleEndian(int64_t data) {
        return {{
            static_cast<uint8_t>(data >> 0),
            static_cast<uint8_t>(data >> 8),
            static_cast<uint8_t>(data >> 16),
            static_cast<uint8_t>(data >> 24),
            static_cast<uint8_t>(data >> 32),
            static_cast<uint8_t>(data >> 40),
            static_cast<uint8_t>(data >> 48),
            static_cast<uint8_t>(data >> 56)
        }};
        //uint8_t* buffer;
        //buffer[0] = static_cast<uint8_t>(data >> 0);
        //buffer[1] = static_cast<uint8_t>(data >> 8);
        //buffer[2] = static_cast<uint8_t>(data >> 16);
        //buffer[3] = static_cast<uint8_t>(data >> 24);
        //buffer[4] = static_cast<uint8_t>(data >> 32);
        //buffer[5] = static_cast<uint8_t>(data >> 40);
        //buffer[6] = static_cast<uint8_t>(data >> 48);
        //buffer[7] = static_cast<uint8_t>(data >> 56);
        //return buffer;
    }
};