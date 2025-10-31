using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class handles the dealing of damage to health components.
/// </summary>
public class ExitFromLevel : MonoBehaviour
{
    [Tooltip("The team of the player")]
    public int teamId = 0;

    /// <summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        ExitCollision(collision.gameObject);
    }

    /// <summary>
    private void ExitCollision(GameObject collisionGameObject)
    {
        Health collidedHealth = collisionGameObject.GetComponent<Health>();
        if (collidedHealth != null)
        {
            if (collidedHealth.teamId == this.teamId)
            {
                ExitReached();
            }
        }
    }

    /// <summary>
    private void ExitReached()
    {
        if (GameManager.instance != null && !GameManager.instance.gameIsOver)
        {
            GameManager.instance.ExitReached();
        }
    }
}