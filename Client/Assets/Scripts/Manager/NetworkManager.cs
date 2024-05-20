using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour
{
    private void Awake()
    {

    }

    private void Start()
    {
        try
        {
            NetworkService.Instance.ConnectToServer();
            NetworkService.Instance.SendPacket(PacketManager.Instance.CreatePacket(PacketType.C_REQ_ID));
            NetworkService.Instance.ReceivePacket();
            P2PNetworkService.Instance.ReceivePacket();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    private void Update()
    {
        TimeManager.Instance.UpdateTime();



        if (ClientManager.Instance.MyClient.CurrentState == ClientState.DISCONNECTED)
            return;

        if (SceneManager.GetActiveScene().name == "Lobby" && NetworkService.Instance.LobbyManager == null)
        {
            Debug.Log("found lobbyManager");
            NetworkService.Instance.LobbyManager = GameObject.Find("LobbyManager").GetComponent<LobbyManager>();
        }

        if (NetworkService.Instance.RecvQueue.Count > 0)
        {
            NetworkService.Instance.HandleReceivedPacket(NetworkService.Instance.RecvQueue.Peek());
            NetworkService.Instance.RecvQueue.Dequeue();
        }

        if (P2PNetworkService.Instance.RecvQueue.Count > 0)
        {
            P2PNetworkService.Instance.HandleReceivedPacket(P2PNetworkService.Instance.RecvQueue.Peek());
            P2PNetworkService.Instance.RecvQueue.Dequeue();
            P2PNetworkService.Instance.ReceivePacket();
        }
    }

    private void OnApplicationQuit()
    {
        ClientManager.Instance.MyClient.CloseConnection();
        Debug.Log("Closed Sockets");
    }
}
