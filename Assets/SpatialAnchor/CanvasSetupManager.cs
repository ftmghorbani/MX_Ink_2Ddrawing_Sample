using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CanvasSetupManager : MonoBehaviour
{
    public static CanvasSetupManager Instance;

    [SerializeField]
    private GameObject _anchorPrefab;

    [SerializeField]
    private StylusHandler _stylusHandler;

    [SerializeField]
    private GameObject _canvasPrefab;

    [SerializeField]
    private LineDrawing _lineDrawing;

    private OVRSpatialAnchor[] _anchors = new OVRSpatialAnchor[4];
    private int _currentAnchorIndex = 0;

    private OVRSpatialAnchor _canvasAnchor;
    private GameObject _canvasInstance;

    private List<OVRSpatialAnchor> _savedAnchors = new List<OVRSpatialAnchor>();

    private const float UnityPlaneSize = 10f;
    private bool _isCreatingAnchor = false;

    private void Awake()
    {
        Debug.Log("[CanvasSetupManager] Awake called.");

        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[CanvasSetupManager] Instance set.");
            PrintSavedAnchors();
        }
        else
        {
            Debug.LogError("[CanvasSetupManager] Instance already exists! Destroying this object.");
            Destroy(this);
        }
    }

    void Update()
    {
        if (_currentAnchorIndex < 3 && _stylusHandler.CurrentState.tip_value > 0.98f && !_isCreatingAnchor)
        {
            StartCoroutine(CreateAnchorAtStylusPose());
        }

        // Only create the canvas if it doesn't exist yet
        if (_currentAnchorIndex == 3 && _stylusHandler.CurrentState.tip_value > 0.98f && !_isCreatingAnchor && _canvasInstance == null)
        {
            StartCoroutine(CreateCanvas());
        }

        if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            Debug.Log("[CanvasSetupManager] Resetting anchors and canvas.");
            ResetAnchorsAndCanvas();
        }

        if (OVRInput.GetDown(OVRInput.Button.Four))
        {
            Debug.Log("[CanvasSetupManager] Reloading canvas with A button.");
            LoadAnchors();
        }
    }

    private IEnumerator CreateAnchorAtStylusPose()
    {
        _isCreatingAnchor = true;

        Vector3 position = _stylusHandler.CurrentState.inkingPose.position;
        position += _stylusHandler.CurrentState.inkingPose.forward * 0.004f; // Offset the anchor slightly in front of the stylus

        Quaternion rotation = Quaternion.Euler(0, 0, 0);

        var anchorObject = Instantiate(_anchorPrefab, position, rotation);
        var anchor = anchorObject.AddComponent<OVRSpatialAnchor>();

        Debug.Log($"[CanvasSetupManager] Anchor created at position: {position}");

        SetupAnchorAsync(anchor, _currentAnchorIndex);
        _currentAnchorIndex++;

        // Wait until the tip value drops to zero before allowing the next anchor to be created
        yield return new WaitUntil(() => _stylusHandler.CurrentState.tip_value <= 0.01f);
        _isCreatingAnchor = false;
    }

    private IEnumerator CreateCanvas()
    {
        _isCreatingAnchor = true;

        // Early return if the canvas already exists
        if (_canvasInstance != null)
        {
            Debug.LogWarning("[CanvasSetupManager] Canvas already exists. Skipping creation.");
            _isCreatingAnchor = false;
            yield break;
        }

        // Canvas creation logic
        Vector3[] corners = new Vector3[4];

        for (int i = 0; i < 3; i++)
        {
            corners[i] = _anchors[i].transform.position;
            Debug.Log($"[CanvasSetupManager] Corner {i} position: {corners[i]}");
        }

        corners[3] = corners[0] + (corners[2] - corners[1]);

        Vector3 center = (corners[0] + corners[1] + corners[2] + corners[3]) / 4;
        Vector3 normal = Vector3.Cross(corners[2] - corners[1], corners[0] - corners[1]).normalized;

        if (Vector3.Dot(normal, Vector3.up) < 0)
        {
            normal = -normal;
        }

        Vector3 up = normal;
        Vector3 forward = Vector3.Cross(up, corners[1] - corners[0]).normalized;

        if (Vector3.Dot(forward, corners[1] - corners[0]) < 0)
        {
            forward = -forward;
        }

        _canvasInstance = Instantiate(_canvasPrefab, center, Quaternion.LookRotation(forward, up));

        Vector3 widthVector = corners[1] - corners[0];
        Vector3 heightVector = corners[2] - corners[1];
        float width = widthVector.magnitude;
        float height = heightVector.magnitude;
        _canvasInstance.transform.localScale = new Vector3(width / UnityPlaneSize, 1, height / UnityPlaneSize);

        Debug.Log($"[CanvasSetupManager] Canvas created at center: {center}, width: {width}, height: {height}");

        PlayerPrefs.SetFloat("CanvasWidth", width);
        PlayerPrefs.SetFloat("CanvasHeight", height);
        PlayerPrefs.Save();

        MeshCollider meshCollider = _canvasInstance.AddComponent<MeshCollider>();
        Mesh mesh = CreateQuadMesh(width, height);
        meshCollider.sharedMesh = mesh;

        _canvasAnchor = _canvasInstance.AddComponent<OVRSpatialAnchor>();
        SetupCanvasAnchorAsync(_canvasAnchor);

        _lineDrawing.SetCanvas(_canvasInstance);

        // Wait until the tip value drops to zero before finishing the canvas creation
        yield return new WaitUntil(() => _stylusHandler.CurrentState.tip_value <= 0.01f);
        _isCreatingAnchor = false;
    }

    private async void SetupAnchorAsync(OVRSpatialAnchor anchor, int index)
    {
        Debug.Log($"[CanvasSetupManager] Setting up anchor at index: {index}");

        while (!anchor.Created && !anchor.Localized)
        {
            await Task.Yield();
        }

        _anchors[index] = anchor;

        var saveResult = await anchor.SaveAnchorAsync();
        if (saveResult.Success)
        {
            Debug.Log($"[CanvasSetupManager] Anchor saved successfully at index: {index}");
            _savedAnchors.Add(anchor);
            SaveAnchors();
        }
        else
        {
            Debug.LogError($"[CanvasSetupManager] Failed to save anchor at index: {index}");
        }
    }

    private async void SetupCanvasAnchorAsync(OVRSpatialAnchor anchor)
    {
        while (!anchor.Created && !anchor.Localized)
        {
            await Task.Yield();
        }

        var saveResult = await anchor.SaveAnchorAsync();
        if (saveResult.Success)
        {
            PlayerPrefs.SetString("CanvasAnchor", anchor.Uuid.ToString());
            PlayerPrefs.Save();
            Debug.Log("[CanvasSetupManager] Canvas anchor saved successfully.");
        }
        else
        {
            Debug.LogError("[CanvasSetupManager] Failed to save canvas anchor.");
        }
    }

    private Mesh CreateQuadMesh(float width, float height)
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(-width / 2, 0, -height / 2);
        vertices[1] = new Vector3(width / 2, 0, -height / 2);
        vertices[2] = new Vector3(-width / 2, 0, height / 2);
        vertices[3] = new Vector3(width / 2, 0, height / 2);

        int[] tris = new int[6];
        tris[0] = 0;
        tris[1] = 2;
        tris[2] = 1;
        tris[3] = 2;
        tris[4] = 3;
        tris[5] = 1;

        Vector3[] normals = new Vector3[4];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = Vector3.up;
        }

        Vector2[] uv = new Vector2[4];
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.normals = normals;
        mesh.uv = uv;

        return mesh;
    }

    private void SaveAnchors()
    {
        PlayerPrefs.SetInt("AnchorCount", _savedAnchors.Count);
        for (int i = 0; i < _savedAnchors.Count; i++)
        {
            PlayerPrefs.SetString($"Anchor_{i}", _savedAnchors[i].Uuid.ToString());
            Debug.Log($"[CanvasSetupManager] Anchor {i} saved with UUID: {_savedAnchors[i].Uuid}");
        }
        PlayerPrefs.Save();
    }

    private async void LoadAnchors()
    {
        // Debug message for starting the loading process
        Debug.Log("[CanvasSetupManager] Starting LoadAnchors");

        int anchorCount = PlayerPrefs.GetInt("AnchorCount", 0);
        if (anchorCount == 0)
        {
            Debug.LogWarning("[CanvasSetupManager] No anchors found to load.");
            return;
        }

        _currentAnchorIndex = anchorCount;
        List<Guid> anchorUuids = new List<Guid>();
        for (int i = 0; i < anchorCount; i++)
        {
            string uuidStr = PlayerPrefs.GetString($"Anchor_{i}");
            if (Guid.TryParse(uuidStr, out Guid uuid))
            {
                anchorUuids.Add(uuid);
                Debug.Log($"[CanvasSetupManager] Anchor UUID loaded: {uuid}");
            }
        }

        List<OVRSpatialAnchor> loadedAnchors = new List<OVRSpatialAnchor>();
        foreach (var uuid in anchorUuids)
        {
            var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
            var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(new HashSet<Guid> { uuid }, unboundAnchors);
            if (result.Success)
            {
                foreach (var unboundAnchor in unboundAnchors)
                {
                    var go = Instantiate(_anchorPrefab);
                    var anchor = go.AddComponent<OVRSpatialAnchor>();
                    unboundAnchor.BindTo(anchor);

                    // Ensure the anchor is activated and positioned correctly
                    anchor.gameObject.SetActive(true);
                    go.transform.position = anchor.transform.position;
                    go.transform.rotation = anchor.transform.rotation;

                    Debug.Log($"[CanvasSetupManager] Anchor bound: {anchor.Uuid} at position {go.transform.position}, active: {go.activeSelf}");
                    loadedAnchors.Add(anchor);
                }
            }
            else
            {
                Debug.LogError($"[CanvasSetupManager] Failed to load anchor with UUID: {uuid}");
            }
        }

        // Update the _anchors array with the loaded anchors
        for (int i = 0; i < loadedAnchors.Count; i++)
        {
            _anchors[i] = loadedAnchors[i];
            Debug.Log($"[CanvasSetupManager] Anchor {i} set at position: {_anchors[i].transform.position}, active: {_anchors[i].gameObject.activeSelf}");
        }
        _savedAnchors = loadedAnchors;

        // Load the canvas anchor separately
        string canvasUuidStr = PlayerPrefs.GetString("CanvasAnchor", string.Empty);
        if (!string.IsNullOrEmpty(canvasUuidStr) && Guid.TryParse(canvasUuidStr, out Guid canvasUuid))
        {
            var unboundCanvasAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
            var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(new HashSet<Guid> { canvasUuid }, unboundCanvasAnchors);
            if (result.Success)
            {
                foreach (var unboundAnchor in unboundCanvasAnchors)
                {
                    var go = Instantiate(_canvasPrefab);
                    var anchor = go.AddComponent<OVRSpatialAnchor>();
                    unboundAnchor.BindTo(anchor);
                    _canvasAnchor = anchor;
                    _canvasInstance = go;

                    Debug.Log("[CanvasSetupManager] Canvas anchor bound.");

                    // Retrieve the saved canvas dimensions
                    float savedWidth = PlayerPrefs.GetFloat("CanvasWidth", 1);
                    float savedHeight = PlayerPrefs.GetFloat("CanvasHeight", 1);

                    // Set the canvas scale
                    _canvasInstance.transform.localScale = new Vector3(savedWidth / UnityPlaneSize, 1, savedHeight / UnityPlaneSize);

                    // Add a MeshCollider to the canvas
                    MeshCollider meshCollider = _canvasInstance.AddComponent<MeshCollider>();
                    Mesh mesh = CreateQuadMesh(savedWidth, savedHeight);
                    meshCollider.sharedMesh = mesh;

                    // Set the canvas reference in LineDrawing
                    _lineDrawing.SetCanvas(_canvasInstance);
                    Debug.Log($"[CanvasSetupManager] Canvas instance set in LineDrawing at position: {_canvasInstance.transform.position}, active: {_canvasInstance.activeSelf}");
                }
            }
            else
            {
                Debug.LogError($"[CanvasSetupManager] Failed to load canvas anchor with UUID: {canvasUuid}");
            }
        }
        else
        {
            Debug.LogWarning("[CanvasSetupManager] No canvas anchor found to load.");
        }
    }



    private void PrintSavedAnchors()
    {
        int anchorCount = PlayerPrefs.GetInt("AnchorCount", 0);
        Debug.Log($"[CanvasSetupManager] Total saved anchors: {anchorCount}");
        for (int i = 0; i < anchorCount; i++)
        {
            string uuidStr = PlayerPrefs.GetString($"Anchor_{i}");
            Debug.Log($"[CanvasSetupManager] Saved Anchor {i} UUID: {uuidStr}");
        }
        string canvasUuidStr = PlayerPrefs.GetString("CanvasAnchor", string.Empty);
        Debug.Log($"[CanvasSetupManager] Saved Canvas UUID: {canvasUuidStr}");
    }

    private void ResetAnchorsAndCanvas()
    {
        Debug.Log("[CanvasSetupManager] Resetting anchors and canvas.");

        // Destroy existing anchors (including the fourth one and the canvas anchor)
        for (int i = 0; i < 4; i++)
        {
            if (_anchors[i] != null)
            {
                Destroy(_anchors[i].gameObject);
                _anchors[i] = null; // Ensure the anchor is cleared from the array
                Debug.Log($"[CanvasSetupManager] Anchor {i} destroyed.");
            }
        }

        // Reset the anchor index to 0
        _currentAnchorIndex = 0;

        // Destroy the canvas anchor if it exists
        if (_canvasAnchor != null)
        {
            Destroy(_canvasAnchor.gameObject);
            _canvasAnchor = null;
            Debug.Log("[CanvasSetupManager] Canvas anchor destroyed.");
        }

        // Destroy the existing canvas instance
        if (_canvasInstance != null)
        {
            Destroy(_canvasInstance);
            _canvasInstance = null;
            Debug.Log("[CanvasSetupManager] Canvas instance destroyed.");
        }

        // Clear saved anchors and PlayerPrefs
        _savedAnchors.Clear();
        PlayerPrefs.DeleteKey("AnchorCount");
        PlayerPrefs.DeleteKey("CanvasAnchor");
        PlayerPrefs.DeleteKey("CanvasWidth");
        PlayerPrefs.DeleteKey("CanvasHeight");
        for (int i = 0; i < 4; i++)
        {
            PlayerPrefs.DeleteKey($"Anchor_{i}");
        }
        PlayerPrefs.Save();

        Debug.Log("[CanvasSetupManager] Anchors and canvas reset complete.");
    }
}
