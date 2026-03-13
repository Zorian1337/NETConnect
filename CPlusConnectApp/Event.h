#pragma once

#include <iostream>
#include <vector>
#include <functional>
#include <algorithm> // for std::remove


// Generic Event class
template <typename... Args>
class Event
{
public:
	using Callback = std::function<void(Args...)>;

	// Removed for C# style 
	//void subscribe(std::function<void(Args...)> callback) {
	//	listeners.push_back(callback);
	//}

	void operator+=(Callback cb) {
		listeners.push_back(cb);
	}

	template<typename T>
	void Subscribe(T* instance, void (T::* memberFunction)(Args...)) {
		listeners.push_back([instance, memberFunction](Args... args) {
			(instance->*memberFunction)(args...);
			});
	}

	// Doesnt support removal of lambdas
	void operator-=(Callback cb) {
		listeners.erase(
			std::remove_if(listeners.begin(), listeners.end(),
				[&](const Callback& f) {
					return f.target_type() == cb.target_type() &&
						f.target<void(*)(Args...)>() == cb.target<void(*)(Args...)>();
				}),
			listeners.end()
		);
	}

	void Invoke(Args... args) {
		for (auto& cb : listeners)
			cb(args...);
	}
private:
	std::vector<std::function<void(Args...)>> listeners;
};

