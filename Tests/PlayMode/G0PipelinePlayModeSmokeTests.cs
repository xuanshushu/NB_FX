using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NBFX.Baseline.Tests
{
    public sealed class G0PipelinePlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator RunnerAdvancesOneFrame()
        {
            var startFrame = Time.frameCount;
            yield return null;
            Assert.That(Time.frameCount, Is.GreaterThan(startFrame));
        }
    }
}
