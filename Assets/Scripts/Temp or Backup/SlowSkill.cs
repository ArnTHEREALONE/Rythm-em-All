using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class PlayerSlowPulse : MonoBehaviour
{
    [Header("Taille de la zone")]
    public float radius;

    [Header("Couleur de la zone")]
    public Color zoneColor;

    [Header("Cooldown en seconde")]
    public float cooldown;

    [Header("Tags à détecter")]
    public List<string> validTags;

    List<Slow> inside = new List<Slow>();

    private float timer;

    void Start()
    {
        GetComponent<SphereCollider>().radius = radius;
        timer = cooldown;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (Input.GetButtonDown("Fire1") && timer >= cooldown)
        {
            UseSlow();
            timer = 0f;
        }
    }

    void UseSlow()
    {
        List<Slow> targets = new List<Slow>(inside);

        foreach (Slow toSlow in targets)
        {
            if (toSlow != null)
            {
                toSlow.ApplySlow();
                Debug.Log("Slowed " + toSlow.name);
            }
        }
    }

    void OnTriggerEnter(Collider col)
    {
        if (!validTags.Contains(col.tag)) return;

        Slow toSlow = col.GetComponent<Slow>();

        if (toSlow && !inside.Contains(toSlow))
            inside.Add(toSlow);
    }

    void OnTriggerExit(Collider col)
    {
        Slow toSlow = col.GetComponent<Slow>();

        if (toSlow)
            inside.Remove(toSlow);
    }

    void OnDrawGizmosSelected()
    {
        Handles.color = zoneColor;
        Handles.DrawWireDisc(transform.position, Vector3.up, radius);
    }
}
