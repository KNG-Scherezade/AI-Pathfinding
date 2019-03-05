using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connection_Data {

    public GameObject connection;
    public GameObject parent;
    public float weight;
    public bool traveled = false;


    public Connection_Data(GameObject node_connection, float edge_weight, GameObject parent_node)
    {
        connection = node_connection; 
        weight = edge_weight;
        parent = parent_node;
    }


} 
