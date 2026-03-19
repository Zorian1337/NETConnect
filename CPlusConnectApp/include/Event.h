#pragma once

#include <iostream>
#include <vector>
#include <functional>
#include <algorithm> // for std::remove
#include <mutex>  // Helps with thread safety
#include <thread> // Added for async
#include "ThreadPool.h"

// Generic Event class
template <typename... Args>
class Event
{
public:
	using Callback = std::function<void(Args...)>;

	Event(ThreadPool& pool) : threadPool(pool) {}

	Event() : threadPool(nullptr) {}

	// Thread safe
	void operator+=(Callback cb) {
		std::lock_guard<std::mutex> lock(mutex);
		listeners.push_back(cb);
	}

	template<typename T>
	void Subscribe(T* instance, void (T::* memberFunction)(Args...)) {
		std::lock_guard<std::mutex> lock(mutex);
		listeners.push_back([instance, memberFunction](Args... args) {
			(instance->*memberFunction)(args...);
			});
	}

	// Doesnt support removal of lambdas
	void operator-=(Callback cb) {
		std::lock_guard<std::mutex> lock(mutex);
		listeners.erase(
			std::remove_if(listeners.begin(), listeners.end(),
				[&](const Callback& f) {
					return f.target_type() == cb.target_type() &&
						f.target<void(*)(Args...)>() == cb.target<void(*)(Args...)>();
				}),
			listeners.end()
		);
	}

	// Runs in sync
	void Invoke(Args... args) {
		std::vector<Callback> currentListeners;
		{
			std::lock_guard<std::mutex> lock(mutex); 
			currentListeners = listeners;
		}

		for (auto& cb : currentListeners)
			cb(args...);
	}

	// Runs Async
    void InvokeAsync(Args... args) 
	{
		std::vector<Callback> currentListeners;
		{
			std::lock_guard<std::mutex> lock(mutex); 
			currentListeners = listeners;
		}

        for (auto& cb : currentListeners) {

			// Uses thread pools 
			threadPool->enqueue([cb, args...]() {
				cb(args...);
				});

            // Launch each listener in its own thread
            //std::thread([cb, args...]() {
            //    cb(args...);
            //}).detach();
		}
	}
	
private:
	std::vector<std::function<void(Args...)>> listeners;
	std::mutex mutex; // Mutex to protect listeners vector
	ThreadPool* threadPool;; // Reference to the thread pool for async invocation
};

