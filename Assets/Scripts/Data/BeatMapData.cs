using System.Collections.Generic;
using UnityEngine;





[System.Serializable]
public class BeatMapData
{
    [Tooltip("Nom de la map (affiché dans le panneau de sélection)")]
    public string mapName;

    [Tooltip("Nom de la chanson (extrait du MusicDatabase)")]
    public string songName;

    [Tooltip("Chemin de l'event FMOD (ex: event:/Music/NeonRush)")]
    public string fmodEventPath;

    [Tooltip("Beats par minute de la chanson")]
    public float bpm;

    [Tooltip("Offset de démarrage en secondes (pour sync)")]
    public float songOffset;

    [Tooltip("Liste de toutes les notes/events de la beatmap")]
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
        BeatMapData map = JsonUtility.FromJson<BeatMapData>(json);
        
        // Migration automatique des anciennes maps qui n'avaient pas de mapName
        if (map != null && string.IsNullOrEmpty(map.mapName) && !string.IsNullOrEmpty(map.songName))
        {
            map.mapName = map.songName;
            map.songName = ""; // On pourra le recréer proprement via FMOD s'il le faut
        }

        return map;
    }
}
