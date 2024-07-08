using AStar.Options;
using AStar;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using System.Linq;

public class Mapper : MonoBehaviour
{
    [SerializeField] private LidarModule2D lidarModule;
    [SerializeField] private float clearMapInterval;
    [SerializeField] private int mapResolutionFactor = 10;
    [SerializeField] private Transform currentPosition;
    [SerializeField] private Transform targetPosition;
    [SerializeField] private float robotSpeed;

    private Vector2 targetVelocity;

    private Vector2 mapTargetPosCeil;

    public short[,] map2D;
    private float[,] rangeList; // Row 0: angles, Row 1: ranges.

    private Vector2 mapCenter; // Center for n x n map

    private float lastMapClearTime = 0;

    PathFinderOptions pathfinderOptions = new PathFinderOptions
    {
        PunishChangeDirection = true,
        UseDiagonals = false,
    };


    // Start is called before the first frame update
    void Start()
    {
        int maxRaySizeCeil = Mathf.CeilToInt(lidarModule.GetMaxRayDistance());

        mapCenter = mapResolutionFactor * maxRaySizeCeil * Vector2.one;

        int mapSize = (maxRaySizeCeil * mapResolutionFactor * 2) + 1; // +1 to add a center

        map2D = new short[mapSize, mapSize];

        // Assign all map spaces to open
        for (int i = 0; i < map2D.GetLength(0); i++)
        {
            for (int j = 0; j < map2D.GetLength(1); j++)
            {
                map2D[i, j] = 1; // Open cell
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

            float angle = (rangeList[0, i] + 270) * Mathf.Deg2Rad;

            int y = Mathf.CeilToInt(Mathf.Sin(angle) * range) + (int)mapCenter.y - 1;
            int x = Mathf.CeilToInt(Mathf.Cos(angle) * range) + (int)mapCenter.x - 1;

            // Add blocked cell (and extra padding)
            for (int r = y - 2; r <= y + 2; r++)
            {
                for (int c = x - 2; c <= x + 2; c++)
                {
                    if (r < map2D.GetLength(0) && r >= 0 && c < map2D.GetLength(1) && c >= 0)
                    {
                        map2D[r, c] = 0;
                    }
                }
            }
        }

        // TODO - add a raytrace map clearer

       

        Vector2 currentPos = new Vector2(currentPosition.position.x, currentPosition.position.z);
        Vector2 targetPos = new Vector2(targetPosition.position.x, targetPosition.position.z);

        //Debug.DrawRay(new Vector3(targetPos.x, 1, targetPos.y), Vector3.right * 0.1f, Color.green, 0.01f);

        Vector2 mapTargetPos = ((targetPos - currentPos) * mapResolutionFactor) + new Vector2(mapCenter.x - 1, mapCenter.y - 1);
        mapTargetPosCeil = new Vector2(Mathf.CeilToInt(mapTargetPos.x), Mathf.CeilToInt(mapTargetPos.y));

        //print("Current Pos: " + mapCenter + ", Target Pos:" + mapTargetPosCeil);

        var worldGrid = new WorldGrid(map2D);
        var pathfinder = new PathFinder(worldGrid, pathfinderOptions);

        Position[] path = pathfinder.FindPath(new Position((int)mapCenter.x, (int)mapCenter.y), new Position((int)mapTargetPosCeil.x, (int)mapTargetPosCeil.y));

        if (path.Length > 0)
        {
            // Convert path to worldspace
            Vector2[] worldPath = new Vector2[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                float y = (path[i].Row - mapCenter.y) / mapResolutionFactor;
                float x = (path[i].Column - mapCenter.x) / mapResolutionFactor;

                Vector2 temp = new Vector2(x, y) + new Vector2(currentPos.y, currentPos.x);
                worldPath[i] = new Vector2(temp.y, temp.x);

                // DEBUG
                Debug.DrawRay(new Vector3(worldPath[i].x, 1, worldPath[i].y), Vector3.right * 0.1f, Color.blue, 0.01f);
            }

            if (worldPath.Length >= 2)
            {
                targetVelocity = (worldPath[1] - worldPath[0]).normalized;
                // Temp
                currentPosition.position += new Vector3(targetVelocity.x, 0, targetVelocity.y) * robotSpeed * Time.deltaTime;
            }
        }

        /*// Debug map print output
        string output = "";

        for (int i = 0; i < map2D.GetLength(0); i++)
        {
            for (int j = 0; j < map2D.GetLength(1); j++)
            {
                output += map2D[i, j] + " ";
            }
            output += "\n";
        }

        //print(output);
        Debug.Log(output);*/


        // Clear map
        if (Time.time - lastMapClearTime > clearMapInterval)
        {
            for (int i = 0; i < map2D.GetLength(0); i++)
            {
                for (int j = 0; j < map2D.GetLength(1); j++)
                {
                    map2D[i, j] = 1; // Open Cell
                }
            }

            lastMapClearTime = Time.time;
        }
    }

    public Vector2 GetMapCenter()
    {
        return mapCenter;
    }

    public int GetMapResolutionFactor()
    {
        return mapResolutionFactor;
    }

    public Vector2 GetMapTargetPos()
    {
        return mapTargetPosCeil;
    }

}
