using UnityEngine;

public enum EnemyInputType
{
    Any,        
    LeftOnly,   
    RightOnly,  
    Both,       
    Spam        
}

[System.Serializable]
public class BeatNote
{
    public float beatTime;

    public int spawnerIndex;

    public EnemyInputType inputType;

    public BeatNote() { }

    public BeatNote(float beatTime, int spawnerIndex, EnemyInputType inputType)
    {
        this.beatTime = beatTime;
        this.spawnerIndex = spawnerIndex;
        this.inputType = inputType;
    }
}
