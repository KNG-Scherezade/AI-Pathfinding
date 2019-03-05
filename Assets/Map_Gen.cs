using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map_Gen : MonoBehaviour {

    public GameObject tile_sphere;
    public GameObject pov_node_obj;
    public GameObject tile_node_obj;

    public Grid tile_grid;

    public Vector3 grid_start;
    public Vector3 grid_end;
    public float raycast_distance  = 1000.0f;
    public float raycast_interval = 0.05f;

    private List<List<GameObject>> tile_node_list = new List<List<GameObject>>();
    private List<GameObject> pov_node_list = new List<GameObject>();
    
    public int rand1;
    public int rand2;

    public enum Heuristic_Search
    {
        Null, Euclidian, Cluster
    };

    public enum Categories
    {
        Tile, PoV
    };

    // Use this for initialization
    void Start()
    {

         rand1 = (int)(Random.Range(0, 1000000));
         rand2 = (int)(Random.Range(0, 1000000));

    // generate tile nodes and place in collection game object
    // Itterate from gridstart to gridend 
        Debug.Log("start");
        LayerMask tile_layermask = ~(1 << 9);
        for (float x_grid = grid_start.x; x_grid < grid_end.x; x_grid += tile_grid.cellSize.x)
        {
            List<GameObject> tile_list_x = new List<GameObject>();
            for (float z_grid = grid_start.z; z_grid < grid_end.z; z_grid += tile_grid.cellSize.y)
            {
                Collider[] overlap = Physics.OverlapBox(new Vector3(
                        x_grid + tile_grid.cellSize.x / 2,
                        -0.05f,
                        z_grid + tile_grid.cellSize.y / 2),
                        new Vector3(tile_grid.cellSize.x / 2, 0.1f, tile_grid.cellSize.z / 2),
                        Quaternion.identity,
                        tile_layermask
                    );
                
                if (overlap.Length > 0)
                {
                    bool is_floor_only = true;
                    foreach (Collider ol in overlap)
                    {
                        if ((ol.name.Contains("floor") || ol.name.Contains("Floor")))
                        {
                            continue;
                        }
                        else
                        {
                            is_floor_only = false;
                            tile_list_x.Add(null);
                            break;
                        }
                    }
                    if (is_floor_only)
                    {
                        GameObject sphere = Instantiate(tile_sphere, tile_node_obj.transform);
                        sphere.transform.position = new Vector3(
                            x_grid + tile_grid.cellSize.x / 2,
                            0f,
                            z_grid + tile_grid.cellSize.y / 2);
                        tile_list_x.Add(sphere);
                    }
                }
                else
                {
                    tile_list_x.Add(null);
                }
            }
            tile_node_list.Add(tile_list_x);
        }
        for (int tile_list_no = 0; tile_list_no < tile_node_list.Count; tile_list_no++)
        {
            for(int tile_no = 0; tile_no < tile_node_list[tile_list_no].Count; tile_no++)
            {
                if (tile_node_list[tile_list_no][tile_no] == null)
                    continue;

                Connection_List cl = tile_node_list[tile_list_no][tile_no].GetComponent<Connection_List>();
                if (tile_list_no - 1 != -1 && tile_no - 1 != -1)
                {
                    addNodeToList(cl, tile_list_no, -1, tile_no, -1);
                }
                if (tile_list_no - 1 != -1 && 0 == 0) 
                {
                    addNodeToList(cl, tile_list_no, -1, tile_no, 0);
                }
                if (tile_list_no - 1 != -1 && tile_no + 1 < tile_node_list[tile_list_no].Count)
                {
                    addNodeToList(cl, tile_list_no, -1, tile_no, +1);
                }
                if (0 == 0 && tile_no - 1 != -1)
                {
                    addNodeToList(cl, tile_list_no, 0, tile_no, -1);
                }
                if (0 == 0 && 0 == 0)
                {
                    // start pos
                }
                if (0 == 0 && tile_no + 1 < tile_node_list[tile_list_no].Count)
                {
                    addNodeToList(cl, tile_list_no, 0, tile_no, +1);
                }
                if (tile_list_no + 1 < tile_node_list.Count && tile_no - 1 != -1)
                {
                    addNodeToList(cl, tile_list_no, +1, tile_no, -1);
                }
                if (tile_list_no + 1 < tile_node_list.Count && 0 == 0)
                {
                    addNodeToList(cl, tile_list_no, +1, tile_no, 0);
                }
                if (tile_list_no + 1 < tile_node_list.Count && tile_no + 1 < tile_node_list[tile_list_no].Count)
                {
                    addNodeToList(cl, tile_list_no, +1, tile_no, +1);
                }
                cl.drawConnectionLines();
            }
        }

        // connect tile nodes with adjacent neighbours
        foreach (Transform pov_node in pov_node_obj.transform)
        {
            //do a 360 raycast for pov nodes 
            // snipet from https://docs.unity3d.com/ScriptReference/Physics.Raycast.html
            // Bit shift the index of the layer (8) to get a bit mask
            int pov_layer = 1 << 9;
            pov_layer = ~pov_layer;
            RaycastHit hit;
            // Does the ray intersect any objects excluding the player layer
            for (float deg = 0; deg < 360; deg += raycast_interval)
            {
                float rad = Mathf.Deg2Rad * deg;
                Vector3 ray_angle = new Vector3(
                    pov_node.forward.magnitude * Mathf.Sin(rad),
                    pov_node.forward.y,
                    pov_node.forward.magnitude * Mathf.Cos(rad));
                //check colisions
                //add to current node's connection_data any matches and their distance
                if (Physics.Raycast(pov_node.position, ray_angle, out hit, Mathf.Infinity, pov_layer)
                    && hit.collider.name.Contains("PoV"))
                {
                    //Debug.Log(hit.normal + " " + ray_angle);
                    List<Connection_Data> conn =  pov_node.GetComponent<Connection_List>().connections;
                    foreach (Connection_Data data in conn) { 
                        if (data.connection == hit.collider.gameObject)
                        {
                            continue;
                        }
                    }
                    conn.Add(new Connection_Data(hit.collider.gameObject, hit.distance, pov_node.gameObject));
                }
                else
                {
                    // Debug.Log("Hit Wrong");
                    //or
                    //  Debug.Log("Did not Hit");
                }
                //break;
            }

            //draw this PoV's lines
            pov_node.GetComponent<Connection_List>().drawConnectionLines();
        }

        //build lookup data for pov and tile
        ClusterFunctions.buildLookupTable();

        //Deactivate PoV
        pov_node_obj.SetActive(false);
    }

     public void addNodeToList(Connection_List cl, int tile_list_no, int tile_list_no_mod, 
        int tile_no, int tile_no_mod)
        {
            if (tile_node_list[tile_list_no + tile_list_no_mod][tile_no + tile_no_mod] != null) { 
                cl.connections.Add(
                    new Connection_Data(
                        tile_node_list[tile_list_no + tile_list_no_mod][tile_no + tile_no_mod],
                        Vector3.Distance(
                            tile_node_list[tile_list_no][tile_no].gameObject.transform.position,
                            tile_node_list[tile_list_no + tile_list_no_mod][tile_no + tile_no_mod].gameObject.transform.position),
                            tile_node_list[tile_list_no][tile_no]
                    )
                );
            }
        }


    public List<GameObject> findAStarRoute(Connection_List start, Connection_List end, Heuristic_Search hs, Categories cat)
    {
        List<Connection_Data> open_nodes = new List<Connection_Data>();
        List<Connection_Data> closed_nodes = new List<Connection_Data>();
        start.cost_so_far = 0;
        start.gameObject.GetComponent<MeshRenderer>().material = start.route_col;
        itterativeAStar(start, end, open_nodes, closed_nodes, hs, cat);

        List<GameObject> path = new List<GameObject>();
        Connection_List path_node = end;
        int i = 0;

        while (++i < 10000) {
            Connection_Data best_candidate = null;
            float best_cost = float.MaxValue;
            foreach (Connection_Data back_node in path_node.connections)
            {
                if (back_node.connection.GetComponent<Connection_List>() == start)
                {
                    path.Add(back_node.connection);
                    drawPathLine(back_node);
                    back_node.connection.gameObject.GetComponent<MeshRenderer>().material = back_node.connection.GetComponent<Connection_List>().route_col;
                    return path;
                }
                foreach (Connection_Data closed_node in closed_nodes) {
                    if (closed_node.parent == back_node.connection
                        && back_node.connection.GetComponent<Connection_List>().cost_so_far != 0
                        && back_node.connection.GetComponent<Connection_List>().cost_so_far < best_cost)
                    {
                        best_candidate = back_node;
                        best_cost = back_node.connection.GetComponent<Connection_List>().cost_so_far;
                    }
                }
            }
            path.Add(best_candidate.connection);
            drawPathLine(best_candidate);
            path_node = best_candidate.connection.GetComponent<Connection_List>();
            best_candidate.connection.gameObject.GetComponent<MeshRenderer>().material = best_candidate.connection.GetComponent<Connection_List>().route_col;
        }
        Debug.LogError("NULL ESCAPE");
        return null;
    }

    public void drawPathLine(Connection_Data best_candidate)
    {
        best_candidate.connection.GetComponent<LineRenderer>().material = best_candidate.connection.GetComponent<Connection_List>().route_col;
        best_candidate.connection.GetComponent<LineRenderer>().positionCount = 2;
        Vector3[] line_vert = {
                new Vector3(best_candidate.connection.transform.position.x,best_candidate.connection.transform.position.y + 0.12f, best_candidate.connection.transform.position.z),
                new Vector3(best_candidate.parent.transform.position.x,best_candidate.parent.transform.position.y + 0.12f, best_candidate.parent.transform.position.z) };
        best_candidate.connection.GetComponent<LineRenderer>().startWidth = (0.3f);
        best_candidate.connection.GetComponent<LineRenderer>().endWidth = (0.3f);
        best_candidate.connection.GetComponent<LineRenderer>().SetPositions(line_vert);
    }

    public List<Connection_Data> itterativeAStar(Connection_List current, Connection_List end, List<Connection_Data> open_nodes, List<Connection_Data> closed_nodes, Heuristic_Search hs, Categories cat)
    {

        if(hs == Heuristic_Search.Cluster)
        {
            setClusterHeuristic(end.gameObject, cat);
        }
        else { 
            if(cat == Categories.Tile) {
                foreach(Transform node in tile_node_obj.transform)
                {
                    node.GetComponent<Connection_List>().heuristic_value = calculateHeuristic(end.gameObject, node.gameObject, hs);
                }
            }
            if (cat == Categories.PoV)
            {
                foreach (Transform node in pov_node_obj.transform)
                {
                    node.GetComponent<Connection_List>().heuristic_value = calculateHeuristic(end.gameObject, node.gameObject, hs);
                }
            }
        }


        int i = 0;
           while(++i < 100000) {
                //color most recently used node and explore possibilities off of it
                foreach (Connection_Data cd in current.connections)
                {
                    if (cd.traveled == false) { 
                        open_nodes.Add(cd);
                    }
                }
                Connection_Data target = null;
                List<Connection_Data> removed_nodes = new List<Connection_Data>();
                foreach (Connection_Data cd in open_nodes)
                {
                    if (cd.connection.GetComponent<Connection_List>().cost_so_far > 0
                        && (cd.weight + cd.parent.GetComponent<Connection_List>().cost_so_far + cd.connection.GetComponent<Connection_List>().cost_so_far
                        + cd.connection.GetComponent<Connection_List>().heuristic_value
                        > cd.connection.GetComponent<Connection_List>().cost_so_far))
                    {
                        removed_nodes.Add(cd);
                    }
                    else if (target == null && cd.traveled == false)
                    {
                        target = cd;
                    }
                    else if (cd.weight + cd.parent.GetComponent<Connection_List>().cost_so_far + cd.connection.GetComponent<Connection_List>().cost_so_far
                            + cd.connection.GetComponent<Connection_List>().heuristic_value
                                < target.weight + target.parent.GetComponent<Connection_List>().cost_so_far + target.connection.GetComponent<Connection_List>().cost_so_far
                                 + target.connection.GetComponent<Connection_List>().heuristic_value
                                && cd.traveled == false
                             )
                    {
                       target = cd;

                    }
                }

            target.connection.GetComponent<Connection_List>().cost_so_far = target.weight + target.parent.GetComponent<Connection_List>().cost_so_far;
                closed_nodes.Add(target);
                open_nodes.Remove(target);
                foreach(Connection_Data node in removed_nodes)
                {
                    open_nodes.Remove(node);
                }


                current = target.connection.GetComponent<Connection_List>();
                current.gameObject.GetComponent<MeshRenderer>().material = current.miss_col;
            if (current.gameObject == end.gameObject)
            {
                current.gameObject.GetComponent<MeshRenderer>().material = current.route_col;
                return closed_nodes;
            }
        }
        Debug.LogError("NULL ESCAPE");
        return null;
    }

    public float calculateHeuristic(GameObject a, GameObject b, Heuristic_Search hs)
    {
        if(hs == Heuristic_Search.Euclidian)
        {
            return calculateEuclidianHeuristic(a.transform.position, b.transform.position);
        }
        else
        {
            return 0.0f;
        }
    }

    public void setClusterHeuristic(GameObject end, Categories cat)
    {
        if (cat == Categories.Tile)
        {
            //for tiles
            string target_room = "";
            foreach (KeyValuePair<string, List<GameObject>> kv in ClusterFunctions.room_tile_association)
            {
                if (kv.Value.Contains(end))
                {
                    target_room = kv.Key;
                }
            }

            foreach (KeyValuePair<string, List<GameObject>> kv in ClusterFunctions.room_tile_association)
            {

                if (kv.Key != target_room)
                {
                    //Debug.Log(kv.Key + " != " + target_room);
                    float h = ClusterFunctions.tile_lookup_table.returnEntry(kv.Key, target_room).heuristic_value;
                    foreach (GameObject node in kv.Value)
                    {
                        node.GetComponent<Connection_List>().heuristic_value = h;
                    }
                }
            }
        }
        else
        {
            //for pov
            string target_room = "";
            foreach (KeyValuePair<string, List<GameObject>> kv in ClusterFunctions.room_pov_association)
            {
                if (kv.Value.Contains(end))
                {
                    target_room = kv.Key;
                }
            }

            foreach (KeyValuePair<string, List<GameObject>> kv in ClusterFunctions.room_pov_association)
            {

                if (kv.Key != target_room)
                {
                    float h = ClusterFunctions.pov_lookup_table.returnEntry(kv.Key, target_room).heuristic_value;
                    foreach (GameObject node in kv.Value)
                    {
                        node.GetComponent<Connection_List>().heuristic_value = h;
                    }
                }
            }
        }
    }

    public float calculateEuclidianHeuristic(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(a, b);
    }

    public void resetGraphs()
    {
        foreach(Transform node in pov_node_obj.transform)
        {
            node.GetComponent<Connection_List>().cost_so_far = 0;
            node.GetComponent<Connection_List>().heuristic_value = 0;
            node.GetComponent<Connection_List>().drawConnectionLines();
        }
        foreach (Transform node in tile_node_obj.transform)
        {
            node.GetComponent<Connection_List>().cost_so_far = 0;
            node.GetComponent<Connection_List>().heuristic_value = 0;
            node.GetComponent<Connection_List>().drawConnectionLines();
        };
    }

    // Update is called once per frame
    void Update () {
        if (Input.GetKeyDown("r"))
        {
            resetGraphs();
            rand1 = (int)(Random.Range(0, 1000000));
            rand2 = (int)(Random.Range(0, 1000000));
        }
        if (Input.GetKeyDown("space"))
        {
            tile_node_obj.SetActive(!tile_node_obj.activeInHierarchy);
            pov_node_obj.SetActive(!pov_node_obj.activeInHierarchy);
        }
        if (Input.GetKeyDown("a"))
        {
            resetGraphs();
            if (tile_node_obj.activeInHierarchy)
            {
                Connection_List[] tno = tile_node_obj.GetComponentsInChildren<Connection_List>();
                Debug.Log(rand1 % tno.Length);
                Debug.Log(rand2 % tno.Length);
                findAStarRoute(
                    tno[rand1 % tno.Length],
                    tno[rand2 % tno.Length], 
                    Heuristic_Search.Null, Categories.Tile);

            }
            else
            {
                Connection_List[] pno = pov_node_obj.GetComponentsInChildren<Connection_List>();
                Debug.Log(rand1 % pno.Length);
                Debug.Log(rand2 % pno.Length);
                findAStarRoute(
                    pno[rand1 % pno.Length],
                    pno[rand2 % pno.Length],
                    Heuristic_Search.Null, Categories.PoV);
            }
        }
        if (Input.GetKeyDown("b"))
        {
            resetGraphs();
            if (tile_node_obj.activeInHierarchy)
            {
                Connection_List[] tno = tile_node_obj.GetComponentsInChildren<Connection_List>();
                Debug.Log(rand1 % tno.Length);
                Debug.Log(rand2 % tno.Length);
                findAStarRoute(
                    tno[rand1 % tno.Length],
                    tno[rand2 % tno.Length],
                    Heuristic_Search.Euclidian, Categories.Tile);

            }
            else
            {
                Connection_List[] pno = pov_node_obj.GetComponentsInChildren<Connection_List>();
                Debug.Log(rand1 % pno.Length);
                Debug.Log(rand2 % pno.Length);
                findAStarRoute(
                    pno[rand1 % pno.Length],
                    pno[rand2 % pno.Length],
                    Heuristic_Search.Euclidian, Categories.PoV);
            }
        }
        if (Input.GetKeyDown("c"))
        {
            resetGraphs();
            if (tile_node_obj.activeInHierarchy)
            {
                Connection_List[] tno = tile_node_obj.GetComponentsInChildren<Connection_List>();
                Debug.Log(rand1 % tno.Length);
                Debug.Log(rand2 % tno.Length);
                findAStarRoute(
                    tno[rand1 % tno.Length],
                    tno[rand2 % tno.Length],
                    Heuristic_Search.Cluster, Categories.Tile);

            }
            else
            {
                Connection_List[] pno = pov_node_obj.GetComponentsInChildren<Connection_List>();
                Debug.Log(rand1 % pno.Length);
                Debug.Log(rand2 % pno.Length);
                findAStarRoute(
                    pno[rand1 % pno.Length],
                    pno[rand2 % pno.Length],
                    Heuristic_Search.Cluster, Categories.PoV);
            }
        }
    }
}
