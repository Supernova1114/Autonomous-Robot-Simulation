using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LidarModule2D : MonoBehaviour
{
    [SerializeField] private GameObject lidarSensor;
    [SerializeField] private float angleStepResolution;
    [SerializeField] private float maxRayDistance;

    private RaycastHit hitInfo;
    private float[,] rangeList; // Row 0: angles, Row 1: ranges.
    private int listIndex = 0;

    Vector3 lidarPosition;
    Quaternion lidarRotation;
    Vector3 lidarForward;

    string output;

    void Start()
    {
        rangeList = new float[2, Mathf.CeilToInt(360 / angleStepResolution)];

        // Fill ranges with infinity
        for (int i = 0; i < rangeList.GetLength(0); i++)
        {
            rangeList[1, i] = Mathf.Infinity;
        }
    }

    void Update()
    {
        /*output = "";
        for (int i = 229; i < 239; i++)
        {
            output += rangeList[i] + " ";
        }
        print(output);*/
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < 10; i++)
        {
            lidarPosition = lidarSensor.transform.position;
            lidarRotation = lidarSensor.transform.localRotation;
            lidarForward = lidarSensor.transform.forward;

            /*if (transform.localEulerAngles.y >= 270 || transform.localEulerAngles.y <= 90)*/

            listIndex = Mathf.CeilToInt(lidarRotation.eulerAngles.y / angleStepResolution);

            rangeList[0, listIndex] = lidarRotation.eulerAngles.y; // Angle

            if (Physics.Raycast(lidarPosition, lidarForward, out hitInfo, maxRayDistance) == true)
            {
                rangeList[1, listIndex] = hitInfo.distance; // Range

                Debug.DrawRay(lidarPosition, lidarForward * hitInfo.distance, Color.red, 0.5f);
            }
            else
            {
                rangeList[1, listIndex] = Mathf.Infinity; // Range
            }
            
            // Rotate object
            lidarSensor.transform.localRotation = Quaternion.Euler(0, lidarSensor.transform.localEulerAngles.y + angleStepResolution, 0);
        }
    }

    public float[,] GetRanges()
    {
        return rangeList;
    }

    public float GetMaxRayDistance()
    {
        return maxRayDistance;
    }

}
