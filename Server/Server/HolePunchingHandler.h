#pragma once
#include "Singleton.h"
#include "NetworkOption.h"
#include <mutex>
#include <vector>

class Client;

class HolePunchingHandler : public Singleton<HolePunchingHandler> {
public:

    HolePunchingHandler();
    HolePunchingHandler(const HolePunchingHandler& holePunchingHandler);
    ~HolePunchingHandler();

    void initializeSockets();
    void beginReceive();
    void recv(SOCKET* socket, char* buffer, int* addrSize, bool* isRunning);
    bool isAllSet();
    void checkNATType(Client& client, EndPoint secondPublicEP);

private:
    WSADATA wsaData;
    SOCKET firstUDPSock, secondUDPSock;
    SOCKADDR_IN firstUDPSockAddr, secondUDPSockAddr;
    int firstUDPSockAddrSize, secondUDPSockAddrSize;

    EndPoint* epOfFirstSend;
    EndPoint* epOfSecondSend;

    char firstBuffer[BUF_SIZE];
    char secondBuffer[BUF_SIZE];
    bool isSet;

    bool isRunning;
    std::mutex holePunchingMutex;
};