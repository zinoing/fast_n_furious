#include "Client.h"
#include "Server.h"
#include <string>

int Client::ID = 1;

Client::Client() 
: clientID(ID++), isPingResponseReceived(false), connectionStatus(ConnectionStatus::DISCONNECTED), readyToPlay(false) {
	sizeMessage[0] = 0;
	memset(message, 0, 50);
	memset(&clientID, 0, sizeof(clientID));
	memset(&option.clntSock, 0, sizeof(SOCKET));
	memset(&option.clntAddr, 0, sizeof(SOCKADDR_IN));
	memset(&roomID, 0, sizeof(roomID));
}

Client::Client(SOCKET clntSock, SOCKADDR_IN clntAddr)
	: option(CLIENT_OPTION{ clntSock, clntAddr}), clientID(ID++), isPingResponseReceived(false), connectionStatus(ConnectionStatus::DISCONNECTED), readyToPlay(false) {
	sizeMessage[0] = 0;
	memset(message, 0, 50);
	memset(roomID, 0, 4);
}

Client::~Client() {
	sizeMessage[1] = NULL;
	memset(message, 0, BUF_SIZE);
	clientID = 0;

	connectionStatus = ConnectionStatus::DISCONNECTED;

	memset(roomID, 0, sizeof(roomID) / sizeof(char));
	isPingResponseReceived = false;
	readyToPlay = false;
}

HANDLE Client::bindIOCompletionPort(HANDLE compPort) {
	HANDLE IOCPHandle = CreateIoCompletionPort((HANDLE)option.clntSock, compPort, (ULONG_PTR)(this), 0);
	if (IOCPHandle == INVALID_HANDLE_VALUE) {
		ErrorHandling("bindIOCompletionPort()");
	}
	return IOCPHandle;
}

char* Client::getSizeMessage() {
	return sizeMessage;
}

char* Client::getMessage() {
	return message;
}

int Client::getClientID() {
	return clientID;
}

SOCKET Client::getSocket() {
	return option.clntSock;
}

SOCKADDR_IN Client::getSocketAddress() {
	return option.clntAddr;
}

char* Client::getRoomID() {
	return roomID;
}

void Client::setRoomID(char* roomID) {
	memcpy(this->roomID, roomID, 4);
	return;
}

ConnectionStatus Client::getConnectionStatus() {
	return connectionStatus;
}

void Client::setConnectionStatus(ConnectionStatus status) {
	connectionStatus = status;
}

NAT_OPTION Client::getNATOption() {
	return option.NATOption;
}

NAT_TYPE Client::getNATType() {
	return option.NATOption.type;
}

void Client::setNATType(NAT_TYPE type) {
	option.NATOption.type = type;
	option.NATOption.isSet = true;
}

EndPoint Client::getUDPSockPublicEP() {
	return option.udpEpInfo.publicEP;
}

unsigned int Client::getUDPSockPublicUdpPort() {
	if (option.udpEpInfo.publicEP.portNumber == "")
		return 0;
	return stoul(option.udpEpInfo.publicEP.portNumber, nullptr, 0);
}

void Client::setUDPSockPublicEP(EndPoint publicEP) {
	option.udpEpInfo.publicEP = publicEP;
}

EndPoint Client::getUDPSockPrivateEP() {
	return option.udpEpInfo.privateEP;
}

unsigned int Client::getUDPSockPrivateUdpPort() {
	if (option.udpEpInfo.privateEP.portNumber == "")
		return 0;
	return stoul(option.udpEpInfo.privateEP.portNumber, nullptr, 0);
}

void Client::setUDPSockPrivateEP(EndPoint privateEP){
	option.udpEpInfo.privateEP = privateEP;
}

bool Client::isReadyToPlay() {
	return readyToPlay;
}

bool Client::getPingResponse() {
	return isPingResponseReceived;
}

void Client::setPingResponse(bool reponse) {
	isPingResponseReceived = reponse;
	return;
}

void Client::setReadyToPlay(bool ready) {
	readyToPlay = ready;
	return;
}

void Client::receivePacket() {
	memset(getMessage(), 0, BUF_SIZE);

	IO_DATA* ioData = new IO_DATA();
	ioData->recvBuf.len = BUF_SIZE;
	ioData->recvBuf.buf = getMessage();
	ioData->rwMode = RECV_PACKET;

	DWORD recvBytes = 0;

	if (WSARecv(getSocket(), &(ioData->recvBuf), 1, &recvBytes, &flags, &(ioData->overlapped), NULL) == SOCKET_ERROR) {
		if (WSAGetLastError() == WSA_IO_PENDING) {
			printf("id: %d | still receiving\n", getClientID());
		}
		else if (WSAGetLastError() == WSAEINTR) {
			printf("%d\n", WSAGetLastError());
			return;
		}
		else {
			printf("%d\n", WSAGetLastError());
			ErrorHandling("receivePacket()");
			delete ioData;
		}
	}
	return;
}

void Client::sendMessage(const char* messageToSend) {
	IO_DATA* ioData = new IO_DATA();
	ioData->sendBuf.len = (int)messageToSend[0];
	ioData->sendBuf.buf = new char[(int)messageToSend[0]];
	memset(ioData->sendBuf.buf, 0, ioData->sendBuf.len);
	memcpy(ioData->sendBuf.buf, messageToSend, ioData->sendBuf.len);
	ioData->rwMode = SEND;

	DWORD sendBytes = 0;

	if (WSASend(getSocket(), &(ioData->sendBuf), 1, &sendBytes, flags, &(ioData->overlapped), NULL) == SOCKET_ERROR) {
		if (WSAGetLastError() == WSA_IO_PENDING) {
			printf("id: %d | still sending\n", getClientID());
		}
		else
		{
			printf("%d\n", WSAGetLastError());
			ErrorHandling("WSASend()");
		}
	}
	return;
}
