/*
* Author: Chase Craig
* Purpose: This file contains logic for the birds to navigate around in the scene.
* Last Updated: 10/27/19
* 
* Todo: Implement division of space for performance increase.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BirdLogic : MonoBehaviour
{
    // These variables are local so that they could be changed in real time.

    // The number of rays to shoot out radially to determine next location to explore, note this is the base amount, includes those excluded by search arc.
    public int numRays = 1000;
    // The angle from straight forward to behind to search, in degrees
    public float searchArc = 120f; 
    // The radius of the bird to use in raycasting a sphere
    public float sphereCheckRad = 0.15f;
    // The layers to consider when raycasting
    public LayerMask targetLayer = 0;

    // The strength of each rule for the birds, note it should be less than 1 usually.
    public Vector3 ruleStrengths = new Vector3(0.9f, 0.15f, 0.2f);
    // The distance of which the birds will try to maintain at minimum between any other bird
    public float dodgeDistance = 0.3f;

    public float speed = 0.5f;
    public float slerpAmount = 10f;
    // Start is called before the first frame update
    public float flockRange = .5f;


    ComputeBuffer LocationsOfBirds;
    ComputeBuffer DirectionsOfBirds;
    ComputeBuffer NewDirections;
    public void Start()
    {
        int kern = FindBirds.FindKernel("FindDirection");
        LocationsOfBirds = new ComputeBuffer(MAX_BIRDS, 4 * sizeof(float));
        DirectionsOfBirds = new ComputeBuffer(MAX_BIRDS, 4*sizeof(float));
        NewDirections = new ComputeBuffer(MAX_BIRDS, 4*sizeof(float));
        FindBirds.SetBuffer(kern, "Locations", LocationsOfBirds);
        FindBirds.SetBuffer(kern, "Direction", DirectionsOfBirds);
        FindBirds.SetBuffer(kern, "NewDirection", NewDirections);
    }
    public void Update()
    {
        LogicalUpdate();
        // Originally housed some other code...
       
    }

    public void LogicalUpdate()
    {
        int kern = FindBirds.FindKernel("FindDirection");
        if(otherBirds.Count != 0)
        {
            // Cool....
            // This will manually change the movement....

            FindBirds.SetFloat("FlockDistance", flockRange);
            FindBirds.SetFloat("DodgeDistance", dodgeDistance);
            FindBirds.SetInt("NumberOfBirds", otherBirds.Count);
            FindBirds.SetVector("totalPullPosition", new Vector4());
            FindBirds.SetFloat("pullAmount", 0);
            FindBirds.SetVector("RuleStrengths", ruleStrengths);

            Vector4[] birdLocs = new Vector4[otherBirds.Count];
            Vector4[] birdDir = new Vector4[otherBirds.Count];
            Vector4[] birdNewDir = new Vector4[otherBirds.Count];
            for (int i = 0; i < otherBirds.Count; i++)
            {
                birdLocs[i] = otherBirds[i].transform.position;
                birdDir[i] = otherBirds[i].transform.forward;
            }
            LocationsOfBirds.SetData(birdLocs, 0, 0, birdLocs.Length);
            DirectionsOfBirds.SetData(birdDir, 0, 0, birdDir.Length);
            FindBirds.SetBuffer(kern, "Locations", LocationsOfBirds);
            FindBirds.SetBuffer(kern, "Direction", DirectionsOfBirds);
            var CT = System.DateTime.UtcNow;
            FindBirds.Dispatch(kern, otherBirds.Count, 1, 1);
            Debug.Log((System.DateTime.UtcNow- CT).Milliseconds);
            NewDirections.GetData(birdNewDir, 0, 0, otherBirds.Count);


            for (int i = 0; i < otherBirds.Count; i++)
            {
                if (Mathf.Approximately(birdNewDir[i].w, 0))
                {
                    SetNewMovement(otherBirds[i], i, Vector3.zero, new float[] { numRays, searchArc, sphereCheckRad, targetLayer.value, speed, slerpAmount });
                }
                else
                {
                    SetNewMovement(otherBirds[i], i, new Vector3(birdNewDir[i].x,birdNewDir[i].y, birdNewDir[i].z)/birdNewDir[i].w, new float[] { numRays, searchArc, sphereCheckRad, targetLayer.value, speed, slerpAmount });
                }
               // Debug.Log(birdNewDir[i]);
                
            }
        }
    }


    // Static variables to control the logic of how the birds will move. The issue is that static variables aren't shown in the inspector for unity.
   
    public static List<BirdMovement> otherBirds = new List<BirdMovement>();
    public static Dictionary<BirdMovement, Vector3> currentBirdsLoc = new Dictionary<BirdMovement, Vector3>();

    public static int MAX_BIRDS = 512;

    public ComputeShader FindBirds;

    public static List<Vector3> DirectionalValues = new List<Vector3>();
    public static List<Vector3> UpDirectionalValues = new List<Vector3>();

    public static int RegisterBird(BirdMovement bm)
    {
        otherBirds.Add(bm);
        currentBirdsLoc.Add(bm, bm.transform.position);
        DirectionalValues.Add(bm.transform.forward);
        UpDirectionalValues.Add(bm.transform.up);
        return otherBirds.Count;
    }

    public static Vector3[] directions;

    void OnApplicationQuit()
    {
        LocationsOfBirds.Dispose();
        DirectionsOfBirds.Dispose();
        NewDirections.Dispose();
    }
    // This class is meant to purely compute the next location the bird object should fly towards.
    public static void SetNewMovement(BirdMovement bm, int index, Vector3 newDir, float[] param)
    {
        
        // Slowly lerp the rotation to the target direction
        currentBirdsLoc[bm] = bm.transform.position;
        // First lets search to see if any points are free
        Quaternion qq = bm.transform.rotation;
        int __numRays = Mathf.FloorToInt(param[0]);
        float __searching = param[1];
        Vector3 selection = qq *Quaternion.Inverse(bm.transform.localRotation)* bm.targetDirection;
        float golden = (1 + Mathf.Sqrt(5f));
        bool dodging = false;
        for (int i = 0; i < __numRays; i++)
        {
            float phi = Mathf.Acos(1f - 2f * (i + .5f) / (__numRays+.5f));
            float theta = Mathf.PI * golden * (i+.5f);
            Vector3 temp = qq * new Vector3(Mathf.Cos(theta) * Mathf.Sin(phi), Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(phi));
            if (phi > __searching)
            {
                break;
            }
            else if (Physics.SphereCast(new Ray(bm.transform.position, temp), param[2], param[4] * Time.smoothDeltaTime * 2f, (int)param[3])) 
            {
                selection -= temp;
                dodging = true;
            }
        }
        if (!dodging)
        {
            selection += qq*Quaternion.Inverse(bm.transform.localRotation)*newDir;
        }
        //Debug.Log(selection + " " + newDir + " " + index);
        bm.targetDirection = bm.transform.localRotation*Quaternion.Inverse(qq)*selection.normalized;

        bm.transform.localRotation = Quaternion.Slerp(bm.transform.localRotation, Quaternion.LookRotation(bm.targetDirection, bm.transform.up), param[5] * Time.deltaTime);
        // Technically make them slow down if they are going to hit something....
        float dist = 1;
        while (true)
        {
            if (Physics.Raycast(bm.transform.position, bm.transform.forward, bm.transform.forward.magnitude * param[4] * Time.smoothDeltaTime * dist * 1.25f))
            {
                bm.transform.localRotation = Quaternion.Slerp(bm.transform.localRotation, Quaternion.LookRotation(bm.targetDirection, bm.transform.up), param[5] * Time.deltaTime);
                dist *= .15f;
            }
            else
            {
                break;
            }
        }
        bm.transform.localPosition += bm.transform.forward * param[4] * Time.smoothDeltaTime * dist;
    }
}
