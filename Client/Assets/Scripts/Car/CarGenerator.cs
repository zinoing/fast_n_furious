using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CarGenerator : MonoBehaviour
{
    public GameObject playerCarPrefab;
    public GameObject otherPlayerCarPrefab;
    public GameObject Player;

    public GameObject CreateCar(CarState currentCarInfo)
    {
        GameObject car = Instantiate(playerCarPrefab, currentCarInfo.position,
                                     currentCarInfo.rotation,
                                     Player.transform);
        return car;
    }

    public GameObject CreateOtherCar(CarState currentCarInfo)
    {
        GameObject car = Instantiate(otherPlayerCarPrefab, 
                                    currentCarInfo.position, 
                                    currentCarInfo.rotation,
                                    Player.transform);
        return car;
    }
}