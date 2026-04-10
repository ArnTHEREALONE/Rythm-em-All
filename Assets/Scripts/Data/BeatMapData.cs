using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BeatMapData
{
    public string songName;

    public string musicFileName;

    public float bpm;

    public float songOffset;

    public List<BeatNote> notes = new List<BeatNote>();

    public float SecondsPerBeat => 60f / bpm;

    public float BeatToSeconds(float beat)
    {
        return songOffset + beat * SecondsPerBeat;
    }

    public float SecondsToBeat(float seconds)
    {
        return (seconds - songOffset) / SecondsPerBeat;
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }

    public static BeatMapData FromJson(string json)
    {
        return JsonUtility.FromJson<BeatMapData>(json);
    }
}
