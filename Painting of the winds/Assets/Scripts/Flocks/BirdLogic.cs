using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BirdLogic : MonoBehaviour
{

    public int numRays = 1000;
    public float searchArc = 120f; // Note in degrees
    public float searchRange = 1f;
    public float sphereCheckRad = 0.15f;

    public LayerMask targetLayer = 0;
    public void Update()
    {
        if(numRays != _numRays)
        {
            _numRays = numRays;
        }
        if(!Mathf.Approximately(searchArc,_searchArc))
        {
            _searchArc = searchArc;
            _searching = (120f / 360f) * 2 * Mathf.PI;
        }
        if(!Mathf.Approximately(searchRange, _searchRange))
        {
            _searchRange = searchRange;
        }
        if(!Mathf.Approximately(sphereCheckRad, _sphereCheckRad))
        {
            _sphereCheckRad = sphereCheckRad;
        }
        if(targetLayer != _targetLayer)
        {
            _targetLayer = targetLayer;
        }
    }


    public static int _numRays = 1000;
    public static float _searchArc = 120f;
    private static float _searching = (120f / 360f) * 2 * Mathf.PI;
    public static float _searchRange = 1f;
    public static float _sphereCheckRad = 0.15f;
    public static LayerMask _targetLayer = 0;
    // This class is meant to purely compute the next location the bird object should fly towards.
    public static Vector3 GetNewMovement(BirdMovement bm)
    {
        // First lets search to see if any points are free
        Quaternion qq = bm.transform.rotation;
        int __numRays = _numRays;
        float __searching = _searching;
        Vector3 selection = Vector3.zero;
        float golden = (1 + Mathf.Sqrt(5f));
        bool first_hit = true;
        for (int i = 0; i < __numRays; i++)
        {

            float phi = Mathf.Acos(1f - 2f * (i) / _numRays);
            float theta = Mathf.PI * golden * (i);
            selection =qq* new Vector3(Mathf.Cos(theta)*Mathf.Sin(phi), Mathf.Sin(theta)*Mathf.Sin(phi), Mathf.Cos(phi));
            Debug.DrawRay(bm.transform.position, selection, Color.blue);
            if(phi > _searching)
            {
                break;
            }
            else if(!Physics.SphereCast(new Ray(bm.transform.position, selection), _sphereCheckRad, _searchRange, _targetLayer))
            {
                break;
            }
            first_hit = false;
        }
        return Quaternion.Inverse(qq)*bm.transform.localRotation*selection;
        

    }
}
