using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public GameObject player = null;

    public Image[] lives;

    public Sprite fulllife;
    public Sprite emptylife;

    private void Start()
    {

    }

    private void Update()
    {
        if (player != null)
        {
            Health playerHealth = player.GetComponent<Health>();
            for (int i = 0; i < lives.Length; i++)
            {
                if (i < playerHealth.currentHealth)
                {
                    lives[i].sprite = fulllife;
                }
                else
                {
                    lives[i].sprite = emptylife;
                }

                if (i < playerHealth.maximumHealth)
                {
                    lives[i].enabled = true;
                }
                else
                {
                    lives[i].enabled = false;
                }
            }
        }
    }

}
