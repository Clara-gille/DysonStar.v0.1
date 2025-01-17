using System.Collections;
using System.Collections.Generic;
using Gravity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShipHUD : MonoBehaviour {

    [Header ("Aim")]
    public float dotSize = 1;
    public float minAimAngle = 30;
    public Image centreDot;
    public TMP_Text planetInfo;
    public TMP_Text matchHint;

    [Header ("Velocity indicators")]
    public VelocityIndicator velocityHorizontal;
    public VelocityIndicator velocityVertical;
    public Vector2 velocityIndicatorSizeMinMax;
    public Vector2 velocityIndicatorThicknessMinMax;
    public float maxVisDst;
    public float velocityDisplayScale = 1;
    private const float MaxVelocityDisplay = 100;

    public SpaceObject lockedBody;
    Camera cam;
    Transform camT;
    UILocker _uiLocker;
    SpaceShipController ship;
    
    [SerializeField] private AudioSource selectSound;
    
    void Update () {
        UpdateUI ();
    }

    void Init () {
        if (cam == null) {
            cam = Camera.main;
        }
        camT = cam.transform;

        if (_uiLocker == null) {
            _uiLocker = GetComponent<UILocker> ();
        }

        if (ship == null) {
            ship = FindObjectOfType<SpaceShipController> ();
        }
    }

    void UpdateUI () {
        Init ();

        centreDot.rectTransform.localScale = Vector3.one * dotSize;
        SpaceObject aimedBody = FindAimedBody ();

        if (aimedBody && aimedBody != lockedBody) {
            _uiLocker.DrawLockOnUI (aimedBody, false);
        }

        if (Input.GetMouseButtonDown (0)) {
            if (lockedBody == aimedBody) {
                lockedBody = null;
            } else {
                lockedBody = aimedBody;
                selectSound.pitch = Random.Range (0.95f, 1f);
                selectSound.Play();
            }
        }

        if (lockedBody) {
            _uiLocker.DrawLockOnUI (lockedBody, true);
            DrawPlanetHUD (lockedBody);
        } else {
            SetHudActive (false);
        }
    }

    void SetHudActive (bool active) {
        planetInfo.gameObject.SetActive (active);
        velocityHorizontal.SetActive (active);
        velocityVertical.SetActive (active);
        matchHint.gameObject.SetActive (active);
    }

    void DrawPlanetHUD (SpaceObject planet) {
        SetHudActive (true);
        Vector3 dirToPlanet = (planet.transform.position - camT.position).normalized;
        float dstToPlanetCentre = (planet.transform.position - camT.position).magnitude;
        float dstToPlanetSurface = dstToPlanetCentre - planet.radius;

        // Calculate horizontal/vertical axes relative to direction toward planet
        Vector3 horizontal = Vector3.Cross (dirToPlanet, camT.up).normalized;
        horizontal *= Mathf.Sign (Vector3.Dot (horizontal, camT.right)); // make sure roughly same direction as right vector of cam
        Vector3 vertical = Vector3.Cross (dirToPlanet, horizontal).normalized;
        vertical *= Mathf.Sign (Vector3.Dot (vertical, camT.up));

        // Calculate relative velocity
        Vector3 relativeVelocityWorldSpace = ship._rb.velocity - planet.Velocity;
        float vx = -Vector3.Dot (relativeVelocityWorldSpace, horizontal);
        float vy = -Vector3.Dot (relativeVelocityWorldSpace, vertical);
        float vz = Vector3.Dot (relativeVelocityWorldSpace, dirToPlanet);
        Vector3 relativeVelocity = new Vector3 (vx, vy, vz);

        // Planet info
        Vector3 planetInfoWorldPos = planet.transform.position + horizontal * (planet.radius * _uiLocker.lockedRadiusMultiplier * 1.8f) + vertical * (planet.radius * 1.5f) ;
        planetInfo.gameObject.SetActive (PointIsOnScreen (planetInfoWorldPos));
        planetInfo.rectTransform.localPosition = CalculateUIPos (planetInfoWorldPos);
        planetInfo.text = $"{planet.bodyName} \n{FormatDistance(dstToPlanetSurface)} \n{relativeVelocity.z:0}m/s";

        // Relative velocity lines
        if (PointIsOnScreen (planet.transform.position)) {
            float arrowHeadSizePercent = dstToPlanetSurface / maxVisDst;
            float arrowHeadSize = Mathf.Lerp (velocityIndicatorSizeMinMax.y, velocityIndicatorSizeMinMax.x, arrowHeadSizePercent);
            float indicatorThickness = Mathf.Lerp (velocityIndicatorThicknessMinMax.y, velocityIndicatorThicknessMinMax.x, dstToPlanetSurface / maxVisDst);
            float indicatorAngle = (relativeVelocity.x < 0) ? 180 : 0;
            var indicatorPos = CalculateUIPos (planet.transform.position + horizontal * planet.radius * _uiLocker.lockedRadiusMultiplier * Mathf.Sign (relativeVelocity.x));
            float indicatorMagnitude = Mathf.Abs (relativeVelocity.x) * velocityDisplayScale;
            velocityHorizontal.Update (indicatorAngle, indicatorPos, indicatorMagnitude, arrowHeadSize, indicatorThickness);

            indicatorAngle = (relativeVelocity.y < 0) ? 270 : 90;
            indicatorPos = CalculateUIPos (planet.transform.position + camT.up * planet.radius * _uiLocker.lockedRadiusMultiplier * Mathf.Sign (relativeVelocity.y));
            indicatorMagnitude = Mathf.Abs (relativeVelocity.y) * velocityDisplayScale;
            velocityVertical.Update (indicatorAngle, indicatorPos, indicatorMagnitude, arrowHeadSize, indicatorThickness);

        } else {
            velocityHorizontal.SetActive (false);
            velocityVertical.SetActive (false);
        }

    }

    SpaceObject FindAimedBody () {
        SpaceObject[] bodies = FindObjectsOfType<SpaceObject> ();
        SpaceObject aimedBody = null;

        Vector3 viewForward = cam.transform.forward;
        Vector3 viewOrigin = cam.transform.position;

        float nearestSqrDst = float.PositiveInfinity;

        // If aimed directly at any body, return the closest one
        foreach (var body in bodies) {
            if (RaySphere (body.transform.position, body.radius, viewOrigin, viewForward, out var intersection)) {
                float sqrDst = (viewOrigin - intersection).sqrMagnitude;
                if (sqrDst < nearestSqrDst) {
                    nearestSqrDst = sqrDst;
                    aimedBody = body;
                }
            }
        }

        if (aimedBody) {
            return aimedBody;
        }

        // Return body with min angle to view direction
        float minAngle = minAimAngle * Mathf.Deg2Rad;

        foreach (var body in bodies) {
            Vector3 offsetToBody = body.transform.position - cam.transform.position;
           
            float aimAngle = Mathf.Acos (Vector3.Dot (viewForward, offsetToBody.normalized));

            if (aimAngle < minAngle) {
                minAngle = aimAngle;
                aimedBody = body;
            }
        }

        return aimedBody;
    }

    bool PointIsOnScreen (Vector3 worldPoint) {
        Vector3 p = cam.WorldToViewportPoint (worldPoint);
        return p.x >= 0 && p is { x: <= 1, y: >= 0 } and { y: <= 1, z: > 0 };
    }

    static string FormatDistance (float distance) {
        const int maxMetreDst = 1000;
        string dstString = (distance < maxMetreDst) ? (int) distance + "m" : $"{distance/1000:0}km";
        return dstString;
    }

    Vector2 CalculateUIPos (Vector3 worldPos) {
        const int referenceWidth = 1920;
        const int referenceHeight = 1080;

        Vector3 viewportCentre = cam.WorldToViewportPoint (worldPos);
        if (viewportCentre.z <= 0) {
            viewportCentre.x = (viewportCentre.x <= 0.5f) ? 1 : 0;
            viewportCentre.y = (viewportCentre.y <= 0.5f) ? 1 : 0;
        }

        return new Vector2 ((viewportCentre.x - 0.5f) * referenceWidth, (viewportCentre.y - 0.5f) * referenceHeight);
    }

    [System.Serializable]
    public struct VelocityIndicator {
        public Image line;
        public Image head;

        public void Update (float angle, Vector2 pos, float magnitude, float arrowHeadSize, float thickness) {
            line.rectTransform.pivot = new Vector2 (0, 0.5f);
            line.rectTransform.eulerAngles = Vector3.forward * angle;
            line.rectTransform.localPosition = pos;
            line.rectTransform.sizeDelta = new Vector2 (magnitude, thickness);
            line.material.SetVector ("_Size", line.rectTransform.sizeDelta);
            

            head.rectTransform.localPosition = pos + (Vector2) line.rectTransform.right * magnitude;
            head.rectTransform.eulerAngles = Vector3.forward * angle;

            head.rectTransform.localScale = Vector3.one * arrowHeadSize;
            
            float opacity = magnitude / MaxVelocityDisplay;
            // Adjust opacity of the head
            Color headColor = head.color;
            headColor.a = opacity; // Convert percentage to fraction
            head.color = headColor;
            
            Color lineColor = line.color;
            lineColor.a = opacity; // Convert percentage to fraction
            line.color = lineColor;
        }

        public void SetActive (bool active) {
            line.gameObject.SetActive (active);
            head.gameObject.SetActive (active);
        }
    }
    
    public static bool RaySphere (Vector3 centre, float radius, Vector3 rayOrigin, Vector3 rayDir, out Vector3 intersectionPoint) {
        // See: http://viclw17.github.io/2018/07/16/raytracing-ray-sphere-intersection/
        Vector3 offset = rayOrigin - centre;
        float a = Vector3.Dot (rayDir, rayDir); // sqr ray length (in case of non-normalized rayDir)
        float b = 2 * Vector3.Dot (offset, rayDir);
        float c = Vector3.Dot (offset, offset) - radius * radius;
        float discriminant = b * b - 4 * a * c;

        // No intersections: discriminant < 0
        // 1 intersection: discriminant == 0
        // 2 intersections: discriminant > 0
        if (discriminant >= 0) {
            float t = (-b - Mathf.Sqrt (discriminant)) / (2 * a);
            //float t2 = (-b + Mathf.Sqrt (discriminant)) / (2 * a); // The further away intersection point

            // If t is negative, the intersection was in negative ray direction so ignore it
            if (t >= 0) {
                intersectionPoint = rayOrigin + rayDir * t;
                return true;
            }
        }

        intersectionPoint = Vector3.zero;
        return false;
    }
}