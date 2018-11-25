using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PolyToolkit;

public class LoadGooglePolyAsset : MonoBehaviour
{

    // Text where we display the current status.
    public Text statusText;
    public float desiredInitialSize = 1.0f;
    public string lastRequestedAsset = "";
    public bool findBestMatchRequested = false;

    // Use this for initialization
    void Start()
    {

    }

    public void LoadAsset()
    {
        Debug.Log("Requesting asset...");
        //        PolyApi.GetAsset("assets/5vbJ5vildOq", GetAssetCallback);
                PolyApi.ListAssets(PolyListAssetsRequest.Featured(), OnAssetListReturned);

        //    statusText.text = "Requesting...";
    }

    public void LoadAsset(string desiredName, bool findBestMatch = false)
    {
        Debug.Log("Requesting asset list using keyword: " + desiredName + " ...");
        //        PolyApi.ListAssets(PolyListAssetsRequest.Featured(), OnAssetListReturned);
        //        PolyApi.GetAsset("assets/5vbJ5vildOq", GetAssetCallback);
        //    statusText.text = "Requesting...";

        PolyListAssetsRequest req = new PolyListAssetsRequest();
        // Search by keyword:
        req.keywords = desiredName;
        // Only curated assets:
        req.curated = true;
        // Limit complexity to medium.
        req.maxComplexity = PolyMaxComplexityFilter.MEDIUM;
        // Only Blocks objects.
        req.formatFilter = PolyFormatFilter.BLOCKS;
        // Order from best to worst.
        req.orderBy = PolyOrderBy.BEST;
        // Up to 20 results per page.
        req.pageSize = 20;

        // Cache the request info because google doesn't track what you asked for
        lastRequestedAsset = desiredName;
        findBestMatchRequested = findBestMatch;
        
        // Send the request.
            PolyApi.ListAssets(req, OnAssetListReturned);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnAssetListReturned(PolyStatusOr<PolyListAssetsResult> result)
    {
        if (!result.Ok)
        {
            // Handle error.
            return;
        }

        PolyAsset bestResult = null;

        // Success. result.Value is a PolyListAssetsResult and
        // result.Value.assets is a list of PolyAssets.
        foreach (PolyAsset asset in result.Value.assets)
        {
            if(bestResult == null)
            {
                bestResult = asset;
            }
            // Do something with the asset here.
            if(!findBestMatchRequested || asset.displayName == lastRequestedAsset)
            {
                bestResult = asset;
            }

        }

        Debug.Log("Loading asset " + bestResult.displayName + " ...");
        PolyApi.GetAsset(bestResult.name, GetAssetCallback);
    }


    // Callback invoked when the featured assets results are returned.
    private void GetAssetCallback(PolyStatusOr<PolyAsset> result)
    {
        if (!result.Ok)
        {
            Debug.LogError("Failed to get assets. Reason: " + result.Status);
            //    statusText.text = "ERROR: " + result.Status;
            return;
        }
        Debug.Log("Successfully got asset!");

        // Set the import options.
        PolyImportOptions options = PolyImportOptions.Default();
        // We want to rescale the imported mesh to a specific size.
        options.rescalingMode = PolyImportOptions.RescalingMode.FIT;
        // The specific size we want assets rescaled to (fit in a 5x5x5 box):
        options.desiredSize = desiredInitialSize;
        // We want the imported assets to be recentered such that their centroid coincides with the origin:
        options.recenter = true;

        //    statusText.text = "Importing...";
        PolyApi.Import(result.Value, options, ImportAssetCallback);
    }

    // jofoste - replace this with a layout system
    // hack to make the assets stack
    int numberOfAssetsLoaded = 0;

    // Callback invoked when an asset has just been imported.
    private void ImportAssetCallback(PolyAsset asset, PolyStatusOr<PolyImportResult> result)
    {
        if (!result.Ok)
        {
            Debug.LogError("Failed to import asset. :( Reason: " + result.Status);
            //      statusText.text = "ERROR: Import failed: " + result.Status;
            return;
        }
        Debug.Log("Successfully imported asset!");

        // Show attribution (asset title and author).
        //    statusText.text = asset.displayName + "\nby " + asset.authorName;

        // Here, you would place your object where you want it in your scene, and add any
        // behaviors to it as needed by your app. As an example, let's just make it
        // slowly rotate:
        // put the piano a little to the right so it isn't near the main character
        float x = 2 + desiredInitialSize * numberOfAssetsLoaded % 4;
        float y = desiredInitialSize * (0.5f + numberOfAssetsLoaded / 4);
        result.Value.gameObject.transform.position = new Vector3(x, y, 0);
        result.Value.gameObject.AddComponent<Rotate>();
        ++numberOfAssetsLoaded;
    }
}
