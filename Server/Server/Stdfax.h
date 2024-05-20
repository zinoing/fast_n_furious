#pragma once

#define _CRT_SECURE_NO_WARNINGS
#define _WINSOCK_DEPRECATED_NO_WARNINGS

#define MAX_WORKERTHREAD 14
// 
#define MAX_IO_WORKER_THREAD 4
#define MAX_QUEUE_WORKER_THREAD 7

#define SERV_IP "192.168.0.6"
#define SERV_PORT 7000
#define BUF_SIZE 1024

#define RECV_SIZE 0
#define RECV_PACKET 1
#define SEND 2

#define ROOMSETTING_SIZE 30
#define ROOMSETTING_WITHOUT_PASSWORD_SIZE 26
#define SUPERPEER_SIZE 17
#define ROOM_SIZE 10

#include <iostream>
#include <winsock2.h>
using namespace std;