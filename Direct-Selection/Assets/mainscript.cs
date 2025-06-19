using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.IO;
using UnityEngine.Serialization;
using System.Security.Permissions;
using static System.Net.Mime.MediaTypeNames;
using System;

using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.Linq;


using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.Subsystems;
using UnityEngine.XR.Interaction.Toolkit;
using MixedReality.Toolkit.Input;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using System.Collections.Specialized;

public class mainscript : MonoBehaviour
{

    public GameObject mainparent;
    public GameObject[] spheres; // Assign 4 spheres in the order: Red, Green, Blue, Brown
    public GameObject[] cubes; // Assign cubes ordered to match: Red, Green, Blue, Brown
    
    public GameObject greenSphere;
    public GameObject blueSphere;
    public GameObject redSphere;
    public GameObject brownSphere;

    public GameObject greenButton;
    public GameObject redButton;
    public GameObject blueButton;
    public GameObject brownButton;

    public string fileName;
    private string dataFolderPath;
    private StreamWriter trailStreamWriter;

    private string appstartTime;

    public float toggleInterval = 2.0f; // Interval in seconds for toggling visibility
    private float timer;

    public Material highlightMaterial;
    public Material defaultMaterial;

    private int currentHighlightIndex = 0;
    private float sphereAppearTime;

    private string highlightedCubeColor;
    private string movedSphereName;
    private float touchTime;

   
    public Renderer targetRenderer;
    public string materialName;
    public int currentMaterialIndex = 0;

    private Vector3[] initialPositions; // Array to store the initial positions of cubes
    private Vector3[] lastRecordedPosition;
    private float[] distance; 
    private float distanceThreshold = 0.1f; // Threshold for significant movement
    private float lastspheremovedIndex;
    private bool interactionStarted = false;
    private bool waitingForInteraction = false; // Flag to track if waiting for interaction timeout
    private int interactionCount = 0;

    private int[] cubeHighlightOrder = { 3, 3, 2, 0, 2, 2, 0, 1 }; // Brown, Brown, Blue, Red, Blue, Blue, Red, Green (indices)

    public bool buttonindexrecorded = false;
    public string lastprintindex = "";
    public string lastprintbutton = "";
    // Sequence of color indices corresponding to spheres (Red = 0, Green = 1, Blue = 2, Brown = 3)
    private int[] sphereColorSequence = new int[]
    {
        0, 1, 2, 3, 0, 1, 3, 2, 0, 2, 1, 3, 0, 2, 3, 1, 0, 3, 1, 2,
        0, 3, 2, 1, 1, 0, 2, 3, 1, 0, 3, 2, 1, 2, 0, 3, 1, 2, 3, 0,
        1, 3, 0, 2, 1, 3, 2, 0, 2, 0, 1, 3, 2, 0, 3, 1, 2, 1, 0, 3,
        2, 1, 3, 0, 2, 3, 0, 1, 2, 3, 1, 0, 3, 0, 1, 2, 3, 0, 2, 1
    };

    private int currentSphereIndex = 0;

    public void Start()
    {
        
        appstartTime = DateTime.Now.ToString("mm:ss.fff");
        UnityEngine.Debug.Log("App started at" + appstartTime);
        dataFolderPath = UnityEngine.Application.persistentDataPath;
        string timestamp = System.DateTime.Now.ToString("MMdd_HHmmss");
        fileName = dataFolderPath + "/" + "bubble_d_" + timestamp + ".txt";
        UnityEngine.Debug.Log("Data folder path: " + dataFolderPath);
        UnityEngine.Debug.Log("File name: " + fileName);

        // Write initial information to the file
        FileStream mystream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);
        trailStreamWriter = new StreamWriter(mystream);

        string data = "Game started at: " + GetCurrentTime();
        WriteToFile(data);

        GameObject mainparent = GameObject.Find("Game");
        spheres = new GameObject[4]; // Re-initialize to be sure
        cubes = new GameObject[4]; // Re-initialize to be sure
        // Initialize the array to the same length as the cubes array
        initialPositions = new Vector3[spheres.Length];
        lastRecordedPosition = new Vector3[spheres.Length];
        distance = new float[spheres.Length];
        //WriteToFile("Initializing spheres array. Length = " + spheres.Length);

        //WriteToFile("Initializing cubes array. Length = " + cubes.Length);

        if (mainparent != null)
        {
                // Assign spheres safely
                string[] sphereNames = { "Game/redbubble", "Game/greenbubble", "Game/bluebubble", "Game/brownbubble" };
                for (int i = 0; i < spheres.Length; i++)
                {
                    //GetChildWithName(mainparent, "greenbubble");
                    spheres[i] = GameObject.Find(sphereNames[i]);
                    if (spheres[i] != null)
                    {
                        //WriteToFile("Found sphere: " + sphereNames[i]);
                        initialPositions[i] = spheres[i].transform.position;
                        lastRecordedPosition[i] = initialPositions[i];
                        distance[i] = Vector3.Distance(lastRecordedPosition[i], initialPositions[i]);
                    //spheres[i].SetActive(false);
                    //spheres[i].GetComponent<Renderer>().material.color = Color.white;
                    //WriteToFile("initial position of sphere " + sphereNames[i] +":" + initialPositions[i]);
                    }
                    else
                    {
                        WriteToFile("Failed to find sphere: " + sphereNames[i]);
                        //WriteToFile("Sphere at index " + i + " is not assigned.");
                    }
                }
                // Assign cubes safely
                string[] cubeNames = { "Game/ColorButtons/redbutton", "Game/ColorButtons/greenbutton", "Game/ColorButtons/bluebutton", "Game/ColorButtons/brownbutton" };
                for (int i = 0; i < cubes.Length; i++)
                {
                    cubes[i] = GameObject.Find(cubeNames[i]);
                    if (cubes[i] == null)
                    {
                        WriteToFile("Failed to find cube: " + cubeNames[i]);
                    }
                    else
                    {
                        //WriteToFile("Found cube: " + cubeNames[i]);
                    }
                }

                CheckAssignments(spheres, "spheres");
                CheckAssignments(cubes, "cubes");
                
                //AllSpheresOff();
        }
            
        timer = toggleInterval;
        // Start the color change routine
        InvokeRepeating("ChangeColor", 3, 3);
    }

    void Update()
    {

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            //ToggleVisibility(spheres[0]);
            timer = toggleInterval; // Reset the timer
        }
        CheckCubeMovements();
    }

    // Call this method to move to the next sphere and cube setup
    public void NextSetup()
    {
        UpdateSphereAndCube();
    }


    // Function to check the movement of each cube from its initial position
    private void CheckCubeMovements()
    {
        for (int i = 0; i < spheres.Length; i++)
        { 
            if (spheres[i] != null)
            {
                string data = "";
                lastRecordedPosition[i] = spheres[i].transform.position;
                distance[i] = Vector3.Distance(lastRecordedPosition[i], initialPositions[i]);
                if (lastRecordedPosition[i] != initialPositions[i])
                {
                    if (distance[i] > distanceThreshold)
                    {
                        if (waitingForInteraction)
                        {
                            data = "At: " + GetCurrentTime();
                            data += spheres[i];
                            //data += " moved to " + lastRecordedPosition[i];
                            data += " moved and covered distance = " + distance[i];
                            WriteToFile(data);
                            //WriteToFile("Sphere " + spheres[i].name + " has moved significantly from its original position.");
                            RecordInteractionStart();
                        }
                    }
                    
                }
            }
        }
    }

    // Function to record interaction start time
    private void RecordInteractionStart()
    {
        // Increment interaction count
        interactionCount++;
        waitingForInteraction = false;
    }

    private void ChangeColor()
    {
        for (int i = 0; i < spheres.Length; i++)
        {
            if (spheres[i] != null)
            {
                spheres[i].transform.position = initialPositions[i];
            }
        }
                waitingForInteraction = true;
        //buttonindexrecorded = false
        UpdateSphereAndCube();
    }

    public void UpdateSphereAndCube()
    {
        if (currentSphereIndex >= sphereColorSequence.Length) currentSphereIndex = 0;

        // Enable only the sphere corresponding to the current index
        int activeSphereIndex = sphereColorSequence[currentSphereIndex];
        for (int i = 0; i < spheres.Length; i++)
        {
            if(i == activeSphereIndex)
            {
                WriteToFile(spheres[i].name + " appeared at " + GetCurrentTime());
            }
            spheres[i].SetActive(i == activeSphereIndex);
        }

        // Update the cube highlight, changing every four spheres
        int cubeIndex = (currentSphereIndex / 3) % cubes.Length;
        if (lastprintindex != "\"Sphere index: \" + activeSphereIndex + \" Cube index: \" + cubeIndex")
        {
            WriteToFile("Sphere index: " + activeSphereIndex + " Cube index: " + cubeIndex);
            lastprintindex = "Sphere index: " + activeSphereIndex + " Cube index: " + cubeIndex;
        }
        for(int j = 0; j < cubes.Length; j++)
        {
            if (j == cubeIndex)
            {
                if (waitingForInteraction)
                {
                    if (lastprintbutton != cubes[j].name + " appeared at ")
                    {
                        WriteToFile(cubes[j].name + " appeared at " + GetCurrentTime());
                        lastprintbutton = cubes[j].name + " appeared at ";
                    }
                   
                cubes[j].SetActive(true);
                }
            }
            else
            {
                // Getting the current color of the material
                cubes[j].SetActive(false);
            }   
            //cube.GetComponent<Renderer>().material.color = Color.black; // Default to unhighlighted
        }
        
        //cubes[cubeIndex].GetComponent<Renderer>().material.color = spheres[activeSphereIndex].GetComponent<Renderer>().material.color; // Highlight the cube

        currentSphereIndex++;
    }



    private void ToggleVisibility(GameObject sphere)
    {
        
        AllSpheresOff();
        /*
         * // Toggle the Renderer.enabled state for all children
        foreach (Transform child in transform)
        {
            Renderer childRenderer = child.GetComponent<Renderer>();
            if (childRenderer != null)
            {
                childRenderer.enabled = !childRenderer.enabled;
            }
        }
        */
        // Activate the selected sphere
        sphere.SetActive(true);
    }

    // Helper method to check if all elements in an array are assigned
    private void CheckAssignments(GameObject[] array, string arrayName)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == null)
            {
                WriteToFile("Assignment missing in " + arrayName + " at index: " + i);
            }
            else
            {
                //WriteToFile(arrayName + " at index " + i + " is assigned to " + array[i].name);
            }
        }
    }

    private void OnDestroy()
    {
        // Record game end time and interaction count

        string data = "Game ended at: " + GetCurrentTime();
        data += "Total interactions: " + interactionCount + "\n";
        WriteToFile(data);
        // Close the stream writer
        trailStreamWriter.Close();
    }

   
    // Function to turn off all spheres
    private void AllSpheresOff()
    {
        foreach (GameObject sphere in spheres)
        {
            if (sphere != null) // Check if the sphere reference is not null
                sphere.SetActive(false);
            else
                WriteToFile("One of the sphere objects is not assigned in the array.");
        }
    }

    // Function to turn off all buttons
    private void AllButtonsOff()
    {
        foreach (GameObject button in cubes)
        {
            if (button != null) // Check if the button reference is not null
                button.SetActive(false);
            else
                WriteToFile("One of the button objects is not assigned in the array.");
        }
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

    private GameObject GetChildWithName(GameObject parent, string name)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child.gameObject.name == name)
            {
                return child.gameObject;
            }
        }
        return null;
    }
}

