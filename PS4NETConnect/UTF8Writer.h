#include <string>
#include <iostream>
#include <unistd.h>

#pragma once
class UTF8Writer
{
public:
	static ssize_t sendMessage(int connfd, const std::string& UTF8String) {
		return write(connfd, UTF8String.c_str(), UTF8String.size());
	}
};

