using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayActitive : MonoBehaviour
{
    void Start()
    {
        Display.displays[0].Activate();//LED屏幕
        Display.displays[1].Activate(); //後台Setting屏幕
    }

}
