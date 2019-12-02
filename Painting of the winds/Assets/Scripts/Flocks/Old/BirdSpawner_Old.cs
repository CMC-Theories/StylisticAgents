using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdSpawner_Old : MonoBehaviour
{
    public BirdMovement_Old bird;
    public int numBirds = 10;
    // Start is called before the first frame update
    void Start()
    {
        for(int i =0; i < numBirds; i++)
        {
            GameObject GO = Instantiate(bird.gameObject, this.transform);
            GO.transform.localPosition = Random.onUnitSphere;
            BirdMovement_Old birdMove = GO.GetComponent<BirdMovement_Old>();
            birdMove.targetDirection = Random.onUnitSphere;
            birdMove.enabled = true;
            TrailRenderer trail = GO.GetComponentInChildren<TrailRenderer>();
            trail.startColor = Random.ColorHSV(0,1f, .9f, 1f, 0.9f, 1.0f,.99f,1.0f);
            trail.endColor = new Color(0, 0, 0, 0f);
            Light ll = GO.GetComponentInChildren<Light>();
            ll.color = trail.startColor;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
