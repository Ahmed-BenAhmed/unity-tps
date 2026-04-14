using UnityEngine;

public class PlanetMotion : MonoBehaviour
{
    public GameObject soleil;
    public float vitesseRotationSurSoi = 35f;
    public float vitesseRotationAutourSoleil = 70f;

    void Update()
    {
        // rotation sur elle-même
        transform.Rotate(0, vitesseRotationSurSoi * Time.deltaTime, 0);

        // rotation autour du soleil
        if (soleil != null)
        {
            transform.RotateAround(
                soleil.transform.position,
                -soleil.transform.up,
                vitesseRotationAutourSoleil * Time.deltaTime
            );
        }
    }
}