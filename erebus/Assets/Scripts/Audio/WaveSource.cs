using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSource : MonoBehaviour {

    // How long does the full length of data represent in seconds?
    public float PlayRate = 0.15f;
    public int Oversample = 25;
    public int BandCount = 8;
    public int SpectrumResolution = 512;
    public AudioClip Source;
    public bool DrawWave = false;

    private float[] channelSamples;
    private float[] averageSamples;
    private float[] spectrum;
    private float[] bands;
    private float elapsedTime;

    public void Start() {
        spectrum = new float[SpectrumResolution];
        bands = new float[BandCount];
    }

    public void FixedUpdate() {
        if (Source == null) {
            Source = Global.Instance().Audio.BGMClip();
        }
        if (Source == null) {
            return;
        }
        int sampleInCount = (int)Math.Ceiling(Source.channels * Source.frequency * PlayRate);
        int sampleCountPerChannel = sampleInCount / Source.channels;
        int outputSampleCount = sampleCountPerChannel / Oversample;
        int inSamplesPerOut = sampleInCount / outputSampleCount;
        if (channelSamples == null) {
            channelSamples = new float[sampleInCount];
            averageSamples = new float[outputSampleCount];
        }

        int offset = (int)(elapsedTime * (float)Source.frequency);
        while (offset >= Source.samples) {
            elapsedTime -= Source.samples * Source.frequency;
            offset = (int)(elapsedTime * (float)Source.frequency);
        }
        Source.GetData(channelSamples, offset);
        for (int outSample = 0; outSample < outputSampleCount; outSample += 1) {
            float accum = 0.0f;
            for (int i = 0; i < inSamplesPerOut; i += 1) {
                accum += channelSamples[outSample * inSamplesPerOut + i];
            }
            averageSamples[outSample] = accum / (float)inSamplesPerOut;
        }
        elapsedTime += Time.deltaTime;

        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.Blackman);
        int count = 0;
        for (int i = 0; i < BandCount; i += 1) {
            float average = 0.0f;
            int sampleCount = (int)Mathf.Pow(2.0f, i) * 2;
            for (int j = 0; j < sampleCount; j += 1) {
                average += spectrum[count] * (count + 1);
                count += 1;
            }
            average /= sampleCount;
            bands[i] = average;
        }

        if (DrawWave) {
            for (int i = 1; i < outputSampleCount; i += 1) {
                Debug.DrawLine(new Vector3(-5.0f + 10.0f * ((float)(i - 1) / (float)outputSampleCount), averageSamples[i - 1], 0.0f),
                               new Vector3(-5.0f + 10.0f * ((float)(i - 0) / (float)outputSampleCount), averageSamples[i], 0.0f),
                               Color.white);
            }
            for (int i = 1; i < BandCount; i += 1) {
                Debug.DrawLine(new Vector3(i - 1, bands[i - 1], 0), new Vector3(i, bands[i], 0), Color.cyan);
            }
        }
    }

    public float[] GetSamples() {
        return averageSamples;
    }

    public int GetSampleCount() {
        if (Source == null) {
            return 0;
        }
        int sampleInCount = (int)Math.Ceiling(Source.channels * Source.frequency * PlayRate);
        int sampleCountPerChannel = sampleInCount / Source.channels;
        return sampleCountPerChannel / Oversample;
    }

    public float GetLowBand() {
        if (bands == null) return 0.0f;
        return bands[1] * 3.0f;
    }

    public float GetHighBand() {
        if (bands == null) return 0.0f;
        return bands[6] / 1.5f;
    }
}
