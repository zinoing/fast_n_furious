using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ErrorHandler : MonoBehaviour
{
    public static void ErrorHandling(string errorDetail)
    {
        Debug.Log("Error: " + errorDetail);
        Environment.Exit(0);
    }
}
