using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TimeManager : Singleton<TimeManager>
{
    private int syncRate;
    private double prevDev;
    private double actualDev;
    private double newDev;
    private int timeOut;
    private double mean;
    private long diffTick; // 슈퍼 피어와 클라이언트 간의 왕복 tick 차
    private TimeSpan diffTime;
    public TimeSpan DiffTime
    {
        get { return diffTime; }
    }
    private DateTime sendTime;
    private DateTime currentTime;
    private DateTime gameTime;
    public DateTime GameTime {
        get {gameTime = DateTime.Now + diffTime;
            return gameTime;
        }
    }

    Queue<long> rttQueue;
    public TimeManager()
    {
        rttQueue = new Queue<long>();
        currentTime = DateTime.Now;
        diffTime = new TimeSpan(0);
        syncRate = 0;
        prevDev = -1;
        actualDev = -1;
        newDev = -1;
        timeOut = 800;
        mean = -1;
    }

    public void ReqTimeToSuperPeer()
    {
        sendTime = DateTime.Now;
        P2PNetworkService.Instance.SendPacketToSuperPeer(PacketManager.Instance.CreatePacket(PacketType.C_REQ_TIME));
    }

    public void CheckDiffTick()
    {
        diffTick = currentTime.Ticks - sendTime.Ticks;
        CalculateAccurateRTO(diffTick);

        diffTime = new TimeSpan(diffTick / 2);
        gameTime = currentTime + diffTime;

        //Debug.Log("currentTime: " + currentTime + "Game time: " + gameTime + " diffTick: " + diffTick + " diffTime: " + diffTime);
    }

    private void CalculateAccurateRTO(long diffTick)
    {
        rttQueue.Enqueue(diffTick);

        if (rttQueue.Count < 2)
        {
            timeOut = (int)diffTick / 30;
            return;
        }

        if (rttQueue.Count > 8)
        {
            rttQueue.Dequeue();
        }

        mean = rttQueue.Average() * 0.875 + rttQueue.Last<long>() * 0.125;
        if(actualDev != -1)
            prevDev = actualDev;
        actualDev = CalculateMeanDeviation(rttQueue);
        newDev = (prevDev == -1 ? actualDev : prevDev) * 0.875 + actualDev * 0.125;

        timeOut = (int)(mean + 4 * newDev) / 30;

        return;
    }

    private double CalculateMeanDeviation(Queue<long> values) // 평균 편차 함수
    {
        double mean = values.Average();
        double deviationSum = 0;
        foreach (long value in values)
        {
            deviationSum += Math.Abs(value - mean);
        }

        return deviationSum / values.Count;
    }


    public long CheckDiffTickFromGameTime(DateTime time)
    {
        gameTime = DateTime.Now + diffTime;
        //Debug.Log("Game time: " + gameTime + " receivedTime: " + time + " diffTick: " + (gameTime.Ticks - time.Ticks) + " diffTime: " + diffTime);

        long tickDiff = (long)Mathf.Abs(gameTime.Ticks - time.Ticks);

        return tickDiff;
    }

    public void UpdateTime()
    {
        currentTime = DateTime.Now;

        // superPeer가 정해지지 않았을 경우
        if (IPEndPoint.Equals(SuperPeerManager.Instance.SuperPeer.ClientUDPEndPointPair.privateEP, new IPEndPoint(IPAddress.Any, 0)) ||
            IPEndPoint.Equals(SuperPeerManager.Instance.SuperPeer.ClientUDPEndPointPair.publicEP, new IPEndPoint(IPAddress.Any, 0)))
        {
            return;
        }
        // superPeer일 경우
        if (SuperPeerManager.Instance.SuperPeer != null)
        {
            gameTime = currentTime;
        }
        else
        {
            gameTime = currentTime + diffTime;
            ++syncRate;
            if (timeOut == 0 || syncRate % timeOut == 0)
            {
                syncRate = 0;
                ReqTimeToSuperPeer();
            }
        }
    }
}
