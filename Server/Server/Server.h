#pragma once
#include "Stdfax.h"
#include "Client.h"
#include "Singleton.h"
#include "Room.h"
#include "HolePunchingHandler.h"
#include "PacketHandler.h"
#include <mutex>

typedef struct
{
    OVERLAPPED overlapped;
    WSABUF sizeBuf;
    WSABUF recvBuf;
    WSABUF sendBuf;
    int rwMode;
} IO_DATA;

class Server : public Singleton<Server>{
public:
    Server();

    ~Server();

    void run();

    void close();

    Client* connectToClient();

    void multicast(const char* message, int size);

    void multicastInRoom(Room& room, const char* message, int size);

    HANDLE getCompletionPort();

    SOCKET getSocket();

    vector<Client*> getClientsInGame();

    bool checkWaitingClient(Client client);

    Client* findClient(int clientId);

    void removeClient(Client& client);

    void IOWorkerThread();

    bool createWokerThread();

    void queueWorkerThread();

    bool createQueueWorkerThread();

    queue<pair<int, char*>> getRecvMsgQueue();
    void enqueueRecvMsgQueue(pair<int, char*> message);
    void dequeueRecvMsgQueue();
private:
    bool isRunning;
    WSADATA wsaData;
    SOCKET clntSock, servSock;
    SOCKADDR_IN clntAddr, servAddr;
    int clntAddrSize;

    u_long non_blocking_mode = 1;
    HANDLE compPort;

    vector<std::thread> IOWorkerThreads;
    vector<std::thread> queueWorkerThreads;
    vector<Client*> clientsInGame;

    queue<pair<int, char*>> recvMsgQueue;

    mutex serverMutex;
    mutex queueThreadMutex;
    mutex recvMsgQueueMutex;
};