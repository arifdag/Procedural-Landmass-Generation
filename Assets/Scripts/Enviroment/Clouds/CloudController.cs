using UnityEngine;

[ExecuteInEditMode]
public class CloudController : MonoBehaviour
{
    public Material cloudMaterial;
    public WorleyNoiseGenerator noiseGenerator;
    
    [Range(1, 128)]
    public int raymarchSteps = 80; 
    
    [Range(0, 10)]
    public float density = 1.5f;
    
    [Range(0, 1)]
    public float noiseThreshold = 0.35f;
    
    [Range(0.01f, 0.5f)]
    public float edgeFadeDistance = 0.1f;

    void Update()
    {
        if (cloudMaterial != null && noiseGenerator != null && noiseGenerator.generatedTexture != null)
        {
            // Set the generated 3D noise texture on the material
            cloudMaterial.SetTexture("_CloudNoiseTex3D", noiseGenerator.generatedTexture);
            

            cloudMaterial.SetInt("_RaymarchSteps", raymarchSteps);
            cloudMaterial.SetFloat("_Density", density);
            cloudMaterial.SetFloat("_NoiseThreshold", noiseThreshold);
            cloudMaterial.SetFloat("_EdgeFadeDistance", edgeFadeDistance);
        }
    }
}