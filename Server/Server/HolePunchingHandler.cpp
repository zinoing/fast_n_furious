#include "HolePunchingHandler.h"
#include "PacketHandler.h"
#include "Server.h"
#include "Client.h"
#include "System.h"
#include <thread>

HolePunchingHandler::HolePunchingHandler()
    : firstUDPSock(NULL), secondUDPSock(NULL), epOfFirstSend(nullptr), epOfSecondSend(nullptr), isSet(false), isRunning(false) {
    memset(firstBuffer, 0, BUF_SIZE);
    memset(secondBuffer, 0, BUF_SIZE);
}

HolePunchingHandler::HolePunchingHandler(const HolePunchingHandler& holePunchingHandler)
    : firstUDPSock(NULL), secondUDPSock(NULL), epOfFirstSend(nullptr), epOfSecondSend(nullptr), isSet(false), isRunning(false) {
    memset(firstBuffer, 0, BUF_SIZE);
    memset(secondBuffer, 0, BUF_SIZE);

    firstUDPSock = holePunchingHandler.firstUDPSock;
    secondUDPSock = holePunchingHandler.secondUDPSock;
    firstUDPSockAddr = holePunchingHandler.firstUDPSockAddr;
    secondUDPSockAddr = holePunchingHandler.secondUDPSockAddr;
    epOfFirstSend = holePunchingHandler.epOfFirstSend;
    epOfSecondSend = holePunchingHandler.epOfSecondSend;
}

HolePunchingHandler::~HolePunchingHandler() {}

void HolePunchingHandler::initializeSockets() {
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
        ErrorHandling("WSAStartup()");
    }
    // Initialize firstUDPSock & secondUDPSock
    if ((firstUDPSock = WSASocket(PF_INET, SOCK_DGRAM, IPPROTO_UDP, NULL, 0, WSA_FLAG_OVERLAPPED)) == SOCKET_ERROR) {
        ErrorHandling("WSASocket()");
    }

    if ((secondUDPSock = WSASocket(PF_INET, SOCK_DGRAM, IPPROTO_UDP, NULL, 0, WSA_FLAG_OVERLAPPED)) == SOCKET_ERROR) {
        ErrorHandling("WSASocket()");
    }

    // Initialize firstUDPSockAddr & secondUDPSockAddr
    memset(&firstUDPSockAddr, 0, sizeof(firstUDPSockAddr));
    firstUDPSockAddr.sin_addr.s_addr = inet_addr(SERV_IP);
    firstUDPSockAddr.sin_family = AF_INET;
    firstUDPSockAddr.sin_port = htons(SERV_PORT + 1);
    firstUDPSockAddrSize = sizeof(firstUDPSockAddr);

    memset(&secondUDPSockAddr, 0, sizeof(firstUDPSockAddr));
    secondUDPSockAddr.sin_addr.s_addr = inet_addr(SERV_IP);
    secondUDPSockAddr.sin_family = AF_INET;
    secondUDPSockAddr.sin_port = htons(SERV_PORT + 2);
    secondUDPSockAddrSize = sizeof(secondUDPSockAddr);

    // set socket option
    u_long non_blocking_mode = 1;
    if (ioctlsocket(firstUDPSock, FIONBIO, &non_blocking_mode) != 0) {
        ErrorHandling("ioctlsocket()");
    }

    if (ioctlsocket(secondUDPSock, FIONBIO, &non_blocking_mode) != 0) {
        ErrorHandling("ioctlsocket()");
    }

    // bind()
    if (bind(firstUDPSock, (SOCKADDR*)&firstUDPSockAddr, sizeof(firstUDPSockAddr)) == SOCKET_ERROR) {
        ErrorHandling("bind()");
    }

    if (bind(secondUDPSock, (SOCKADDR*)&secondUDPSockAddr, sizeof(secondUDPSockAddr)) == SOCKET_ERROR) {
        ErrorHandling("bind()");
    }

    isRunning = true;
}

void HolePunchingHandler::beginReceive() {
    std::thread firstRecvThread([&]() {
        recv(&firstUDPSock, firstBuffer, &firstUDPSockAddrSize, &isRunning);
        });

    std::thread secondRecvThread([&]() {
        recv(&secondUDPSock, secondBuffer, &secondUDPSockAddrSize, &isRunning);
        });

    firstRecvThread.detach();
    secondRecvThread.detach();
}

void HolePunchingHandler::recv(SOCKET* socket, char* buffer, int* addrSize, bool* isRunning) {
    while (*isRunning) {
        sockaddr_in senderAddr;
        int senderAddrSize = sizeof(senderAddr);
        memset(&senderAddr, 0, senderAddrSize);
        int bytesReceived = recvfrom(*socket, buffer, BUF_SIZE, 0, (SOCKADDR*)&senderAddr, &senderAddrSize);

        if (bytesReceived == SOCKET_ERROR) {
            int error = WSAGetLastError();
            if (error != WSAEWOULDBLOCK) {
                wprintf(L"recvfrom failed with error %d\n", error);
                continue;
            }
        }

        if (bytesReceived > 0) {
            // holePunchingMutex.lock();
            
            // get the sender's public end point
            EndPoint ep;
            ep.ipAddress = inet_ntoa(senderAddr.sin_addr);
            ep.portNumber = to_string(ntohs(senderAddr.sin_port));
            ep.fullEP = ep.ipAddress + ":" + ep.portNumber;

            // find the client with clientID
            int clientId;
            std::memcpy(&clientId, buffer + 2, sizeof(int));

            Client* client = Server::Instance().findClient(clientId);

            // if all set, check NAT type
            if (client->getUDPSockPublicUdpPort() != 0) {
                checkNATType(*client, ep);

                char* packet = new char[bytesReceived];
                memset(packet, 0, bytesReceived);
                memcpy(packet, buffer, bytesReceived);

                Server::Instance().enqueueRecvMsgQueue(make_pair(clientId, packet));
            }
            else {
                client->setUDPSockPublicEP(ep);
            }
            continue;
        }
    }
}

bool HolePunchingHandler::isAllSet() {
    return isSet;
}

void HolePunchingHandler::checkNATType(Client& client, EndPoint secondPublicEP) {

    // compare sender's public portNumber
    // if same, the NAT type is non_symmetric
    if (client.getUDPSockPublicEP().portNumber.compare(secondPublicEP.portNumber) == 0) {
        client.setNATType(NAT_TYPE::NON_SYMMETRIC);
    }
    // if not, the NAT type is symmetric
    else
    {
        client.setNATType(NAT_TYPE::SYMMETRIC);
    }
}