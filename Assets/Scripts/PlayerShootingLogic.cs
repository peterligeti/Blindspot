using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private Shooter shooter;   // Reference to the generic shooter
    [SerializeField] private Transform firePoint;

    void Update()
    {
        // Unity’s default "Fire1" input (Left Ctrl / Left Mouse)
        if (Input.GetButton("Fire1"))
        {
            shooter.Shoot(firePoint.right);
        }

        // Support your custom analog trigger button but cam rotation does not seem to work
        if (InputUtility.RTriggerPulled)
        {
            shooter.Shoot(firePoint.right);
        }
    }
}