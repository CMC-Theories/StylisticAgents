
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreFlyGeneration : MonoBehaviour
{

    Dictionary<int, AudioSource> clips = new Dictionary<int, AudioSource>();
    public GameObject audioTemplate;
    public void Start()
    {
        if(audioTemplate == null)
        {
            Debug.LogError("An AUDIO TEMPLATE was not assigned!");
            
        }
        MusicialGen.PDP = Application.persistentDataPath;
        
    }

    public AudioSource getAudioKey(int k)
    {
        if (!clips.ContainsKey(k))
        {
            float[] ss = getKey(k);
            AudioClip myClip = AudioClip.Create("Key_" + k, ss.Length, 1, 44100 * 3, false);
            myClip.SetData(ss, 0);
            GameObject newAudioObject = Instantiate(audioTemplate);
            newAudioObject.name = "AudioClip_" + k;
            AudioSource aud = newAudioObject.GetComponent<AudioSource>();
            aud.clip = myClip;
            aud.loop = true;
            aud.Play();
            clips.Add(k, aud);

        }

        return clips[k];
    }

    public const float M_TAU = 6.28318530718f;
    float[][] keys = new float[32][];
    // 32 keys
    private float[] getKey(int k)
    {
        
        float AK = 440.0f * Mathf.Pow(2.0f, (k - 49.0f) / 12.0f) * M_TAU / 44100f;

        // The idea is to generate frequencies....
        int NK = 1; // Current index key
        int BNK = 1; // Best index key
        float best = 100; // Best error from 0
        float desired = 0.01f; // Desired error from 0, early termination if below this threshold
        bool WN = false; // Was negative, only want to end on a phase that is approximately 2*pi
        while (NK < 44100) // Only run until 44100 samples.
        {
            if(WN && Mathf.Abs(Mathf.Sin(AK * NK)) < best)
            {
                BNK = NK;
                best = Mathf.Abs(Mathf.Sin(AK * NK));
                if(desired >= best)
                {
                    break;
                }
            }
            WN = Mathf.Sin(AK * NK) < 0;
            NK++;
        }
        float[] values = new float[(int)((BNK+1)*3)]; // Generate 3 times the amount of samples, the idea is to give better waves at higher frequencies.
        Debug.Log(values.Length);
        for(int i = 0; i < values.Length; i++)
        {
            values[i] = Mathf.Sin(AK * (i / 3f)); // Account for the 3 times stretching
            if (Mathf.FloorToInt(k / 4) % 2 == 1)
            {
                // Make it more like a piano, with an exponential rolloff of multiple sine waves...
                // 3 overtones...
                values[i] += Mathf.Pow(2f, -1)*Mathf.Sin(2f*AK * (i/3f));
                values[i] += Mathf.Pow(2f, -2) * Mathf.Sin(4f * AK * (i / 3f));
                values[i] += Mathf.Pow(2f, -3) * Mathf.Sin(6f * AK * (i / 3f));
                values[i] += Mathf.Pow(2f, -4) * Mathf.Sin(8f * AK * (i / 3f));
                values[i] /=(2f - (Mathf.Pow(2f, -4))); // 2^-4(2^4 + 2^3 + 2^2 + 2^1 + 1) = (2^5 - 1)/2^4
            }
        }
        
        return values;
    }
}
