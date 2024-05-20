using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RoomGenerator : MonoBehaviour
{
    public GameObject roomPrefab;
    public GameObject roomListBackground;

    public GameObject CreateRoom(RoomSetting roomSetting)
    {
        GameObject room = Instantiate(roomPrefab, Vector3.zero, Quaternion.identity, roomListBackground.transform);
        room.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
        room.GetComponent<RoomController>().SetRoomSetting(roomSetting);

        return room;
    }
}


