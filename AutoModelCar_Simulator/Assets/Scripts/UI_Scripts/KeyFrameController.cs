﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;

public class KeyFrameController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform canvas;
    private RectTransform self;
    private AnimationCurve reference;
    public RectTransform l_tangent_img;
    public RectTransform r_tangent_img;
    public Keyframe key { get {return reference.keys[ac_index];} }
    private int ac_index;
    private bool isPressed = false;
    private float min_x, max_x, min_y, max_y;
    private float delta_x, delta_y;

    public float leftTangent;
    public float rightTangent;
    public AnimationUtility.TangentMode leftTangent_mode, rightTangent_mode;
    
    void Start()
    {
        self = GetComponent<RectTransform>();
        self.anchoredPosition3D = Vector3.zero;
        leftTangent_mode = rightTangent_mode = AnimationUtility.TangentMode.Free;
    }

    public void init(RectTransform canvas, AnimationCurve reference, int index, float min_x, float max_x, float min_y, float max_y) {
        transform.SetParent(canvas.transform, false);
        this.canvas = canvas;
        this.reference = reference;
        this.ac_index = index;
        this.min_x = min_x;
        this.max_x = max_x;
        this.min_y = min_y;
        this.max_y = max_y;
        this.delta_x = Mathf.Abs(max_x-min_x);
        this.delta_y = Mathf.Abs(max_y-min_y);
    }

    public void updateAxesIntervals(float min_x, float max_x, float min_y, float max_y) {
        this.min_x = min_x;
        this.max_x = max_x;
        this.min_y = min_y;
        this.max_y = max_y;
        this.delta_x = Mathf.Abs(max_x-min_x);
        this.delta_y = Mathf.Abs(max_y-min_y);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(!KeyFrameEditor.selected && ac_index<reference.keys.Length-1 && ac_index>0) {
            isPressed = true;
            KeyFrameEditor.selected = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        KeyFrameEditor.selected = false;
    }
    
    void Update()
    {
        if(AnimationUtility.GetKeyLeftTangentMode(reference, ac_index)==AnimationUtility.TangentMode.Free) {
            l_tangent_img.gameObject.SetActive(true);
            l_tangent_img.anchoredPosition = new Vector2(10, 10*leftTangent);
        }
        else {
            l_tangent_img.gameObject.SetActive(false);
        }

        if(AnimationUtility.GetKeyRightTangentMode(reference, ac_index)==AnimationUtility.TangentMode.Free) {
            r_tangent_img.gameObject.SetActive(true);
            r_tangent_img.anchoredPosition = new Vector2(-10, -10*rightTangent);
        }
        else {
            l_tangent_img.gameObject.SetActive(false);
        }


        if(isPressed && Input.GetMouseButton(0)) {
            //Set the keyframe of keyframe editing window to the one just clicked
            KeyFrameEditor.keyframe = this;
            Globals.Instance.kfEditor.gameObject.SetActive(true);

            Vector3 offset;
            if(gameObject.layer == 9) offset = Globals.Instance.UI_GRAPH_CAMERA.WorldToScreenPoint(canvas.position); //Layer 9 is GRAPH_UI, requires special treatment
            else offset = canvas.position;

            Vector3 np = Input.mousePosition - offset;

            if(np.y > 0 && np.y < canvas.rect.height && np.x > Mathf.Max(0, reference.keys[ac_index-1].time/delta_x*canvas.rect.width) && np.x < Mathf.Min(canvas.rect.width, reference.keys[ac_index+1].time/delta_x*canvas.rect.width)) {
                self.anchoredPosition = np;

                forceUpdate();
            }
        }
        else {
            self.anchoredPosition3D = toCanvasSpace(reference.keys[ac_index].time, reference.keys[ac_index].value);
        }
    }

    public void forceUpdate() {
        Keyframe[] keys = reference.keys; // Get a copy of the array
        keys[ac_index].time = delta_x * self.anchoredPosition.x / canvas.rect.width;
        keys[ac_index].value = delta_y * self.anchoredPosition.y / canvas.rect.height;
        keys[ac_index].inTangent = leftTangent;
        keys[ac_index].outTangent = rightTangent;
        reference.keys = keys; // assign the array back to the property

        self.anchoredPosition3D = toCanvasSpace(reference.keys[ac_index].time, reference.keys[ac_index].value);
    }

    Vector3 toCanvasSpace(float x, float y) {
        return new Vector3(canvas.rect.width*(x)/delta_x, canvas.rect.height*(y)/delta_y, 0.0f);
    }
}
