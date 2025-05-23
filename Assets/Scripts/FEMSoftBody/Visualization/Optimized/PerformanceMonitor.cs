using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PerformanceMonitor
{
    private FEMPerformanceSettings settings;
    private Queue<float> frameTimeHistory;
    private float lastFrameTime;
    private float currentFPS;
    private System.Diagnostics.Stopwatch frameTimer;

    public PerformanceMonitor(FEMPerformanceSettings settings)
    {
        this.settings = settings;
        frameTimeHistory = new Queue<float>();
        frameTimer = new System.Diagnostics.Stopwatch();
        currentFPS = 60f;
    }

    public void Update()
    {
        // ¼ÆËãÆ½¾ùFPS
        if (frameTimeHistory.Count >= settings.performanceWindowSize)
        {
            frameTimeHistory.Dequeue();
        }

        frameTimeHistory.Enqueue(Time.unscaledDeltaTime);

        float averageFrameTime = frameTimeHistory.Average();
        currentFPS = 1f / averageFrameTime;
    }

    public void BeginFrame()
    {
        frameTimer.Restart();
    }

    public void EndFrame()
    {
        frameTimer.Stop();
        lastFrameTime = (float)frameTimer.Elapsed.TotalSeconds;
    }

    public float GetCurrentFPS()
    {
        return currentFPS;
    }

    public bool IsPerformanceGood()
    {
        return currentFPS >= settings.targetFPS;
    }

    public bool IsPerformanceCritical()
    {
        return currentFPS < settings.minimumFPS;
    }

    public float GetFrameTimeMs()
    {
        return lastFrameTime * 1000f;
    }
}