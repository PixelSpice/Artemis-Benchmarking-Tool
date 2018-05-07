using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;
//using UnityEngine.AI;
using System;
using System.IO;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

[InitializeOnLoad]
public class ArtemisWindow : EditorWindow
{
    static ArtemisWindow()
    {
        ShowWindow();
    }

    static void Update()
    {

    }

    [Serializable]
    public struct PreLoadedResults
    {
        public int BenchmarkType;
        public string Scene;
        public string Title;
        public float Duration;
        public bool CacheCleared;
        public string TypeFlag; // White Other, Orange AMD, Blue Intel, Green current results
    }


    public Color AMDColor = new Color(.82f, .47f, 0f, 1f);
    public Color IntelColor = new Color(0f, .47f, .7f, 1f);
    public Color OtherColor = new Color(.8f, .8f, .8f, 1f);
    public Color CurrentColor = new Color(.1f, .8f, .1f, 1f);

    public string[] BenchmarkType = new string[] { "LightMapping Benchmark", "NavMesh Baking Benchmark", "Build Time Benchmark" };
    public int BMType;

    public Texture2D PSLogo;
    public Texture2D ArtemisLogo;
    public Texture2D ShapeSceneTexture;
    public Texture2D TanksSceneTexture;
    public Texture2D HallSceneTexture;

    public bool ClearCache = false;
    public bool AverageResults = false;
    private bool HasBaked = false;

    //Current-benchmark run data
    public string Last_SceneName = "";
    public int Last_BMType = -1;
    public bool Last_Clear = false;
    public float[] results; //Holds old results for averaging.
    public float MaxResult;
    public float MinResult;

    //Loaded Results from file and final results for display.
    //Final Results is re-created every run, while loaded results is the import/output array.
    public PreLoadedResults[] LoadedResults;
    public PreLoadedResults[] FinalResults;

    [MenuItem("Window/Artemis Benchmarking Tool")]
    public static void ShowWindow()
    {
        GetWindow(typeof(ArtemisWindow),false, "PS - Artemis");
    }

    void Awake()
    {
            LoadedResults = new PreLoadedResults[0];
            //You can use the format below (and change the array size above) to load results from multiple sources at a time if you've been tracking them manually for some reason.
            //Don't do that though, just use the import/export tool.

            //LoadedResults[0].BenchmarkType = 0;
            //LoadedResults[0].Scene = "Shapes";
            //LoadedResults[0].Title = "AMD 1";
            //LoadedResults[0].Duration = 200f;
            //LoadedResults[0].CacheCleared = true;
            //LoadedResults[0].TypeFlag = "AMD";

            //LoadedResults[1].BenchmarkType = 0;
            //LoadedResults[1].Scene = "Shapes";
            //LoadedResults[1].Title = "Intel 1";
            //LoadedResults[1].Duration = 15f;
            //LoadedResults[1].CacheCleared = true;
            //LoadedResults[0].TypeFlag = "Intel";

            //LoadedResults[2].BenchmarkType = 0;
            //LoadedResults[2].Scene = "Shapes";
            //LoadedResults[2].Title = "Other 1";
            //LoadedResults[2].Duration = 75f;
            //LoadedResults[2].CacheCleared = true;
            //LoadedResults[0].TypeFlag = "Other";

        FinalResults = new PreLoadedResults[0];
        results = new float[1];
    }

    void OnGUI ()
    {
        if (!PSLogo)
            PSLogo = Resources.Load("Images/PSLogo") as Texture2D;
        if (!ArtemisLogo)
            ArtemisLogo = Resources.Load("Images/ArtemisLogo") as Texture2D;
        if (!ShapeSceneTexture)
            ShapeSceneTexture = Resources.Load("Images/ShapesScene") as Texture2D;
        if (!TanksSceneTexture)
            TanksSceneTexture = Resources.Load("Images/TanksScene") as Texture2D;
        if (!HallSceneTexture)
            HallSceneTexture = Resources.Load("Images/CorridorScene") as Texture2D;

        float ButtonWidth = Screen.width * .3f;
        float LogoWidth = ButtonWidth;
        if (ButtonWidth > 1024) ButtonWidth = 1024;
        if (LogoWidth < 150) LogoWidth = 150;
        if (LogoWidth > 450) LogoWidth = 450;
        //Logo & Hardware Area
        //##########################################################################################################
        GUILayout.Space(16);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        //ArtemisLogo
        GUILayout.Label(ArtemisLogo, GUILayout.Width(LogoWidth), GUILayout.Height(LogoWidth*.57f));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.BeginVertical("box");
        GUILayout.Label("Hardware Info", EditorStyles.boldLabel);
        GUILayout.Label("" + SystemInfo.processorType.ToString() + " @ " + SystemInfo.processorFrequency / 1000f + "Ghz", EditorStyles.miniLabel);
        GUILayout.Label("Threads:" + SystemInfo.processorCount + "     RAM: " + Mathf.Round(SystemInfo.systemMemorySize / 1000f) + " GB", EditorStyles.miniLabel);
        GUILayout.EndVertical();
        GUILayout.Space(8f);

        //Scene Selector Area
        //##########################################################################################################
        GUILayout.Label("1. Select Scene", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal("box");
        if(GUILayout.Button(ShapeSceneTexture, GUILayout.Width(ButtonWidth), GUILayout.Height(ButtonWidth * .625f)))
        {
            EditorSceneManager.OpenScene("Assets/Artemis Benchmarking Tool/Resources/Scenes/Shapes.unity");
        }
        if (GUILayout.Button(TanksSceneTexture, GUILayout.Width(ButtonWidth), GUILayout.Height(ButtonWidth * .625f)))
        {
            EditorSceneManager.OpenScene("Assets/Artemis Benchmarking Tool/Resources/Scenes/Tanks.unity");
        }
        if (GUILayout.Button(HallSceneTexture, GUILayout.Width(ButtonWidth), GUILayout.Height(ButtonWidth * .625f)))
        {
            EditorSceneManager.OpenScene("Assets/Artemis Benchmarking Tool/Resources/Scenes/CorridorDemo.unity");
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);

        //Options Area
        //##########################################################################################################

        //Dropdown with Benchmark Type Selection
        GUILayout.Label("2. Select Options", EditorStyles.boldLabel);
        BMType = EditorGUILayout.Popup(BMType, BenchmarkType);

        switch (BMType)
        {
            case 0: //Lightmapping
                LightMapOptions();
                break;
            case 1: //Navmesh
                NavmeshOptions();
                break;
            case 2: //Building
                BuildingOptions();
                break;
            default:
                Debug.Log("What did you even do to get this error message?");
                return;
        }
        GUILayout.Space(8f);

        //Run Buttons Area
        //##########################################################################################################
        GUILayout.Label("3. Run Benchmark", EditorStyles.boldLabel);
        if (GUILayout.Button("Run Benchmark"))//, GUILayout.Width(ButtonWidth), GUILayout.Height(ButtonWidth * .125f)))
        {
            //Resets and disallows AverageResults if needed.
            //Keeps somebody from inadvertantly averaging mutliple mis-matched data types.
            if (Last_SceneName != EditorSceneManager.GetActiveScene().name || Last_BMType != BMType || Last_Clear != ClearCache)
            {
                Last_SceneName = EditorSceneManager.GetActiveScene().name;
                Last_BMType = BMType;
                HasBaked = false;
                results[0] = 0;
            }
            
            double LastResult = results[0];
            if (LastResult == 0)
            {
                AverageResults = false;
            }

            switch (BMType)
            {
                case 0: //Lightmapping
                    LightingOptionHandler();
                    LightmapBenchmark();
                    break;
                case 1: //Navmesh
                    NavmeshOptionHandler();
                    NavmeshBenchmark();
                    break;
                case 2: //Building
                    BuildingOptionHandler();
                    BuildingBenchmark();
                    break;
                default:
                    Debug.Log("What did you even do to get this error message?");
                    return;
            }
        }
        


        //Results Area
        //##########################################################################################################
        if (MaxResult == 0)
        {
            UpdateMinMax();
        }

        if (results[0] != 0)
        {
            switch (Last_BMType)
            {
                case 2: //Building
                    GUILayout.Label("Build Time Benchmark:", EditorStyles.miniLabel);
                    break;
                case 1: //Navmesh
                    GUILayout.Label("Navmesh Bake: " + Last_SceneName, EditorStyles.miniLabel);
                    break;
                case 0: //Lightmapping
                    GUILayout.Label("Lightmapping Bake: " + Last_SceneName, EditorStyles.miniLabel);
                    break;
                case -1:
                    break;
                default:
                    break;
            }
        }

        Rect rect = EditorGUILayout.BeginVertical();
        float rectHeight = rect.height / FinalResults.Length;
        for (int i = 0; i < FinalResults.Length; i++)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rectHeight * i, rect.width, rectHeight), Color.gray);
            switch (FinalResults[i].TypeFlag)
            {
                case ("AMD"):
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y + rectHeight * i, rect.width * (FinalResults[i].Duration / MaxResult), rectHeight), AMDColor);
                    break;
                case ("Intel"):
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y + rectHeight * i, rect.width * (FinalResults[i].Duration / MaxResult), rectHeight), IntelColor);
                    break;
                case ("Current"):
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y + rectHeight * i, rect.width * (FinalResults[i].Duration / MaxResult), rectHeight), CurrentColor);
                    break;
                default:
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y + rectHeight * i, rect.width * (FinalResults[i].Duration / MaxResult), rectHeight), OtherColor);
                    break;
            }
            GUILayout.Label(FinalResults[i].Title +": " + FinalResults[i].Duration + " seconds");
        }
        GUILayout.EndVertical();
        if (results[0] != 0)
            GUILayout.Label("Note: Lower is better.", EditorStyles.miniLabel);

        GUILayout.Space(8f);


        //Import/Export Area
        //##########################################################################################################
        GUILayout.Label("Optional - Import/Export result set.", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Import"))
        {
            ImportFromFile();
        }
        if (GUILayout.Button("Export"))
        {
            ExportToFile();
        }
        GUILayout.EndHorizontal();
        GUILayout.Label("Note: Use this to save and load result from benchmarks,\n to caompare results more easily from different hardware.", EditorStyles.miniLabel);

        //PixelSpice Logo
        //##########################################################################################################
        GUILayout.Space(8);
        GUILayout.Label(PSLogo, GUILayout.Width(150), GUILayout.Height(150 * .57f));
    }

    public void LightMapOptions()
    {
        GUILayout.BeginHorizontal("box");
        ClearCache = EditorGUILayout.Toggle("Clear Cache", ClearCache);
        AverageResults = EditorGUILayout.Toggle("Average Results", AverageResults);
        GUILayout.EndHorizontal();
        if (AverageResults)
        {
            GUILayout.Label("Note: Without previous results average will be ignored.", EditorStyles.miniLabel);
        }
        if (ClearCache)
        {
            GUILayout.Label("Note: This will clear GI data for ALL of Unity, which leads to more repeatable\n benchmarks, but also longer bake-times.", EditorStyles.miniLabel);
        }
        else
        {
            GUILayout.Label("Note: Benchmark will run twice on first run with ClearCache disabled.", EditorStyles.miniLabel);
        }
    }

    public void LightingOptionHandler()
    {
        if (ClearCache)
        {
            Lightmapping.Clear();
            Lightmapping.ClearDiskCache();
            Lightmapping.ClearLightingDataAsset();
            Debug.Log("PixelSpice - Cache Cleared");
            Last_Clear = true;
        }
        else if (!HasBaked)
        {
            Debug.Log("PixelSpice - Prebakeing Lightmap");
            Lightmapping.Bake();
            HasBaked = true;
            Debug.Log("PixelSpice - Prebake Complete");
            Last_Clear = false;
        }
        else
        {
            Debug.Log("PixelSpice - No Prebake Needed - Skipping");
            Last_Clear = false;
        }
    }

    public void LightmapBenchmark()
    {
        double TimeBefore = EditorApplication.timeSinceStartup;
        Lightmapping.Bake();
        double TimeElapsed = EditorApplication.timeSinceStartup - TimeBefore;

        StoreResults(TimeElapsed);
    }

    public void NavmeshOptions()
    {
        GUILayout.BeginHorizontal("box");
        ClearCache = EditorGUILayout.Toggle("Clear Cache", ClearCache); ;
        AverageResults = EditorGUILayout.Toggle("Average Results", AverageResults);
        GUILayout.EndHorizontal();
        if (AverageResults)
        {
            GUILayout.Label("Note: Without previous results average will be ignored.", EditorStyles.miniLabel);
        }
        if (EditorSceneManager.GetActiveScene().name != "Tanks")
        {
            GUILayout.Label("Note: NavMeshbaking benchmarks are best done with complex geometry.\n Consider using the Tanks scene for this benchmark.", EditorStyles.miniLabel);
        }
    }

    public void NavmeshOptionHandler()
    {
        if (ClearCache)
        {
            NavMeshBuilder.ClearAllNavMeshes();
            Last_Clear = true;
        }
        else
        {
            Last_Clear = false;
        }
    }

    public void NavmeshBenchmark()
    {
        double TimeBefore = EditorApplication.timeSinceStartup;
        NavMeshBuilder.BuildNavMesh();
        double TimeElapsed = EditorApplication.timeSinceStartup - TimeBefore;
        
        StoreResults(TimeElapsed);
    }

    public void BuildingOptions()
    {
        GUILayout.BeginHorizontal("box");
        ClearCache = EditorGUILayout.Toggle("Reimport Assets", ClearCache); ;
        AverageResults = EditorGUILayout.Toggle("Average Results", AverageResults);
        GUILayout.EndHorizontal();
        if (AverageResults)
        {
            GUILayout.Label("Note: Without previous results average will be ignored.", EditorStyles.miniLabel);
        }
        if (ClearCache)
        {
            GUILayout.Label("Note: Reimport time included in benchmark time.", EditorStyles.miniLabel);
        }
    }

    public void BuildingOptionHandler()
    {
        if (ClearCache)
        {
            Last_Clear = true;
        }
        else
        {
            Last_Clear = false;
        }
    }

    public void BuildingBenchmark()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Artemis Benchmarking Tool/Resources/Scenes/Shapes.unity", "Assets/Artemis Benchmarking Tool/Resources/Scenes/CorridorDemo.unity" , "Assets/Artemis Benchmarking Tool/Resources/Scenes/Tanks.unity" };
        buildPlayerOptions.locationPathName = "BenchmarkBuild";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows;
        buildPlayerOptions.options = BuildOptions.None;

        double TimeBefore = EditorApplication.timeSinceStartup;
        if (ClearCache)
        {
            AssetDatabase.Refresh();
        }
        BuildPipeline.BuildPlayer(buildPlayerOptions);
        double TimeElapsed = EditorApplication.timeSinceStartup - TimeBefore;

        StoreResults(TimeElapsed);
    }

    public void StoreResults(double TimeElapsed)
    {
        if (AverageResults)
        {
            //If we're averaging the results from previous runs
            //Add new results to results array
            float[] tempResults = new float[results.Length + 1];
            for (int i = 0; i < results.Length; i++)
            {
                tempResults[i + 1] = results[i];
            }
            tempResults[0] = Mathf.Round((float)TimeElapsed);

            results = new float[results.Length + 1];
            results = tempResults;
        }
        else
        {
            //If we aren't averaging results, clear the results array and load results 
            results = new float[1];
            results[0] = Mathf.Round((float)TimeElapsed);
        }

        //Average results
        float sum = 0;
        for (int i = 0; i < results.Length; i++)
            sum += results[i];

        //Check how many matches for current config we have in our loaded results
        int PreloadedResultsLength = 0;
        for (int i = 0; i < LoadedResults.Length; i++)
        {
            if (BMType == LoadedResults[i].BenchmarkType && EditorSceneManager.GetActiveScene().name == LoadedResults[i].Scene && ClearCache == LoadedResults[i].CacheCleared)
            {
                PreloadedResultsLength += 1;
            }
        }

        //Empty out FinalResults array
        FinalResults = new PreLoadedResults[PreloadedResultsLength + 1];

        //Loads results into final results.
        FinalResults[0].Duration = sum / results.Length; //Averages from Results array, which is only one item if we aren't averaging the results
        FinalResults[0].Scene = Last_SceneName;
        FinalResults[0].BenchmarkType = BMType;
        FinalResults[0].CacheCleared = ClearCache;
        FinalResults[0].Title = SystemInfo.processorType.ToString();
        FinalResults[0].TypeFlag = "Current";



        //Load Results from file here
        int Counter = 1;
        for (int i = 0; i < LoadedResults.Length; i++)
        {
            if (BMType == LoadedResults[i].BenchmarkType && EditorSceneManager.GetActiveScene().name == LoadedResults[i].Scene && ClearCache == LoadedResults[i].CacheCleared)
            {
                FinalResults[Counter] = LoadedResults[i];
                Counter += 1;
            }
        }

        //MinMax is used for result display scaling
        UpdateMinMax();
        
        //Sort Array
        Array.Sort<PreLoadedResults>(FinalResults, (x, y) => x.Duration.CompareTo(y.Duration));
    }

    public void UpdateMinMax()
    {
        MaxResult = 0;
        MinResult = 0;
        if (FinalResults.Length > 0)
        {
            for (int i = 0; i < FinalResults.Length; i++)
            {
                if (MaxResult < FinalResults[i].Duration)
                {
                    MaxResult = FinalResults[i].Duration;
                }
                if (MinResult > FinalResults[i].Duration)
                {
                    MinResult = FinalResults[i].Duration;
                }
            }
        }
    }

    public void ImportFromFile()
    {
        string path = EditorUtility.OpenFilePanel("Select Results File", "", "txt");
        //Split loading so we can call it with the preloaded during Awake
        LoadFromFile(path);
    }

    public void LoadFromFile(string path)
    {
        ///////////////////////////Save file
        if (File.Exists(path))
        {
            BinaryFormatter BinaryFormatter = new BinaryFormatter();
            FileStream ResultsFile = File.OpenRead(path);
            LoadedResults = (PreLoadedResults[])BinaryFormatter.Deserialize(ResultsFile);
            ResultsFile.Close();
        }
    }

    public void ExportToFile()
    {
        //Get save location and path
        string path = EditorUtility.SaveFilePanel("Save results to file", "", "ResultsFile.txt", "txt");

        //Add all loaded results & the most recent result
        PreLoadedResults[] TempResults;
        if (FinalResults.Length == 0)
        {
            TempResults = new PreLoadedResults[LoadedResults.Length];
            TempResults = LoadedResults;
        }
        else
        {
            TempResults = new PreLoadedResults[LoadedResults.Length + 1];
            TempResults[0] = FinalResults[0];
            if (TempResults[0].Title.Contains("Intel"))
            {
                TempResults[0].TypeFlag = "Intel";
            }
            else if (TempResults[0].Title.Contains("AMD"))
            {
                TempResults[0].TypeFlag = "AMD";
            }
            else
            {
                TempResults[0].TypeFlag = "Other";
            }

            for (int i = 0; i < LoadedResults.Length; i++)
            {
                TempResults[i+1] = LoadedResults[i];
            }
        }
        

        BinaryFormatter BinaryFormatter = new BinaryFormatter();
        FileStream ResultsFile = File.Create(path);
        BinaryFormatter.Serialize(ResultsFile, TempResults);
        ResultsFile.Close();
    }
}
