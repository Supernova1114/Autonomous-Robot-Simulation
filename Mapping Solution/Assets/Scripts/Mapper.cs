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
    [SerializeField] private Transform robotTransform;
    [SerializeField] private Transform robotBaseTransform;
    [SerializeField] private Transform targetPosition;
    [SerializeField] private float robotSpeed;

    private Vector2 targetVelocity;
    private Quaternion targetRotation;

    private Vector2 mapTargetPosCeil;

    public short[,] map2D;
    private float[,] rangeList; // Row 0: angles, Row 1: ranges.

    private Vector2 mapCenter; // Center for n x n map

    private float lastMapClearTime = 0;

    private int maxRaySizeCeil;

    PathFinderOptions pathfinderOptions = new PathFinderOptions
    {
        PunishChangeDirection = true,
        UseDiagonals = false,
    };


    // Start is called before the first frame update
    void Start()
    {
        maxRaySizeCeil = Mathf.CeilToInt(lidarModule.GetMaxRayDistance());

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
            float angle = (rangeList[0, i] + 270) % 360 * Mathf.Deg2Rad;

            if (range == Mathf.Infinity)
            {
                // Raytrace clear cells

                range = maxRaySizeCeil * mapResolutionFactor;

                float slope = Mathf.Tan(angle);
                //print(slope); 

                //float slope = (Mathf.Sin(angle) * range) / (Mathf.Cos(angle) * range);


                float linePercent = 0;

                while (linePercent < 1)
                {
                    int sideSwitch;
                    int bruh;

                    if (angle >= 0 && angle < Mathf.PI / 2)
                    {
                        sideSwitch = -1;
                        bruh = 1;
                    }
                    else if (angle >= Mathf.PI / 2 && angle < Mathf.PI)
                    {
                        sideSwitch = 1;
                        bruh = 1;
                    }
                    else if (angle >= Mathf.PI && angle < Mathf.PI * 3 / 2)
                    {
                        sideSwitch = 1;
                        bruh = -1;
                    }
                    else
                    {
                        sideSwitch = -1;
                        bruh = -1;
                    }

                    // Create parametric equation of line
                    int x = Mathf.CeilToInt(linePercent * range * -sideSwitch * 1) + (int)mapCenter.x - 1;
                    int y = Mathf.CeilToInt(linePercent * Mathf.Abs(slope) * range * bruh * 1) + (int)mapCenter.y - 1;



                    for (int r = y - 2; r <= y + 2; r++)
                    {
                        for (int c = x - 2; c <= x + 2; c++)
                        {
                            if (r < map2D.GetLength(0) && r >= 0 && c < map2D.GetLength(1) && c >= 0)
                            {
                                map2D[r, c] = 1;
                            }
                        }
                    }

                    linePercent += 0.05f;
                }




            }
            else
            {
                // Add blocked cell (and extra padding)

                range *= mapResolutionFactor;

                int y = Mathf.CeilToInt(Mathf.Sin(angle) * range) + (int)mapCenter.y - 1;
                int x = Mathf.CeilToInt(Mathf.Cos(angle) * range) + (int)mapCenter.x - 1;

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

            
        }

        return;

        Vector2 currentPos = new Vector2(robotTransform.position.x, robotTransform.position.z);
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
                robotTransform.position += new Vector3(targetVelocity.x, 0, targetVelocity.y) * robotSpeed * Time.deltaTime;

                targetRotation = Quaternion.LookRotation(new Vector3(targetVelocity.x, 0, targetVelocity.y), robotBaseTransform.up);
            }

            robotBaseTransform.rotation = Quaternion.Slerp(robotBaseTransform.rotation, targetRotation, 0.05f);
        }

        // Debug map print output
        /*string output = "";

        for (int i = 0; i < map2D.GetLength(0); i++)
        {
            for (int j = 0; j < map2D.GetLength(1); j++)
            {
                output += map2D[i, j] + " ";
            }
            output += "\n";
        }*/

        //print(output);
        //Debug.Log(output);
        // Clear map

        /*if (Time.time - lastMapClearTime > clearMapInterval)
        {
            

            *//*for (int i = 0; i < map2D.GetLength(0); i++)
            {
                for (int j = 0; j < map2D.GetLength(1); j++)
                {
                    map2D[i, j] = 1; // Open Cell
                }
            }*//*

            lastMapClearTime = Time.time;
        }*/
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
