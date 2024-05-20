#include "Server.h"
#include "RoomManager.h"
#include "PacketHandler.h"
#include "PacketManager.h"
#include "SuperPeerScouter.h"
#include "Client.h"
#include <vector>
#include <algorithm>
#include <utility> 

Server::Server()
{
    // 윈속 초기화
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
        ErrorHandling("WSAStartup()");
    }

    // I/O(입출력) 완료 포트 생성
    compPort = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);
    if (compPort == INVALID_HANDLE_VALUE) {
        ErrorHandling("CreateIoCompletionPort()");
    }

    // TCP 연결 가능 소켓 생성
    if ((servSock = WSASocket(PF_INET, SOCK_STREAM, 0, NULL, 0, WSA_FLAG_OVERLAPPED)) == SOCKET_ERROR) {
        ErrorHandling("WSASocket()");
    }
    
    // 소켓을 non blocking 모드로 변경
    if (ioctlsocket(servSock, FIONBIO, &non_blocking_mode) != 0) {
        ErrorHandling("ioctlsocket()");
    }

    // Initialize servAddr
    memset(&servAddr, 0, sizeof(servAddr));
    servAddr.sin_addr.s_addr = inet_addr(SERV_IP);
    servAddr.sin_family = AF_INET;
    servAddr.sin_port = htons(SERV_PORT);

    // bind
    if (bind(servSock, (SOCKADDR*)&servAddr, sizeof(servAddr)) == SOCKET_ERROR) {
        ErrorHandling("bind()");
    }

    // listen
    if (listen(servSock, 5) == SOCKET_ERROR)
        ErrorHandling("listen()");

    clntAddrSize = sizeof(clntAddr);
    printf("server initialization completed.\n");
}

Server::~Server() {}

void Server::run() {
    isRunning = true;
    bool check = createWokerThread();
    if (!check)
        return;
    check = createQueueWorkerThread();
    if (!check)
        return;
    HolePunchingHandler::Instance().initializeSockets();
    HolePunchingHandler::Instance().beginReceive();
}

void Server::close() {
    isRunning = false;

    vector<Client*>().swap(clientsInGame);

    closesocket(servSock);
    printf("server closed\n");
    WSACleanup();
}

Client* Server::connectToClient() {
    clntSock = accept(servSock, (SOCKADDR*)&clntAddr, &clntAddrSize);
    if (clntSock == INVALID_SOCKET) {
        if (WSAGetLastError() == WSAEWOULDBLOCK) {
            return nullptr;
        }
        else{
            printf("%d\n", WSAGetLastError());
            ErrorHandling("accept()");
            return nullptr;
        }
    }
    struct linger ling = { 1, 0 };
    //ling.l_onoff = 1;
    //ling.l_linger = 0;
    setsockopt(clntSock, SOL_SOCKET, SO_LINGER, (char*)&ling, sizeof(ling));

    Client* client = new Client(clntSock, clntAddr);
    client->setConnectionStatus(ConnectionStatus::CONNECTED);
    clientsInGame.push_back(client); // Add client
    printf("id: %d | connected\n", client->getClientID());
    printf("id: %d | tcp port: %d\n", client->getClientID(), ntohs(clntAddr.sin_port));

    RoomManager::Instance().updateRooms(*client);
    return client;
}

void Server::multicast(const char* message, int size) {
    for (int i = 0; i < clientsInGame.size(); i++) {
        send(clientsInGame[i]->getSocket(), message, size, 0);
    }
    printf("multicast completed\n");
    return;
}

void Server::multicastInRoom(Room& room, const char* message, int size) {
    for (int i = 0; i < room.getPlayers().size(); i++) {
        send(room.getPlayers()[i]->getSocket(), message, size, 0);
    }
    printf("multicast in room completed\n");
    return;
}

HANDLE Server::getCompletionPort() {
    return compPort;
}

SOCKET Server::getSocket() {
    return servSock;
}

vector<Client*> Server::getClientsInGame() {
    return clientsInGame;
}

bool Server::checkWaitingClient(Client client) {
    for (int i = 0; i < clientsInGame.size(); i++) {
        if (clientsInGame[i]->getClientID() == client.getClientID())
            return true;
    }
    return false;
}

Client* Server::findClient(int clientId) {
    for (int i = 0; i < clientsInGame.size(); i++) {
        if (clientsInGame[i]->getClientID() == clientId)
            return clientsInGame[i];
    }
    return nullptr;
}

void Server::removeClient(Client& client) {
    serverMutex.lock();

    clientsInGame.erase(remove_if(clientsInGame.begin(), clientsInGame.end(),
        [&client](Client* otherClient) {
            return client.getClientID() == otherClient->getClientID();
        }), clientsInGame.end());

    delete& client;

    serverMutex.unlock();
    return;
}

void Server::IOWorkerThread() {
    DWORD bytesTransfered = 0;
    Client* client = nullptr;
    IO_DATA* ioData = nullptr;

    try {
        while (isRunning) {
            client = nullptr;
            bytesTransfered = 0;

            bool result = GetQueuedCompletionStatus(
                compPort,
                &bytesTransfered,
                (PULONG_PTR)&client,
                (LPOVERLAPPED*)&ioData,
                INFINITE);

            if (result == false || (bytesTransfered == 0 && result == true)) {
                if (client->getConnectionStatus() == ConnectionStatus::DISCONNECTED)
                    continue;

                client->setConnectionStatus(ConnectionStatus::DISCONNECTED);
                printf("closed socket which has port num: %d\n", ntohs(client->getSocketAddress().sin_port));
                closesocket(client->getSocket());
                removeClient(*client);
                delete ioData;
                ioData = nullptr;
                continue;
            }

            switch (ioData->rwMode) {
            case RECV_PACKET:
            {
                printf("id: %d | [receivePacket] [tag: %d] [size: %d]\n",
                    client->getClientID(), ioData->recvBuf.buf[1], bytesTransfered);

                char* packet = new char[bytesTransfered];
                memset(packet, 0, bytesTransfered);
                memcpy(packet, ioData->recvBuf.buf, bytesTransfered);

                enqueueRecvMsgQueue(make_pair(client->getClientID(), packet));

                client->receivePacket();
                delete ioData;
                ioData = nullptr;
                break;
            }
            case SEND:
                printf("id: %d | [sendMessage] [tag: %d] [size: %d]\n", client->getClientID(), (int)ioData->sendBuf.buf[1], (int)ioData->sendBuf.buf[0]);
                delete ioData;
                ioData = nullptr;
                break;
            }
        }
    }
    catch (exception e) {
        ErrorHandling(e.what());
    }
    return;
}

bool Server::createWokerThread()
{
    for (int i = 0; i < MAX_IO_WORKER_THREAD; i++)
    {
        IOWorkerThreads.emplace_back([this]() { IOWorkerThread(); });
    }
    return true;
}

void Server::queueWorkerThread() {
    while (isRunning) {
        // 처리 되지 않은 작업이 존재할 경우
        queueThreadMutex.lock();
        if (getRecvMsgQueue().size() > 0) {
            // findClient와 handlePacket 작업이 지체가 될 수 있으므로 message를 복사한 다음 mutex를 unlock한다.
            pair<int, char*> message = getRecvMsgQueue().front();
            dequeueRecvMsgQueue();

            queueThreadMutex.unlock();

            Client& client = *Server::Instance().findClient(message.first);
            PacketHandler::Instance().handlePacket(client, message.second);
        }
        else {
            queueThreadMutex.unlock();
        }
    }
}

bool Server::createQueueWorkerThread()
{
    for (int i = 0; i < MAX_QUEUE_WORKER_THREAD; i++)
    {
        queueWorkerThreads.emplace_back([this]() { queueWorkerThread(); });
    }
    return true;
}

queue<pair<int, char*>> Server::getRecvMsgQueue() { return recvMsgQueue; }

void Server::enqueueRecvMsgQueue(pair<int, char*> message) {
    recvMsgQueueMutex.lock();
    recvMsgQueue.push(message);
    recvMsgQueueMutex.unlock();
}

void Server::dequeueRecvMsgQueue() {
    recvMsgQueueMutex.lock();
    recvMsgQueue.pop();
    recvMsgQueueMutex.unlock();
}