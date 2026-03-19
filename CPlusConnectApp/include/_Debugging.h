// Crossplatform supported debug output for visualization

#pragma once

#include <iostream> // allows use of std::string (in windows, and ps4)
#include <string>   // smart to still include this for std::string

#ifdef _WIN32
#include <stdio.h> // printf - Base C support
#endif 

#ifdef _PS4
#include "_PS4.h" // Include PS4 specific headers for standard includes (IE Debug Stream)
#endif 



class Debugger {

public:
	static void WriteLine(const std::string Message) {
		std::string Modified = "[DEBUG] " + Message + "\n"; // Add a prefix and newline for better visualization

		#ifdef _PS4
		DEBUGLOG << Modified; // PS4 Debug Stream	
		#endif 

		#ifdef _WIN32
		printf(Modified.c_str());
		#endif 

	}

	static void WriteError(const std::string Message) {
		std::string Modified = "[ERROR] " + Message;

		#ifdef _PS4
		Modified += strerror(errno) + "\n";
		DEBUGLOG << Modified; // PS4 Debug Stream	
		#endif 

		#ifdef _WIN32
		Modified += "\n"; // fix this later as apparently WSAGetLastError is specifically for sockets and not all errors removed [WSAGetLastError() +]
		printf(Modified.c_str()); //
		#endif 

	}

};