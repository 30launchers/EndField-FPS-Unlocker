#pragma once
#pragma once
#include <string>

// XOR加密解密函数
class XorString {
private:
    static constexpr char key = 0x5C9E17; // 加密密钥

public:
    // 编译时加密
    template<size_t N>
    static constexpr auto encrypt(const char(&str)[N]) {
        std::array<char, N> encrypted{};
        for (size_t i = 0; i < N; ++i) {
            encrypted[i] = str[i] ^ key;
        }
        return encrypted;
    }

    // 运行时解密
    static std::string decrypt(const char* encrypted, size_t len) {
        std::string decrypted;
        decrypted.reserve(len);
        for (size_t i = 0; i < len; ++i) {
            char c = encrypted[i] ^ key;
            // 如果遇到加密后的空终止符，停止解密
            if (c == '\0') break;
            decrypted += c;
        }
        return decrypted;
    }
};