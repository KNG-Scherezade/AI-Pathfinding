using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotController : MonoBehaviour
{

    Map_Gen map;
    Transform robot;
    GameObject[] rooms;
    List<GameObject> small_rooms = new List<GameObject>();
    List<GameObject> route;

    GameObject pov_start;
    GameObject pov_target;
    GameObject tile_start;
    GameObject tile_target;

    bool active = false;
    bool aligned = false;
    bool arived = false;

    public float rotation_timestep = 0.2f;
    public float movement_timestep = 0.1f;
    public Material route_col;
    public float node_switch_radius = 0.5f;
    public float padding = 1.0f;
    public float bounding_angle = 179.8f;
    public float forced_stop_angle = 80.0f;
    public float t2a = 1.0f;



    // Use this for initialization
    void Start()
    {
        Debug.Log("rc start");
        map = GameObject.FindGameObjectWithTag("GameController").GetComponent<Map_Gen>();
        robot = GameObject.FindGameObjectWithTag("Player").transform;
        rooms = GameObject.FindGameObjectsWithTag("Room");
        foreach (GameObject room in rooms)
        {
            if (room.name.Contains("Closet") || room.name.Contains("closet"))
            {
                small_rooms.Add(room);
                Debug.Log("cl add " + room.name);
            }
        }
    }

    void steeringAlign(GameObject target)
    {
        Vector3 roboto_target = new Vector3(robot.GetComponent<BoxCollider>().bounds.center.x - target.transform.position.x, 0, robot.GetComponent<BoxCollider>().bounds.center.z - target.transform.position.z).normalized;
        float angle_between = -Vector3.SignedAngle(robot.forward, roboto_target, new Vector3(0, 1.0f, 0));
        if (Mathf.Abs(angle_between) >= bounding_angle)
        {
            aligned = true;
            return;
        }
        if(Mathf.Abs(angle_between) < forced_stop_angle)
        {
            aligned = false;
        }
        float angle_this_frame = (Mathf.Sign(angle_between) * 180 - angle_between) * rotation_timestep;
        Debug.Log(angle_between + " " + target.transform.position + " " + angle_this_frame);
        robot.transform.Rotate(new Vector3(0, 1.0f, 0), angle_this_frame);
    }

    void Update()
    {

        robot.GetComponent<Animator>().SetBool("isWalking", false);
        robot.GetComponent<Animator>().SetBool("isRunning", false);
        if (active && route != null)
        {
            // get into proper angle immediately
            if (!aligned) {
                steeringAlign(route[route.Count - 1]);
            }
            else
            {
                if (route.Count > 1)
                {
                    robot.GetComponent<Animator>().SetBool("isRunning", true);
                    steeringAlign(route[route.Count - 1]);
                    robot.transform.position += robot.transform.forward * movement_timestep;
                    Vector3 roboto_target = new Vector3(
                        robot.GetComponent<BoxCollider>().bounds.center.x - route[route.Count - 1].transform.position.x,
                        0,
                        robot.GetComponent<BoxCollider>().bounds.center.z - route[route.Count - 1].transform.position.z);
                    if (roboto_target.magnitude < node_switch_radius)
                    {
                        route.RemoveAt(route.Count - 1);
                    }
                }
                else if(route.Count == 1)
                {
                    robot.GetComponent<Animator>().SetBool("isWalking", true);
                    steeringAlign(route[route.Count - 1]);
                    robot.transform.position += robot.transform.forward * movement_timestep * (robot.transform.position - route[route.Count - 1].transform.position).magnitude * t2a;
                    Vector3 roboto_target = new Vector3(
                        robot.GetComponent<BoxCollider>().bounds.center.x - route[route.Count - 1].transform.position.x,
                        0,
                        robot.GetComponent<BoxCollider>().bounds.center.z - route[route.Count - 1].transform.position.z);
                    if (roboto_target.magnitude < node_switch_radius)
                    {
                        GameObject new_start = route[route.Count - 1];
                        route.RemoveAt(route.Count - 1);
                        reset(new_start);
                    }
                }
                else
                {
                    Debug.Log("No Routes");
                }
            }
        }

        //stop character
        if (Input.GetKeyDown("space"))
        {
            active = false;
        }
        //camera
        if (Input.GetKeyDown("left shift"))
        {
            active = false;
        }
        //start and stop character
        if (Input.GetKeyDown("right shift"))
        {
            Debug.Log("robot move");
            active = !active;
        }
        if (Input.GetKeyDown("\\"))
        {
            Debug.Log("reset full");
            map.resetGraphs();

            pov_start = null;
            pov_target = null;
            tile_start = null;
            tile_target = null;
            route = null;
            aligned = false;
            arived = false;

            //home
            int r = Random.Range(0, small_rooms.Count);
            GameObject random_room_home = small_rooms[r];

            List<GameObject> floors_home = new List<GameObject>();
            foreach (Transform component in random_room_home.transform)
            {
                if (component.gameObject.name.Contains("floor") || component.gameObject.name.Contains("Floor"))
                {
                    floors_home.Add(component.gameObject);
                }
            }

            //target
            r++;
            if (r > small_rooms.Count - 1) r = 0;

            GameObject random_room_target = small_rooms[r];

            List<GameObject> floors_target = new List<GameObject>();
            foreach (Transform component in random_room_target.transform)
            {
                if (component.gameObject.name.Contains("floor") || component.gameObject.name.Contains("Floor"))
                {
                    floors_target.Add(component.gameObject);
                }
            }

            //home
            r = Random.Range(0, floors_home.Count - 1);
            GameObject random_floor = floors_home[r];

            Vector3 position = new Vector3(
                Random.Range(random_floor.GetComponent<Renderer>().bounds.min.x + padding, random_floor.GetComponent<Renderer>().bounds.max.x - padding),
                0.0f,
                Random.Range(random_floor.GetComponent<Renderer>().bounds.min.z + padding, random_floor.GetComponent<Renderer>().bounds.max.z - padding));

            robot.SetParent(random_room_home.transform);
            robot.position = position;

            float detect_range = 1.0f;
            int i = 0;
            while(pov_start == null && tile_start == null && i++ != 100) {
                Collider[] overlaps = Physics.OverlapSphere(position, detect_range, LayerMask.GetMask("PostProcessing"));
                detect_range += 0.1f;
                foreach (Collider overlap in overlaps)
                {
                    if (overlap.gameObject.name.Contains("PoV"))
                    {
                        if (ClusterFunctions.room_pov_association[random_room_home.name].Contains(overlap.gameObject))
                        {
                            pov_start = overlap.gameObject;
                            pov_start.GetComponent<MeshRenderer>().material = route_col;
                            Debug.Log(pov_start.transform.position);
                            break;
                        }
                    }
                    else if (overlap.gameObject.name.Contains("Sphere"))
                    {
                        if (ClusterFunctions.room_tile_association[random_room_home.name].Contains(overlap.gameObject))
                        {
                            tile_start = overlap.gameObject;
                            tile_start.GetComponent<MeshRenderer>().material = route_col;
                            Debug.Log(tile_start.transform.position);
                            break;
                        }
                    }
                }
            }

            //target
            r = Random.Range(0, floors_target.Count - 1);
            random_floor = floors_target[r];

            position = new Vector3(
                Random.Range(random_floor.GetComponent<Renderer>().bounds.min.x + padding, random_floor.GetComponent<Renderer>().bounds.max.x - padding),
                0.0f,
                Random.Range(random_floor.GetComponent<Renderer>().bounds.min.z + padding, random_floor.GetComponent<Renderer>().bounds.max.z - padding));

            detect_range = 1.0f;
            i = 0;
            while (pov_target == null && tile_target == null && i++ != 100)
            {
                Collider[] overlaps = Physics.OverlapSphere(position, detect_range, LayerMask.GetMask("PostProcessing"));
                detect_range += 0.1f;
                foreach (Collider overlap in overlaps)
                {
                    if (overlap.gameObject.name.Contains("PoV"))
                    {
                        Debug.Log(ClusterFunctions.room_pov_association[random_room_target.name].Contains(overlap.gameObject));
                        Debug.Log(ClusterFunctions.room_pov_association[random_room_target.name]);
                        Debug.Log(ClusterFunctions.room_pov_association);
                        if (ClusterFunctions.room_pov_association[random_room_target.name].Contains(overlap.gameObject))
                        {
                            pov_target = overlap.gameObject;
                            pov_target.GetComponent<MeshRenderer>().material = route_col;
                            Debug.Log(pov_target.transform.position);
                            break;
                        }
                    }
                    else if (overlap.gameObject.name.Contains("Sphere"))
                    {
                        if (ClusterFunctions.room_tile_association[random_room_target.name].Contains(overlap.gameObject))
                        {
                            tile_target = overlap.gameObject;
                            tile_target.GetComponent<MeshRenderer>().material = route_col;
                            Debug.Log(tile_target.transform.position);
                            break;
                        }
                    }
                }
            }
            if (map.tile_node_obj.activeInHierarchy)
            {
                (route = map.findAStarRoute(
                    tile_start.GetComponent<Connection_List>(),
                    tile_target.GetComponent<Connection_List>(),
                    Map_Gen.Heuristic_Search.Cluster,
                    Map_Gen.Categories.Tile)).Insert(0,tile_target);
            }
            else
            {
                (route = map.findAStarRoute(
                    pov_start.GetComponent<Connection_List>(),
                    pov_target.GetComponent<Connection_List>(),
                    Map_Gen.Heuristic_Search.Cluster,
                    Map_Gen.Categories.PoV)).Insert(0,pov_target);
            }
        }
    }

    void reset(GameObject new_start)
    {
        Debug.Log("reset");
        map.resetGraphs();

        if (map.tile_node_obj.activeInHierarchy)
        {
            tile_start = new_start;
            tile_target = null;
        }
        else
        {
            pov_start = new_start;
            pov_target = null;
        }
        route = null;
        aligned = false;
        arived = false;


        //target
        
        int r = Random.Range(0, small_rooms.Count);
        GameObject random_room_target = small_rooms[r];

        while (ClusterFunctions.room_pov_association[random_room_target.name].Contains(new_start) || ClusterFunctions.room_tile_association[random_room_target.name].Contains(new_start)) {
            r = Random.Range(0, small_rooms.Count);
            random_room_target = small_rooms[r];
        }

        List<GameObject> floors_target = new List<GameObject>();
        foreach (Transform component in random_room_target.transform)
        {
            if (component.gameObject.name.Contains("floor") || component.gameObject.name.Contains("Floor"))
            {
                floors_target.Add(component.gameObject);
            }
        }

        //target
        r = Random.Range(0, floors_target.Count - 1);
        GameObject random_floor = floors_target[r];

        Vector3 position = new Vector3(
            Random.Range(random_floor.GetComponent<Renderer>().bounds.min.x + padding, random_floor.GetComponent<Renderer>().bounds.max.x - padding),
            0.0f,
            Random.Range(random_floor.GetComponent<Renderer>().bounds.min.z + padding, random_floor.GetComponent<Renderer>().bounds.max.z - padding));

        float detect_range = 1.0f;
        int i = 0;
        while (pov_target == null && tile_target == null && i++ != 100)
        {
            Collider[] overlaps = Physics.OverlapSphere(position, detect_range, LayerMask.GetMask("PostProcessing"));
            detect_range += 0.1f;
            foreach (Collider overlap in overlaps)
            {
                if (overlap.gameObject.name.Contains("PoV"))
                {
                    if (ClusterFunctions.room_pov_association[random_room_target.name].Contains(overlap.gameObject))
                    {
                        pov_target = overlap.gameObject;
                        pov_target.GetComponent<MeshRenderer>().material = route_col;
                        Debug.Log(pov_target.transform.position);
                        break;
                    }
                }
                else if (overlap.gameObject.name.Contains("Sphere"))
                {
                    if (ClusterFunctions.room_tile_association[random_room_target.name].Contains(overlap.gameObject))
                    {
                        tile_target = overlap.gameObject;
                        tile_target.GetComponent<MeshRenderer>().material = route_col;
                        Debug.Log(tile_target.transform.position);
                        break;
                    }
                }
            }
        }
        if (map.tile_node_obj.activeInHierarchy)
        {
            (route = map.findAStarRoute(
                tile_start.GetComponent<Connection_List>(),
                tile_target.GetComponent<Connection_List>(),
                Map_Gen.Heuristic_Search.Cluster,
                Map_Gen.Categories.Tile)).Insert(0, tile_target);
        }
        else
        {
            (route = map.findAStarRoute(
                pov_start.GetComponent<Connection_List>(),
                pov_target.GetComponent<Connection_List>(),
                Map_Gen.Heuristic_Search.Cluster,
                Map_Gen.Categories.PoV)).Insert(0, pov_target);
        }
    }
}
