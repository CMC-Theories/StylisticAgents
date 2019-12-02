using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicialGen : MonoBehaviour
{

    /*
     * After further digging:
     *      No one __can__ access information directly from the 
     *      buffers inside of the function that generates audio (since
     *      it is not the main thread.)
     *      
     *      The only workaround is to possibly have an array that is
     *      modified on the main thread, and read in the function.
     * 
     *      This will clearly lead to some desyncs, so as a work around
     *      we can turn to fourier. The idea is that we will only allow
     *      certain frequencies to be represented.
     *      
     *      The compute shader will figure out the volume of the birds
     *      and perform the fourier transform (tenatively).
     *      
     *      This amplitude data will then be fed back into the audio generator.
     *      It will need to do 2 things:
     *          Prevent clipping/clicking, this means we simply need
     *              to have a counter in addition to position, pos dictates
     *              where to write data out, counter is effectively time.
     *              In addition, we will need to phase sync the waves, and
     *              deal with waves decreasing in amplitude, this could be
     *              best done with an quadratic/sloping envelope. The idea is 
     *              that over the range of the data, we have the envelope take
     *              action and dampen/increase the volume of the wave.
     *          Optimize the Sine function. Since we are generating audio data,
     *              we need to perform upwards of (size of data to generate * 
     *              number of channels), this will be terrible in the long run
     *              as it effectively means that it will run almost n^2 time, on
     *              top of having to evaluate sine. By precomputing sine for a 
     *              lot of points, we can then simply interpolate the points inbetween.
     *              Also we don't need pristine sine curves, we can get away with approximated curves.
     *              
     *              
     *          
     */

    public enum Style{
        ClosestFFT,
        Chords
    }

    public Style musicStyle = Style.Chords;
    public long positionit = 0; // This just keeps counting no matter what.
    public int samplerate = 2000; // low but we don't need higher...
    public Transform leftMostPlane, rightMostPlane, topMostPlane, bottomMostPlane, farPlane, closePlane;
    public float minFreq = 50;
    public float maxFreq = 250;

    // The keys that the tone generates will pick out
    public int MinKey = 15;
    public int KeySkip = 4;

    public PreFlyGeneration audioGen;

    [Range(32,2048)]
    public int GenerationAmt = 512;

    public static int NumberOfTones = 16;
    
    float[] currentAmp = new float[NumberOfTones];
    float[] previousAmp = new float[NumberOfTones];

    /*
     *The idea is that the previous amp will ONLY
     * be changed by the audio generator.
    */

    public ComputeShader PianoFinder;
    ComputeBuffer GPFreq;
    ComputeBuffer GPLocations;
    ComputeBuffer GPKeys;


    public float refreshRate = 10f; // 10 hertz
    public float lastRefresh = -2000f;
    float[] generationFreq = new float[NumberOfTones];
    void Start()
    {
        currentAmp = new float[NumberOfTones];
        previousAmp = new float[NumberOfTones];
        generationFreq = new float[NumberOfTones];
        PDP = Application.persistentDataPath;
        for (int i = 0; i < NumberOfTones; i++)
        {
            // Note that the frequencies used are
            //  always of (2pi * f / N) * t
            previousAmp[i] = 0;
            currentAmp[i] = 0;
        }

        GPFreq = new ComputeBuffer(currentAmp.Length, sizeof(float));
        GPKeys = new ComputeBuffer(generationFreq.Length, sizeof(float));
        GPKeys.SetData(generationFreq);
        GPFreq.SetData(currentAmp);
        GPLocations = new ComputeBuffer(BirdLogic.MAX_BIRDS, sizeof(float) * 4);
        int kern = FetchKernel();
        
        PianoFinder.SetBuffer(kern, "Loudness", GPFreq);
        PianoFinder.SetBuffer(kern, "GenKeys", GPKeys);
        // Change
        
        PianoFinder.SetInt("KeyStart", MinKey);
        PianoFinder.SetInt("KeyJump", KeySkip);
        PianoFinder.SetInt("GenerationAmt", GenerationAmt);
        PianoFinder.SetVector("LeftBound", new Vector3(Mathf.Min(leftMostPlane.position.x, rightMostPlane.position.x), 
            Mathf.Min(bottomMostPlane.position.y, topMostPlane.position.y), Mathf.Min(closePlane.position.z, farPlane.position.z)));
        PianoFinder.SetVector("SizeBound", new Vector3(Mathf.Abs(leftMostPlane.position.x - rightMostPlane.position.x), 
            Mathf.Abs(topMostPlane.position.y - bottomMostPlane.position.y), Mathf.Abs(farPlane.position.z - closePlane.position.z)));
        PianoFinder.SetVector("HertzLower", new Vector2(minFreq, maxFreq - minFreq));
    }
    public int FetchKernel()
    {
        switch (musicStyle)
        {
            case Style.Chords:
                return PianoFinder.FindKernel("PianoBird");
            case Style.ClosestFFT:
                return PianoFinder.FindKernel("BirdFFT");
            default:
                return -1;
        }
    }
    public float vol = 10f;
    public bool generatorFinished = true;
    public void Update()
    {
        if(BirdLogic.otherBirds.Count == 0)
        {
            return;
        }
        if (Time.unscaledTime - lastRefresh >= 1f / refreshRate && generatorFinished)
        {
            Vector4[] positions = new Vector4[BirdLogic.otherBirds.Count];
            
            for (int i = 0; i < BirdLogic.otherBirds.Count; i++)
            {
                positions[i] = BirdLogic.otherBirds[i].transform.position;
            }
            GPLocations.SetData(positions);

            int kern = FetchKernel();
            PianoFinder.SetInt("NumLocations", BirdLogic.otherBirds.Count);
            PianoFinder.SetBuffer(kern, "Locations", GPLocations);
            PianoFinder.SetInt("NumberOfBins", NumberOfTones);
            // Actually calculate it...
            PianoFinder.Dispatch(kern, NumberOfTones,1,1);
            // PERFORMANCE HIT HERE :(
            GPFreq.GetData(currentAmp);
            GPKeys.GetData(generationFreq);
            Debug.Log(currentAmp.Length + " " + generationFreq.Length);

            for(int i = 0; i < currentAmp.Length; i++)
            {
                Debug.Log(generationFreq[i]);
                AudioSource ss = audioGen.getAudioKey((int)generationFreq[i]);
                ss.volume = vol*currentAmp[i];
            }


            lastRefresh = Time.unscaledTime;
            counter++;
            
        }
    }
    float counter = 0;
   
    void OnApplicationQuit()
    {
        if (this.enabled)
        {
            //?!?1
            GPFreq.Dispose();
            GPLocations.Dispose();
            GPKeys.Dispose();
        }
    }

    long LP = 0;
    bool didJump = false;
    public void OnAudioRead(float[] data)
    {
       
        // INIT
        if(buffer1.Length == 0 || buffer2.Length == 0)
        {
            buffer1 = new float[data.Length];
            buffer2 = new float[data.Length];
            Debug.Log("INIT BUFFERS WITH " + data.Length);
        }

        bool mb = rbuffer;
        //Debug.Log(NumberToSmear[0] + " " + NumberToSmear[1]);
        for (int i = 0; i < data.Length; i++)
        {
            int bufferLen = (mb ? buffer1.Length : buffer2.Length);
            float id = (mb ? buffer1[i % bufferLen] : buffer2[i % bufferLen]);
            if (i < SmearAmount)
            {
                data[i] = vol*Mathf.Lerp(NumberToSmear[0] + NumberToSmear[1]*(i+1),id, Mathf.Clamp01(((float)i) / SmearAmount));
            }
            else
            {
                if(i % bufferLen < SmearAmount)
                {
                    // Smear the buffer values...
                    data[i] = vol*Mathf.Lerp((mb ? buffer1[buffer1.Length-1] + (buffer1[buffer1.Length -1] - buffer1[buffer1.Length-2])*((i%bufferLen)+1)/(SmearAmount/3f) : 
                        buffer2[buffer2.Length-1] + (buffer2[buffer2.Length - 1] - buffer2[buffer2.Length - 2])*((i%bufferLen)+1)/(SmearAmount/3f)), 
                       id, (Mathf.Clamp01(((float)i) / SmearAmount)));
                }
                else
                {
                    data[i] = vol*id;
                }
                // Lets assume that the buffers aren't generated correctly...
                // That is, they are too small, how to handle smearing?
            }
        }
        
        

        NumberToSmear[0] = data[data.Length - 1];
        NumberToSmear[1] = (data[data.Length - 1] - data[data.Length - 2])*3f/SmearAmount;
        
        //Debug.Log(NumberToSmear[0] + " " + NumberToSmear[1]);
        buffer = mb;
        SaveWaveAsWave(data, samplerate);
    }
    public static int SmearAmount = 20;
    bool rbuffer = false;
    bool buffer = false;
    float[] buffer1 = new float[0];
    float[] buffer2 = new float[0];

    float[] NumberToSmear = new float[2]; // The idea is that it stores a psuedo derivative and a y offset
    public static string PDP;
    public static int WFC = 0;
    void SaveWave(float[] dd)
    {
        System.IO.Directory.CreateDirectory(PDP +"/WAV");
        System.IO.StreamWriter f = new System.IO.StreamWriter(PDP + "/WAV/wav_" + WFC + ".txt");
        for(int i = 0; i < dd.Length; i++)
        {
            f.Write(dd[i] + " ");
        }
        f.Close();
        WFC++;
    }
    public void OnAudioSet(int np) {
        LP = positionit;
        didJump = true;
        Debug.Log(positionit + " " + np);
        //positionit = np; sdassss
    } // Ignore.

    public static void SaveWaveAsWave(float[] dd, long samplerate)
    {
        System.IO.Directory.CreateDirectory(PDP + "/WAV");
        using (System.IO.BinaryWriter w = new System.IO.BinaryWriter(System.IO.File.Open(PDP + "/WAV/wav_" + WFC + ".wav", System.IO.FileMode.Create)))
        {
            // UGH
            w.Write(new byte[] {0x52,0x49,0x46,0x46 });
            w.Write(ToProperByte(36+2*dd.Length));
            w.Write(new byte[] { 0x57, 0x41, 0x56, 0x45});
            w.Write(new byte[] { 0x66, 0x6d, 0x74, 0x20 });
            w.Write(ToProperByte(16));
            w.Write(new byte[] { 0x01, 0x00});
            w.Write(new byte[] {0x01,0x00 });
            w.Write(ToProperByte(samplerate));
            w.Write(ToProperByte(samplerate*2));
            w.Write(new byte[] { 0x02,0x00});
            w.Write(new byte[] {0x10,0x00 });
            w.Write(new byte[] {0x64,0x61,0x74,0x61 });
            w.Write(ToProperByte(dd.Length*2));
            for (int i = 0; i < dd.Length; i++){
                w.Write(TwosCompl(dd[i]));
            }
        }
        WFC++;
    }
    public static byte[] TwosCompl(float l)
    {
        // The idea is to transform l to a -1 <-> 1
        int ll = Mathf.Clamp((int)(l * 32767f), -32767, 32767);
        // Slightly interesting...
        if (ll < 0)
        {
            ll = 32768 + ll;

            return new byte[] { (byte)(ll % 256),(byte)(((ll-(ll%256)) / 256) + 0x80) };
        }
        else
        {
            return new byte[] { (byte)(ll % 256), (byte)(((ll - (ll % 256)) / 256)) };
        }
    }
    public static byte[] ToProperByte(long l)
    {
        byte a = (byte)(l % 256);
        l = (l - a) / 256;
        byte b = (byte)(l % 256);
        l = (l - b) / 256;
        byte c = (byte)(l % 256);
        l = (l - c) / 256;
        byte d = (byte)(l % 256);
        return new byte[] {a,b,c,d };
    }




}
/*
int copys = BirdLogic.otherBirds.Count;
Debug.Log("Human = " + System.String.Join("",
    new List<float>(data)
    .ConvertAll(i => i.ToString())
    .ToArray()));
// DON'T ZERO IT!!!!!
int NumberSmear = (int)Mathf.Max(10, Mathf.CeilToInt(samplerate / 1000));
SaveWave(data);
bool hasJumped = true;
didJump = false;
//float NumberToSmear = data[(int)Mathf.Clamp(LP - positionit,0, data.Length-1)]; // Last datapoint...

for (int i = 0; i < data.Length; i++)
{
    data[i] = 0;
}

// Smooth the first 100 or so samples to try to remove clicking....
// This idea is that we have key frequencies picked out we then can generate these tones here based on their amplitude.
for (int i = 0; i < NumberOfTones; i++)
{
    for (int j = 0; j < data.Length; j++)
    {
        data[j] += (hasJumped ? ((1f - Mathf.Clamp01(((float)j) / NumberSmear)) * NumberToSmear / ((float)NumberOfTones)) + Mathf.Clamp01(((float)j) / NumberSmear) : 1) * vol * (previousAmp[i] + ((currentAmp[i] - previousAmp[i]) * (((float)j) / data.Length))) * Mathf.Sin((generationFreq[i] / samplerate) * (positionit + j)) / NumberOfTones;
    }
    previousAmp[i] = currentAmp[i];

}
NumberToSmear = data[data.Length - 1];
SaveWave(data);
positionit += data.Length;
Debug.Log("Generating sound...");
*/
