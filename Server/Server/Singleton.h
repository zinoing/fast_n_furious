#pragma once
#include "System.h"
#include <mutex>

template <typename T>
class Singleton : public System {
private:
	static T* instance;
	static mutex singletonMutex;

protected:
	Singleton() {};

	~Singleton() {
		delete instance;
		instance = nullptr;
	}

public:
	static T& Instance() {
		singletonMutex.lock();
		if (instance == nullptr) {
			instance = new T();
		}
		singletonMutex.unlock();
		return *instance;
	}

	static void destroyInstance() {
		delete instance;
		instance = nullptr;
	}
};

template <typename T>
T* Singleton<T>::instance = nullptr;
template <typename T>
mutex Singleton<T>::singletonMutex;