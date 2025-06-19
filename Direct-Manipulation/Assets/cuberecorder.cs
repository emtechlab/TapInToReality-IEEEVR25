using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.IO;
using System;
using UnityEngine.Serialization;
using System.Globalization;
/*using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.IO;
using UnityEngine.Serialization;
using System;
using System.Globalization;
using System.Diagnostics;

//using Microsoft.MixedReality.Toolkit;
//using System.Diagnostics;
//using System.ComponentModel;
//using System.Globalization;
//using System.Linq;*/

public class cuberecorder : MonoBehaviour
{
  
    public GameObject cube;
    private Material cubeMaterial;
    private Vector3 initialPosition;
    private Color originalColor;

    private Color greenColor = Color.green;
    private Color whiteColor = Color.white;
    public Color redTimeoutColor = Color.red;

    public float colorChangeInterval = 15f;
    public float interactionTimeout = 10f; // Interaction timeout in seconds
    public float redTimeoutDuration = 3f; // Timeout for changing color to red after no interaction in seconds

    private int boxMoveCount = 0; // Count of box movements
    private int colorChangeToGreenCount = 0; // Count of color changes to green
    private int colorChangeToWhiteCount = 0; // Count of color changes to white
    private int colorChangeToRedCount = 0; // Count of color changes to red

    private bool boxMoved = false; // Flag to track box movement
    private bool interactionRecorded = false; // Flag to track if interaction is recorded

    private bool waitingForInteraction = false; // Flag to track if waiting for interaction timeout
    private float greenColorStartTime; // Timestamp when the box turned green

    public string fileName;
    private string dataFolderPath;
    private StreamWriter trailStreamWriter;

    private Coroutine colorChangeCoroutine; // Coroutine for changing box color
    private Coroutine interactionCoroutine;
    private bool interactionStarted = false;
    private int interactionCount = 0;
    private int colorChangeCount = 0;
    private int greenColorChangeCount = 0;
    private int whiteColorChangeCount = 0;


    private float TimeSpan;

    private string appstartTime;

    private Vector3 lastRecordedPosition;

    private float interactionThreshold = 0.1f; // Adjust this value as needed
    private float lastBoxMovementTime = 0.0f;

    private int colorChangeCounter = 0; // Counter for color changes

    private void Start()
    {
        appstartTime = DateTime.Now.ToString("mm:ss.fff");
        UnityEngine.Debug.Log("App started at" + appstartTime);
        // Check if the GameObject is assigned in the Unity Editor
        if (cube != null)
        {
            // Access the GameObject and do something with it
            cube.GetComponent<Renderer>().material.color = Color.red;
        }
        else
        {
            UnityEngine.Debug.Log("GameObject 'cube' is not assigned in the Unity Editor.");
        }

        cubeMaterial = cube.GetComponent<Renderer>().material;
        originalColor = cubeMaterial.color;

        // Store initial position of the cube
        initialPosition = cube.transform.position;

        //StartColorChangeTimer();

        // Create file at the start of the game
        dataFolderPath = UnityEngine.Application.persistentDataPath;

        // Create file name based on start time
        string timestamp = System.DateTime.Now.ToString("MMdd_HHmmss");
        fileName = dataFolderPath + "/" + "box_d_" + timestamp + ".txt";

        // Debug logging to check dataFolderPath and fileName
        UnityEngine.Debug.Log("Data folder path: " + dataFolderPath);
        UnityEngine.Debug.Log("File name: " + fileName);

        // Write initial information to the file
        FileStream mystream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);
        //System.IO.File.WriteAllText(fileName, data);
        trailStreamWriter = new StreamWriter(mystream);

        string data = "Game started at: " + GetCurrentTime();
        WriteToFile(data);

        // Start the color change routine
        InvokeRepeating("ChangeColor", colorChangeInterval, colorChangeInterval);
    }

    private void Update()
    {
        // Check distance exceeds for box movement If the  the threshold, consider it as an interaction
        if (cube.transform.position != initialPosition)
        {
            float timenow = GetCurrentTime();
            if (IsInteracting())
            {
                UnityEngine.Debug.Log("inside the IsInteracting");
                if (waitingForInteraction)
                {
                    UnityEngine.Debug.Log("inside waitingforInteraction");
                    // Record interaction and box movement
                    RecordInteractionAndMovement();
                    cubeMaterial.color = whiteColor;
                    colorChangeToWhiteCount++; // Increment white color count
                    initialPosition = cube.transform.position;
                    WriteToFile("Color changed to white at: " + timenow);
                }
            }

        }

    }

    // Coroutine to change color after a delay
    private IEnumerator ChangeColorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Start the color change routine
        InvokeRepeating("ChangeColor", colorChangeInterval, colorChangeInterval);
    }

    // Function to record interaction and movement
    private void RecordInteractionAndMovement()
    {
        // Record interaction start time
        RecordInteractionStart();

        // Record box movement
        RecordBoxMovement();

        // Reset boxMoved flag and set interactionRecorded flag
        //boxMoved = false;
        interactionRecorded = true;
    }

    // Function to record box movement
    private void RecordBoxMovement()
    {
        // Record box move time
        string data = "Box moved at: " + GetCurrentTime();
        WriteToFile(data);

        // Increment box move count
        boxMoveCount++;
    }

    // Function to record interaction start time
    private void RecordInteractionStart()
    {
        // Record interaction start tim
        string data = "Interaction started at: " + GetCurrentTime();
        WriteToFile(data);

        // Increment interaction count
        interactionCount++;
        waitingForInteraction = false;
        //interactionRecorded = false;
    }

    private bool IsInteracting()
    {
        // Calculate the distance between the current and last recorded position of the cube
        float distance = Vector3.Distance(cube.transform.position, initialPosition);

        // If the distance exceeds the threshold, consider it as an interaction
        if (distance > interactionThreshold)
        {
            //boxMoved = true;
            lastBoxMovementTime = GetCurrentTime();
            string data = "Change in Distance is:" + distance + " at " + lastBoxMovementTime;
            // Update the last recorded position
            initialPosition = cube.transform.position;
            WriteToFile(data);

            return true;
        }

        return false;
    }

    private void ChangeColor()
    {
        // Change the color of the box to green to signal the user to move the box
        cube.GetComponent<Renderer>().material.color = greenColor;
        // Record the green color change start time
        greenColorStartTime = GetCurrentTime();

        // Record the green color change
        string data = "Color changed to green at: " + greenColorStartTime;
        WriteToFile(data);

        // Increment green color change count
        colorChangeToGreenCount++;

        // Start interaction timeout
        waitingForInteraction = true;
    }

    private void OnDestroy()
    {
        // Record game end time and interaction count

        string data = "Game ended at: " + GetCurrentTime();
        data += "Total interactions: " + interactionCount + "\n";
        data += "Total red color changes: " + colorChangeToRedCount + "\n";
        data += "Total green color changes: " + colorChangeToGreenCount + "\n";
        data += "Total white color changes: " + colorChangeToWhiteCount + "\n";
        data += "Total box movements: " + boxMoveCount + "\n";
        WriteToFile(data);

        // Close the stream writer
        trailStreamWriter.Close();
    }


    private void WriteToFile(string data)
    {
        if (trailStreamWriter != null)
        {
            try
            {
                trailStreamWriter.WriteLine(data);
                trailStreamWriter.Flush();
                UnityEngine.Debug.Log(data);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error writing to file: " + e.Message);
            }
        }
        else
        {
            UnityEngine.Debug.LogError("StreamWriter is null.");
        }
    }

    private float GetCurrentTime()
    {
        return CalculateTimeDifference(appstartTime, DateTime.Now.ToString("mm:ss.fff"));
    }


    public static float CalculateTimeDifference(string timestamp1Str, string timestamp2Str)
    {

        DateTime timestamp1 = DateTime.ParseExact(timestamp1Str, "mm:ss.fff", CultureInfo.InvariantCulture);
        DateTime timestamp2 = DateTime.ParseExact(timestamp2Str, "mm:ss.fff", CultureInfo.InvariantCulture);

        TimeSpan timeDifference = timestamp2 - timestamp1;
        float timeDifferenceInSeconds = (float)timeDifference.TotalMilliseconds / 1000.0f;

        return timeDifferenceInSeconds;
    }
}

