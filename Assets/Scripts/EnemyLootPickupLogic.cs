using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLootPickupLogic : MonoBehaviour
{
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioClip healthPackClip;
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            sfxSource.PlayOneShot(healthPackClip);
            PlayerHealthLogic playerHealth = other.GetComponent<PlayerHealthLogic>();
            if (playerHealth != null)
            {
                playerHealth.AddHealthPoints(1);
                Destroy(gameObject); 
            }
        }
    }
}
