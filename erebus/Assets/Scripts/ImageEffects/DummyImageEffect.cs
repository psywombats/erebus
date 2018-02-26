﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

[RequireComponent(typeof(Camera))]
public class DummyImageEffect : MonoBehaviour {

    public void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(source, destination);
    }
}