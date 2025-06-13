using UnityEngine;

[ExecuteInEditMode]
public class WorleyNoiseGenerator : MonoBehaviour
{
    public ComputeShader worleyComputeShader;
    [Header("Texture Settings")]
    public int resolution = 64;
    public bool autoUpdate = true;
    public Material previewMaterial;


    [Header("Worley Base Settings")]
    public int baseCellCount = 8; // Renamed from cellCount for clarity with FBM
    public float randomSeed = 0f;

    [Header("FBM Settings")]
    [Range(1, 10)]
    public int octaves = 4;
    [Range(1.1f, 4.0f)]
    public float lacunarity = 2.0f; // How much detail (frequency) increases each octave
    [Range(0.1f, 0.9f)]
    public float persistence = 0.5f; // How much amplitude decreases each octave

    public RenderTexture generatedTexture { get; private set; }
    private ComputeBuffer featurePointsBuffer;
    private const int THREAD_GROUP_SIZE = 8;

    void OnValidate()
    {
        if (octaves < 1) octaves = 1;
        if (autoUpdate && Application.isPlaying == false)
        {
            UnityEditor.EditorApplication.delayCall -= GenerateTexture; // Remove previous if any
            UnityEditor.EditorApplication.delayCall += GenerateTexture;
        }
    }

    void Start()
    {
        if (generatedTexture == null)
        {
            GenerateTexture();
        }
    }

    public void GenerateTexture()
    {
        if (worleyComputeShader == null)
        {
            Debug.LogError("Worley Compute Shader not assigned!");
            return;
        }
        if (resolution <= 0 || baseCellCount <= 0)
        {
            Debug.LogError("Resolution and Base Cell Count must be positive.");
            return;
        }
        if (resolution % THREAD_GROUP_SIZE != 0)
        {
            Debug.LogWarning($"Resolution ({resolution}) is not a multiple of thread group size ({THREAD_GROUP_SIZE}). This is generally fine as the shader has boundary checks, but performance might be slightly affected.");
        }

        UnityEngine.Random.InitState((int)randomSeed);

        int totalCells = baseCellCount * baseCellCount * baseCellCount;
        Vector3[] featurePoints = new Vector3[totalCells];
        for (int i = 0; i < totalCells; i++)
        {
            featurePoints[i] = new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        }

        ReleaseBuffers();
        featurePointsBuffer = new ComputeBuffer(totalCells, sizeof(float) * 3);
        featurePointsBuffer.SetData(featurePoints);

        if (generatedTexture != null)
        {
            generatedTexture.Release();
        }
        // Use ARGBFloat for RGBA output with high precision
        generatedTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
        generatedTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        generatedTexture.volumeDepth = resolution;
        generatedTexture.enableRandomWrite = true;
        generatedTexture.wrapMode = TextureWrapMode.Repeat;
        generatedTexture.filterMode = FilterMode.Bilinear; // Or Trilinear
        generatedTexture.Create();

        int kernelHandle = worleyComputeShader.FindKernel("WorleyFBMKernel"); // Updated kernel name
        if (kernelHandle < 0) {
            Debug.LogError("Kernel 'WorleyFBMKernel' not found in compute shader.");
            ReleaseBuffers(); // Clean up buffer if kernel is missing
            return;
        }

        worleyComputeShader.SetInt("_Resolution", resolution);
        worleyComputeShader.SetInts("_BaseCellCount", new int[] { baseCellCount, baseCellCount, baseCellCount });
        worleyComputeShader.SetInt("_Octaves", octaves);
        worleyComputeShader.SetFloat("_Lacunarity", lacunarity);
        worleyComputeShader.SetFloat("_Persistence", persistence);

        worleyComputeShader.SetBuffer(kernelHandle, "_FeaturePointsBuffer", featurePointsBuffer);
        worleyComputeShader.SetTexture(kernelHandle, "_ResultTexture", generatedTexture);

        int threadGroups = Mathf.CeilToInt((float)resolution / THREAD_GROUP_SIZE);
        worleyComputeShader.Dispatch(kernelHandle, threadGroups, threadGroups, threadGroups);

        Debug.Log($"Generated FBM Worley 3D Texture ({resolution}^3) with {baseCellCount}^3 base cells, {octaves} octaves.");
        
        if (previewMaterial != null)
        {
            previewMaterial.SetTexture("_NoiseTex", generatedTexture);
        }

        // Example: Make it globally available if needed
        // Shader.SetGlobalTexture("_WorleyFBMNoise3D", generatedTexture);
    }

    public Texture3D CopyRenderTextureToTexture3D(RenderTexture rt)
    {
        if (rt == null || rt.dimension != UnityEngine.Rendering.TextureDimension.Tex3D)
        {
            Debug.LogError("Invalid RenderTexture provided for copying to Texture3D.");
            return null;
        }

        // Ensure Texture3D format matches the RenderTexture format (or is compatible)
        // ARGBFloat in RenderTexture corresponds to RGBAFloat in Texture3D
        Texture3D tex3D = new Texture3D(rt.width, rt.height, rt.volumeDepth, TextureFormat.RGBAFloat, false);
        tex3D.wrapMode = rt.wrapMode;
        tex3D.filterMode = rt.filterMode;

        Graphics.CopyTexture(rt, 0, 0, tex3D, 0, 0);

        tex3D.Apply(false);
        return tex3D;
    }

    void OnDisable()
    {
        ReleaseAllResources();
    }

    void OnDestroy()
    {
        ReleaseAllResources();
    }

    private void ReleaseAllResources()
    {
        ReleaseBuffers();
        if (generatedTexture != null)
        {
            // Check if it's safe to release (e.g. not in a build if it's an asset)
            if (Application.isEditor && !Application.isPlaying)
            {
                // In editor mode, if it's a temporary RT, it's usually safe.
                // If it was saved as an asset, this might be an issue.
                // For dynamically generated RTs, this is fine.
            }
            generatedTexture.Release();
            generatedTexture = null;
        }
    }

    private void ReleaseBuffers()
    {
        if (featurePointsBuffer != null)
        {
            featurePointsBuffer.Release();
            featurePointsBuffer = null;
        }
    }
}