using UnityEngine;


[CreateAssetMenu]
public class HeightMapSettings : UpdatableData
{
    public NoiseSettings noiseSettings;
    
    [SerializeField] public float heightMultiplier;
    [SerializeField] public AnimationCurve heightCurve;
    
    [SerializeField] public bool useFallof;
    [SerializeField] public AnimationCurve falloffCurve;
    

    public float minHeight => heightMultiplier * heightCurve.Evaluate(0);

    public float maxHeight =>  heightMultiplier * heightCurve.Evaluate(1);

#if UNITY_EDITOR

    protected override void OnValidate()
    {
        noiseSettings.ValidateValues();
        base.OnValidate();
    }
#endif
}