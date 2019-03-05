using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Connection_List : MonoBehaviour {

    public List<Connection_Data> connections = new List<Connection_Data>();
    public float elevation = 0.1f;

    public  Material route_col;
    public  Material pov_col;
    public  Material miss_col;
    public  Material tile_col;

    public float cost_so_far = 0;
    public float heuristic_value = 0;

    public void removeConnection(GameObject gorm)
    {
        bool found = false;
        foreach(Connection_Data cd in connections)
        {
            if(gorm == cd.connection)
            {
                cd.traveled = true;
                found = true;
            }
        }
        if (!found) Debug.Log("NOT FOUND");
    }

    public void drawConnectionLines()
    {
        this.GetComponent<MeshRenderer>().material = tile_col;
        LineRenderer line_rend = this.GetComponent<LineRenderer>();
        line_rend.useWorldSpace = true;
        line_rend.positionCount = (connections.Count * 3);
        line_rend.material = tile_col;
        line_rend.startWidth = (0.05f);
        line_rend.endWidth = (0.05f);
        int index3 = 0;
        foreach (Connection_Data cd in connections) {
            if(cd.connection != null) {  
                line_rend.SetPosition(index3 * 3 + 0, this.transform.position + new Vector3(0, elevation, 0));
                line_rend.SetPosition(index3 * 3 + 1, cd.connection.transform.position + new Vector3(0, elevation, 0));
                line_rend.SetPosition(index3 * 3 + 2, this.transform.position + new Vector3(0, elevation, 0));
            }
            else
            {
                line_rend.SetPosition(index3 * 3 + 0, this.transform.position + new Vector3(0, elevation, 0));
                line_rend.SetPosition(index3 * 3 + 1, this.transform.position + new Vector3(0, elevation, 0));
                line_rend.SetPosition(index3 * 3 + 2, this.transform.position + new Vector3(0, elevation, 0));
            }
            index3++;
        }
    }
}
