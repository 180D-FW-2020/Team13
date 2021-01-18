using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    public int levelNumber;
    public List<Transform> playerPads;
    public List<Transform> waypoints;


    public List<Transform> GetWaypoints()
    {
        return waypoints;
    }

    public List<Transform> GetPlayerPads()
    {
        return playerPads;
    }
}
