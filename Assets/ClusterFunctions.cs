using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterFunctions {

    public class LookupTable
    {
        public class LookupEntry
        {
            public  string base_obj;
            public string target_obj;
            public  float heuristic_value;

            public LookupEntry(string b_obj, string t_obj, float heuristic)
            {
                base_obj = b_obj;
                target_obj = t_obj;
                heuristic_value = heuristic;
            }

            public string prettyString()
            {
                return ("base_obj: " + base_obj + "\ntarget_obj: " + target_obj + "\nheuristic_value: " + heuristic_value);
            }

        }
        public List<LookupEntry> lookup_table = new List<LookupEntry>();

        public LookupEntry returnEntry(string b_obj, string t_obj)
        {
            foreach(LookupEntry le in lookup_table)
            {
                if(b_obj == le.base_obj && t_obj == le.target_obj)
                {
                    return le;
                }
            }
            Debug.Log("No lookup entry found");
            return null;
        }

    }

    public static LookupTable tile_lookup_table = new LookupTable();
    public static LookupTable pov_lookup_table = new LookupTable();

    public static Dictionary<string, List<GameObject>> room_tile_association = new Dictionary<string, List<GameObject>>();
    public static Dictionary<string, GameObject> tile_centers = new Dictionary<string, GameObject>();
    public static Dictionary<string, List<GameObject>> room_pov_association = new Dictionary<string, List<GameObject>>();
    public static Dictionary<string, GameObject> pov_centers = new Dictionary<string, GameObject>();

    static GameObject[] rooms = GameObject.FindGameObjectsWithTag("Room");
    static GameObject[] tiles = GameObject.FindGameObjectsWithTag("TileNode");
    static GameObject[] povs = GameObject.FindGameObjectsWithTag("PoVNode");

    public static void buildLookupTable()
    {
        foreach (GameObject room in rooms)
        {
            List<GameObject> room_tiles = new List<GameObject>();
            float closest_so_far = float.MaxValue;
            tile_centers.Add(room.name, null);
            foreach (GameObject tile in tiles)
            {
                if (room.GetComponent<BoxCollider>().bounds.Contains(tile.transform.position))
                {
                    room_tiles.Add(tile);
                    float dist_from_cent = Vector3.Distance(tile.transform.position, room.GetComponent<BoxCollider>().bounds.center);
                    if (dist_from_cent < closest_so_far)
                    {
                        closest_so_far = dist_from_cent;
                        tile_centers[room.name] = tile;
                    }
                }
            }
            room_tile_association.Add(room.name, room_tiles);

            List<GameObject> room_povs = new List<GameObject>();
            closest_so_far = float.MaxValue;
            pov_centers.Add(room.name, null);
            foreach (GameObject pov in povs)
            {
                if (room.GetComponent<BoxCollider>().bounds.Contains(pov.transform.position))
                {
                    room_povs.Add(pov);
                    float dist_from_cent = Vector3.Distance(pov.transform.position, room.GetComponent<BoxCollider>().bounds.center);
                    if (dist_from_cent < closest_so_far)
                    {
                        closest_so_far = dist_from_cent;
                        pov_centers[room.name] = pov;
                    }
                }
            }
            room_pov_association.Add(room.name, room_povs);
        }

        foreach(KeyValuePair<string, GameObject> tile_origin in tile_centers)
        {
            foreach (KeyValuePair<string, GameObject> tile_hunting in tile_centers)
            {
                if (tile_origin.Key != tile_hunting.Key)
                {
                    List<Connection_Data> open_nodes = new List<Connection_Data>();
                    List<Connection_Data> closed_nodes = new List<Connection_Data>();
                    GameObject.FindGameObjectWithTag("GameController")
                        .GetComponent<Map_Gen>()
                        .itterativeAStar(
                            tile_origin.Value.GetComponent<Connection_List>(),
                            tile_hunting.Value.GetComponent<Connection_List>(), open_nodes, closed_nodes, Map_Gen.Heuristic_Search.Euclidian, Map_Gen.Categories.Tile);
                    //last closed node contains the end, but tile_hunting should have data anyways
                    LookupTable.LookupEntry table_entry = new LookupTable.LookupEntry(tile_origin.Key, tile_hunting.Key, tile_hunting.Value.GetComponent<Connection_List>().cost_so_far);
                    ClusterFunctions.tile_lookup_table.lookup_table.Add(table_entry);
                    GameObject.FindGameObjectWithTag("GameController")
                        .GetComponent<Map_Gen>().resetGraphs();
                }
            }

        }

        foreach (KeyValuePair<string, GameObject> pov_origin in pov_centers)
        {
            foreach (KeyValuePair<string, GameObject> pov_hunting in pov_centers)
            {
                if (pov_origin.Key != pov_hunting.Key)
                {
                    List<Connection_Data> open_nodes = new List<Connection_Data>();
                    List<Connection_Data> closed_nodes = new List<Connection_Data>();

                    GameObject.FindGameObjectWithTag("GameController")
                        .GetComponent<Map_Gen>()
                        .itterativeAStar(
                            pov_origin.Value.GetComponent<Connection_List>(),
                            pov_hunting.Value.GetComponent<Connection_List>(), open_nodes, closed_nodes, Map_Gen.Heuristic_Search.Euclidian, Map_Gen.Categories.PoV);

                    //last closed node contains the end, but pov_hunting should have data anyways
                    LookupTable.LookupEntry table_entry = new LookupTable.LookupEntry(pov_origin.Key, pov_hunting.Key, pov_hunting.Value.GetComponent<Connection_List>().cost_so_far);
                    ClusterFunctions.pov_lookup_table.lookup_table.Add(table_entry);
                    GameObject.FindGameObjectWithTag("GameController")
                        .GetComponent<Map_Gen>().resetGraphs();
                }
            }
        }
        /*
        foreach(LookupTable.LookupEntry le in pov_lookup_table.lookup_table)
        {
            Debug.Log(le.prettyString());
        }
        foreach (LookupTable.LookupEntry le in tile_lookup_table.lookup_table)
        {
            Debug.Log(le.prettyString());
        }
        */
    }


}
