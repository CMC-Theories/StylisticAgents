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
    // The division of space, larger = less boxes to check but more birds to check (edge cases), smaller = more boxes to check but less birds to check (fewer edge cases)
    public float flockDivision = 5f;
    // The strength of each rule for the birds, note it should be less than 1 usually.
    public Vector3 ruleStrengths = new Vector3(0.9f, 0.15f, 0.2f);
    // The distance of which the birds will try to maintain at minimum between any other bird
    public float dodgeDistance = 0.3f;

    
    public void Start()
    {
        _numRays = numRays;
        _searchArc = searchArc;
        _searching = (_searchArc / 360f) * 2 * Mathf.PI;
        _sphereCheckRad = sphereCheckRad;
        _targetLayer = targetLayer;
        _ruleStrengths = ruleStrengths;
        _dodgeDist = dodgeDistance;

    }
    public void Update()
    {
        if(numRays != _numRays)
        {
            _numRays = numRays;
        }
        if(!Mathf.Approximately(searchArc,_searchArc))
        {
            _searchArc = searchArc;
            _searching = (_searchArc / 360f) * 2 * Mathf.PI;
        }
        
        if(!Mathf.Approximately(sphereCheckRad, _sphereCheckRad))
        {
            _sphereCheckRad = sphereCheckRad;
        }
        if(targetLayer != _targetLayer)
        {
            _targetLayer = targetLayer;
        }
        if(!Mathf.Approximately(Mathf.Epsilon,Vector3.SqrMagnitude(_ruleStrengths- ruleStrengths)))
        {
            _ruleStrengths = ruleStrengths;
        }
        if(!Mathf.Approximately(dodgeDistance, _dodgeDist))
        {
            _dodgeDist = dodgeDistance;
        }
    }

    // Static variables to control the logic of how the birds will move. The issue is that static variables aren't shown in the inspector for unity.
    public static int _numRays = 1000; 
    public static float _searchArc = 120f;
    private static float _searching = (120f / 360f) * 2 * Mathf.PI;
    public static float _sphereCheckRad = 0.15f;
    public static LayerMask _targetLayer = 0;
    public static Vector3 _ruleStrengths = new Vector3(0.9f, 0.15f, 0.2f);
    public static float _dodgeDist = 0.3f;

    public static List<BirdMovement> otherBirds = new List<BirdMovement>();
    public static bool inuse = false;
    public static Dictionary<Vector3Int, HashSet<BirdMovement>> considerationNodes = new Dictionary<Vector3Int, HashSet<BirdMovement>>();
    public static void RegisterBird(BirdMovement bm)
    {
        otherBirds.Add(bm);
        
    }
    // This class is meant to purely compute the next location the bird object should fly towards.
    public static Vector3 GetNewMovement(BirdMovement bm)
    {
        // First lets search to see if any points are free
        Quaternion qq = bm.transform.rotation;
        int __numRays = _numRays;
        float __searching = _searching;
        Vector3 selection = qq *Quaternion.Inverse(bm.transform.localRotation)* bm.targetDirection;
        float golden = (1 + Mathf.Sqrt(5f));
        bool dodging = false;
        for (int i = 0; i < __numRays; i++)
        {
            
            float phi = Mathf.Acos(1f - 2f * (i + .5f) / (_numRays+.5f));
            float theta = Mathf.PI * golden * (i+.5f);
            Vector3 temp = qq * new Vector3(Mathf.Cos(theta) * Mathf.Sin(phi), Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(phi));
            //Debug.DrawRay(bm.transform.position, temp, Color.blue);
            if(phi > _searching)
            {
                break;
            }
            else if(Physics.SphereCast(new Ray(bm.transform.position, temp), _sphereCheckRad, bm.speed * Time.smoothDeltaTime*2f, _targetLayer))
            {
                selection -= temp;
                dodging = true;
            }
        }

        if (dodging)
        {
            Debug.Log("Dodging...");
            return bm.transform.localRotation * Quaternion.Inverse(qq) * selection.normalized;
        }


        Vector3 aveflock = bm.transform.position;
        bool seenOthers = false;
        int numberSeen = 1;
        Vector3 aveVel = selection;
        foreach(BirdMovement bmm in otherBirds)
        {
            if (!bmm.Equals(bm))
            {
                float distanceToOther = Vector3.Distance(bmm.transform.position, bm.transform.position);
                if (distanceToOther < bm.flockRange)
                {
                    seenOthers = true;
                    aveflock += bmm.transform.position;
                    numberSeen++;
                    // But like...yeah
                    if (distanceToOther < _dodgeDist)
                    {
                        selection -= (bmm.transform.position - bm.transform.position) * _ruleStrengths.x;
                    }
                    aveVel +=bmm.transform.forward;
                }

            }
        }
        if (seenOthers)
        {
            //Debug.DrawLine(bm.transform.position, (aveflock/numberSeen), Color.red);
            selection += ((aveflock/numberSeen)-bm.transform.position).normalized * _ruleStrengths.z;
            selection += (aveVel / numberSeen).normalized * _ruleStrengths.y;
        }

        return bm.transform.localRotation*Quaternion.Inverse(qq)*selection.normalized;
        

    }
}
