using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PolyToolkit;

public class LoadGooglePolyAsset : MonoBehaviour {

    // Text where we display the current status.
    public Text statusText;

    // Use this for initialization
    void Start () {
        Debug.Log("Requesting asset...");
        PolyApi.GetAsset("assets/5vbJ5vildOq", GetAssetCallback);
        //    statusText.text = "Requesting...";

    }

    // Update is called once per frame
    void Update () {
		
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
        options.desiredSize = 5.0f;
        // We want the imported assets to be recentered such that their centroid coincides with the origin:
        options.recenter = true;

        //    statusText.text = "Importing...";
        PolyApi.Import(result.Value, options, ImportAssetCallback);
    }

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
        result.Value.gameObject.AddComponent<Rotate>();
    }
}
