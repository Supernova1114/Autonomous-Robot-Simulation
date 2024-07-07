using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mapper : MonoBehaviour
{
    [SerializeField] private LidarModule2D lidarModule;
    [SerializeField] private float clearMapInterval;

    public float[,] map2D;
    private float[,] rangeList; // Row 0: angles, Row 1: ranges.

    private float angleStepResolution;
    private Vector2 mapCenter; // Center for n x n map

    private int mapResolutionFactor = 10;

    private float lastMapClearTime = 0;


    // Start is called before the first frame update
    void Start()
    {
        int maxRaySizeCeil = Mathf.CeilToInt(lidarModule.GetMaxRayDistance());
        mapCenter = new Vector2(maxRaySizeCeil * mapResolutionFactor, maxRaySizeCeil * mapResolutionFactor);

        int mapSize = (maxRaySizeCeil * mapResolutionFactor * 2) + 1; // +1 to add a center

        map2D = new float[mapSize, mapSize];

        // Assign all map spaces to zero
        for (int i = 0; i < map2D.GetLength(0); i++)
        {
            for (int j = 0; j < map2D.GetLength(1); j++)
            {
                map2D[i, j] = 0;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        rangeList = lidarModule.GetRanges();

        for (int i = 0; i < rangeList.GetLength(1); i++)
        {
            float range = rangeList[1, i];

            if (range == Mathf.Infinity)
                continue;

            range *= mapResolutionFactor;

            float angle = rangeList[0, i] * Mathf.Deg2Rad;

            int y = Mathf.CeilToInt(Mathf.Sin(angle) * range) + (int)mapCenter.y - 1;
            int x = Mathf.CeilToInt(Mathf.Cos(angle) * range) + (int)mapCenter.x - 1;

            map2D[y, x] = 1;
        }

        // TODO - add a raytrace map clearer

        // Debug map print output
        /*string output = "";

        for (int i = 0; i < map2D.GetLength(0); i++)
        {
            for (int j = 0; j < map2D.GetLength(1); j++)
            {
                output += map2D[i, j] + " ";
            }
            output += "\n";
        }

        print(output);*/

        // Assign all map spaces to zero
        if (Time.time - lastMapClearTime > clearMapInterval)
        {
            for (int i = 0; i < map2D.GetLength(0); i++)
            {
                for (int j = 0; j < map2D.GetLength(1); j++)
                {
                    map2D[i, j] = 0;
                }
            }

            lastMapClearTime = Time.time;
        }
        
    }

    public Vector2 GetMapCenter()
    {
        return mapCenter;
    }

}
