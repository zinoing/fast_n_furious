#pragma once
#include "System.h"
#include "NetworkOption.h"
#include <mutex>
#include <queue>

class Client : public System
{
private:
public:
	Client();
	Client(SOCKET clntSock, SOCKADDR_IN clntAddr);
	~Client();

	HANDLE bindIOCompletionPort(HANDLE compPort);

	char* getSizeMessage();
	char* getMessage();

	int getClientID();
	SOCKET getSocket();
	SOCKADDR_IN getSocketAddress();

	char* getRoomID();
	void setRoomID(char* roomID);

	ConnectionStatus getConnectionStatus();
	void setConnectionStatus(ConnectionStatus status);

	NAT_OPTION getNATOption();
	NAT_TYPE getNATType();
	void setNATType(NAT_TYPE type);

	EndPoint getUDPSockPublicEP();
	unsigned int getUDPSockPublicUdpPort();
	void setUDPSockPublicEP(EndPoint publicEP);

	EndPoint getUDPSockPrivateEP();
	unsigned int getUDPSockPrivateUdpPort();
	void setUDPSockPrivateEP(EndPoint privateEP);

	bool isReadyToPlay();
	void setReadyToPlay(bool ready);

	bool getPingResponse();
	void setPingResponse(bool reponse);

	void receivePacket();
	void sendMessage(const char* messageToSend);

private:
	static int ID;

	char sizeMessage[1];
	char message[BUF_SIZE];
	int clientID;

	ConnectionStatus connectionStatus;
	CLIENT_OPTION option;

	char roomID[4];
	bool isPingResponseReceived;
	bool readyToPlay;

	DWORD flags = 0;

	queue<char*> recvMsgQueue;
	mutex msgQueueMutex;
};