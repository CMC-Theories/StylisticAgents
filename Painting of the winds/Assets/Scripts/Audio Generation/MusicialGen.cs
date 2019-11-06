using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicialGen : MonoBehaviour
{
    public int position = 0;
    public int samplerate = 2000; // low but we don't need higher...
    public float frequency = 440;
    public Transform leftMostPlane, rightMostPlane, topMostPlane, bottomMostPlane, farPlane, closePlane;
    public float minFreq = 50;
    public float maxFreq = 250;

    // The idea is to add up additional frequencies
    Vector3 LMP, RMP, TMP, BMP, FP, CP;
    void Start()
    {
        LMP = leftMostPlane.position;
        RMP = rightMostPlane.position;
        TMP = topMostPlane.position;
        BMP = bottomMostPlane.position;
        FP = farPlane.position;
        CP = closePlane.position;
        Debug.Log("" + LMP + " " + RMP + " " + TMP + " " + BMP + " " + FP + " " + CP);
        AudioClip myClip = AudioClip.Create("MS", samplerate /4, 2, samplerate, true, OnAudioRead, OnAudioSet);
        AudioSource aud = GetComponent<AudioSource>();
        aud.clip = myClip;
        aud.loop = true;
        aud.Play();
    }
    public void OnAudioRead(float[] data) {
        int count = 0;
        int copys = BirdLogic.otherBirds.Count;
        int op = position;
        for(int i = 0; i < data.Length; i++)
        {
            data[i] = 0;
        }

        foreach (BirdMovement bm in BirdLogic.otherBirds)
        {
            float targFreq = (BirdLogic.currentBirdsLoc[bm].x - LMP.x) / (RMP.x - LMP.x)*(maxFreq - minFreq) + minFreq;
            count = 0;
            Debug.Log(targFreq);
            position = op;
            while (count < data.Length)
            {
                data[count] += Mathf.Sin(position *
                    targFreq * Mathf.PI /samplerate)/((float)copys);
                count++;
                position++;
            }
        }
    }
    public void OnAudioSet(int np) { position = np; }
}
