using System.Collections;
using System.Collections.Generic;
using Obi;
using UnityEngine;

public class RamdonizeWind : MonoBehaviour
{
    public float angleYCycle = 13f;
    public float angleXCycle = 11f;
    public float intensityCycle = 7f;
    public AnimationCurve intensityCurve;
    public AnimationCurve angleYCurve;
    public AnimationCurve angleXCurve;
    public float intensity = 1000f;
    public float angleXRange = 30f;
    public float angleYRange = 360f;
    public ObiAmbientForceZone ambientForceZone;


    public void Update()
    {
        float t_angle_Y = Mathf.Repeat(Time.time, angleYCycle) / angleYCycle;
        float t_angle_X = Mathf.Repeat(Time.time, angleXCycle) / angleXCycle;
        float t_intensity = Mathf.Repeat(Time.time, intensityCycle) / intensityCycle;
        transform.eulerAngles = new Vector3(angleXCurve.Evaluate(t_angle_X) * 360f, angleYCurve.Evaluate(t_angle_Y) * 360f,0);
        ambientForceZone.intensity = intensityCurve.Evaluate(t_intensity) * intensity;
    }
}
