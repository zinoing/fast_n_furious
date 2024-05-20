using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Threading.Tasks;
using System;
using JetBrains.Annotations;

public class SuperPeerManager : Singleton<SuperPeerManager>
{
    public int numOfClientsAnsweredHeratbeat;

    private List<Client> clients;
    private Client superPeer;
    public Client SuperPeer
    {
        get { return superPeer; }
        set { superPeer = value;}
    }

    public bool inGame {  get; set; }

    public SuperPeerManager()
    {
        clients = new List<Client>();
        superPeer = new Client();
        numOfClientsAnsweredHeratbeat = -1;
    }

    public List<Client> GetClients()
    {
        return clients;
    }
    public Client FindClient(int id)
    {
        for(int i=0; i< clients.Count; i++)
        {
            if (clients[i].ClientID == id)
            {
                return clients[i];
            }
        }
        return null;
    }
    public Client FindClient(IPEndPoint ep)
    {
        for (int i = 0; i < clients.Count; i++)
        {
            if (IPEndPoint.Equals(clients[i].ClientUDPEndPointPair.privateEP, ep) ||
                IPEndPoint.Equals(clients[i].ClientUDPEndPointPair.publicEP, ep))
            {
                return clients[i];
            }
        }
        return null;
    }
    public void SetClients(List<Client> clients)
    {
        this.clients.Clear();
        Debug.Log("clients.count: " + clients.Count);
        
        for (int i=0; i< clients.Count; i++)
        {
            this.clients.Add(clients[i]);
        }
        Debug.Log("Clients setting completed");
    }

    public void RemoveClient(IPEndPoint ep)
    {
        if (clients.Count == 0)
            return;

        for (int i = 0; i < clients.Count; i++)
        {
            if (IPEndPoint.Equals(clients[i].ClientUDPEndPointPair.privateEP, ep) ||
                IPEndPoint.Equals(clients[i].ClientUDPEndPointPair.publicEP, ep))
            {
                clients.RemoveAt(i);
            }
        }
    }
    public void RemoveClient(IPEndPointPair epPair)
    {
        if (clients.Count == 0)
            return;

        for (int i = 0; i < clients.Count; i++)
        {
            if (IPEndPoint.Equals(clients[i].ClientUDPEndPointPair.privateEP, epPair.privateEP) ||
                IPEndPoint.Equals(clients[i].ClientUDPEndPointPair.publicEP, epPair.publicEP))
            {
                clients.RemoveAt(i);
            }
        }
    }
    public void SetPositions()
    {
        int num_of_clients = clients.Count;
        Vector3 startPos = new Vector3(-190.0f, -1.5f, -20.0f);
        Quaternion startRotation = Quaternion.Euler(0f, -8.0f, 0f);
        float initialSpped = 0.0f;
        Vector3 initialAngularVelocity = Vector3.zero;
        for (int i=0; i< num_of_clients; i++)
        {
            if(i % 2 == 0)
            {
                startPos.x -= 3.0f;
            }
            else
            {
                startPos.x += 3.0f;
            }
            startPos.z -= 4.0f;
            CarState carState = new CarState(startPos, startRotation, initialSpped, initialAngularVelocity);
            clients[i].CarState = carState;
            Debug.Log("Position: " + startPos);

        }
        Debug.Log("Position setting completed");
    }
    public async void SendHeartbeat()
    {
        while (true)
        {
            if (!inGame)
                break;

            await Task.Delay(3000);

            if (numOfClientsAnsweredHeratbeat != -1 &&
                numOfClientsAnsweredHeratbeat != clients.Count - 1)
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    if(clients[i].AnsweredHeartbeat == false &&
                        clients[i].ClientID != SuperPeer.ClientID)
                    {
                        clients[i].CurrentState = ClientState.DISCONNECTED;
                        clients.RemoveAt(i);
                        Debug.Log("Removed client,current client size: " + clients.Count);

                        // 이 Peer가 lost connection된 것을 모두에게 알린다
                        P2PNetworkService.Instance.SelectiveMulticast(PacketManager.Instance.CreatePacket(PacketType.SP_INFORM_LOST_PEER,
                                                                      PacketManager.Instance.EncodeIPEndPoint(clients[i].ClientUDPEndPointPair.privateEP)), 
                                                                      superPeer.ClientUDPEndPointPair);
                    }
                }
            }

            for (int i = 0; i < clients.Count; i++) {
                clients[i].AnsweredHeartbeat = false;
            }
            numOfClientsAnsweredHeratbeat = 0;
            Debug.Log("Send SP_REQ_HB,current client size: " + clients.Count);
            P2PNetworkService.Instance.SelectiveMulticast(PacketManager.Instance.CreatePacket(PacketType.SP_REQ_HB), 
                                                          superPeer.ClientUDPEndPointPair);
            Debug.Log("Send Heartbeat");
        }
    }
}
