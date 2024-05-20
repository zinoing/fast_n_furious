using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine.SceneManagement;
using System.Threading;
using System.Threading.Tasks;

public enum ClientState
{
    CONNECTED,
    DISCONNECTED
};

public class Client
{
    [Header("Socket Information")]
    public Socket clntTCPSock;
    public UdpClient udpClient;
    public IPEndPoint clientTCPEndPoint;
    private IPEndPointPair clientUDPEndPointPair;
    public IPEndPointPair ClientUDPEndPointPair
    {
        get { return clientUDPEndPointPair; }
        set { clientUDPEndPointPair = value; }
    }

    private int clientID;
    public int ClientID
    {
        get { return clientID; }
        set { clientID = value; }
    }

    private ulong previousPacketNum;
    public ulong PreviousPacketNum
    {
        get { return previousPacketNum; }
        set { previousPacketNum = value; }
    }

    private RoomSetting inRoomSetting;
    public RoomSetting InRoomSetting
    {
        get { return inRoomSetting; }
        set { inRoomSetting = value; }
    }

    private ClientState currentState;
    public ClientState CurrentState
    {
        get { return currentState; }
        set { currentState = value; }
    }

    private CarState carState;
    public CarState CarState
    {
        get { return carState; }
        set { carState = value; }
    }

    private DetailCarInfo detailCarInfo;
    public DetailCarInfo DetailCarInfo
    {
        get { return detailCarInfo = new DetailCarInfo(clientID, carState); }
        set { detailCarInfo = value; }
    }

    private bool answeredHeartbeat;
    public bool AnsweredHeartbeat
    {
        get { return answeredHeartbeat; }
        set { answeredHeartbeat = value; }
    }

    public Client()
    {
        clntTCPSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        udpClient = new UdpClient(0, AddressFamily.InterNetwork);
        ClientUDPEndPointPair = new IPEndPointPair(new IPEndPoint(IPAddress.Any, 0), new IPEndPoint(IPAddress.Any, 0));
        currentState = ClientState.CONNECTED;
        clientID = 0;
        previousPacketNum = 0;
    }

    public Client(IPEndPoint privateEP, IPEndPoint publicEP)
    {
        clntTCPSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        udpClient = new UdpClient(0, AddressFamily.InterNetwork);
        ClientUDPEndPointPair = new IPEndPointPair(privateEP, publicEP);
        currentState = ClientState.CONNECTED;
        clientID = 0;
        previousPacketNum = 0;
    }

    public void CloseConnection()
    {
        clntTCPSock.Close();
        udpClient.Close();
    }
}
