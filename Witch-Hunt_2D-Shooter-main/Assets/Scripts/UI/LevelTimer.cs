using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class LevelTimer : UIelement
{
    public float timeRemaining = 60.0f;
    public bool timerIsRunning = false;
    public Text displayText = null;
    private void Start()
    {
        // Starts the timer automatically
        timerIsRunning = true;
    }
    void Update()
    {
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                if (displayText != null)
                {
                    string output = string.Format("{0:f0}", timeRemaining);
                    displayText.text = "Time left: " + output;
                }
            }
            else
            {
                if (displayText != null)
                {
                    displayText.text = "The passage is open.";
                }
                timeRemaining = 0;
                timerIsRunning = false;
            }
        }
    }
}