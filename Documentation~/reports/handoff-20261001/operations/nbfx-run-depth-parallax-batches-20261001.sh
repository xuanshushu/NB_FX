#!/bin/bash
set -u
export UNITY_BURST_DISABLE_COMPILATION=1
unset NBFX_RT_DIAGNOSTIC
export NBFX_MESH_EVIDENCE_DIR=/tmp/nbfx-g4-depth-parallax-batch1-20261001
/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /tmp/NBFXG2DissolveMaskProbe-20260928 -runTests -testPlatform EditMode -testFilter 'NBFX.Baseline.Tests.G2TextureNoiseExtractionTests;NBFX.Baseline.Tests.G4GraphGuiStorageTests;NBFX.Baseline.Tests.G4GraphGuiMainTextureTests;NBFX.Baseline.Tests.G4GraphMeshParityTests;NBFX.Baseline.Tests.G4GraphAdvancedUVTests;NBFX.Baseline.Tests.G4GraphFeatureUVTests' -testResults /tmp/nbfx-g4-depth-parallax-batch1-20261001.xml -logFile /tmp/nbfx-g4-depth-parallax-batch1-20261001.log
code=$?
echo NBFX_BATCH_RESULT batch=1 exit=$code
if [ "$code" -ne 0 ] && [ "$code" -ne 2 ]; then exit "$code"; fi
export NBFX_MESH_EVIDENCE_DIR=/tmp/nbfx-g4-depth-parallax-batch2-20261001
/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /tmp/NBFXG2DissolveMaskProbe-20260928 -runTests -testPlatform EditMode -testFilter 'NBFX.Baseline.Tests.G4GraphBlendClipTests;NBFX.Baseline.Tests.G4GraphDepthDecalTests;NBFX.Baseline.Tests.G4GraphDepthShadowTests;NBFX.Baseline.Tests.G4GraphMatCapTests;NBFX.Baseline.Tests.G4GraphNormalMapTests' -testResults /tmp/nbfx-g4-depth-parallax-batch2-20261001.xml -logFile /tmp/nbfx-g4-depth-parallax-batch2-20261001.log
code=$?
echo NBFX_BATCH_RESULT batch=2 exit=$code
if [ "$code" -ne 0 ] && [ "$code" -ne 2 ]; then exit "$code"; fi
export NBFX_MESH_EVIDENCE_DIR=/tmp/nbfx-g4-depth-parallax-batch3-20261001
/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /tmp/NBFXG2DissolveMaskProbe-20260928 -runTests -testPlatform EditMode -testFilter 'NBFX.Baseline.Tests.G4GraphLightingTests;NBFX.Baseline.Tests.G4GraphParallaxTests;NBFX.Baseline.Tests.G4GraphSixWayTests;NBFX.Baseline.Tests.G4GraphPNoiseTests' -testResults /tmp/nbfx-g4-depth-parallax-batch3-20261001.xml -logFile /tmp/nbfx-g4-depth-parallax-batch3-20261001.log
code=$?
echo NBFX_BATCH_RESULT batch=3 exit=$code
if [ "$code" -ne 0 ] && [ "$code" -ne 2 ]; then exit "$code"; fi
export NBFX_MESH_EVIDENCE_DIR=/tmp/nbfx-g4-depth-parallax-batch4-20261001
/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /tmp/NBFXG2DissolveMaskProbe-20260928 -runTests -testPlatform EditMode -testFilter 'NBFX.Baseline.Tests.G4GraphRefractionTests;NBFX.Baseline.Tests.G4GraphPNoiseScreenTests;NBFX.Baseline.Tests.G4GraphRenderStateTests;NBFX.Baseline.Tests.G4GraphSamplingTests;NBFX.Baseline.Tests.G4GraphScreenNoiseTests;NBFX.Baseline.Tests.G4GraphVertexOffsetTests' -testResults /tmp/nbfx-g4-depth-parallax-batch4-20261001.xml -logFile /tmp/nbfx-g4-depth-parallax-batch4-20261001.log
code=$?
echo NBFX_BATCH_RESULT batch=4 exit=$code
if [ "$code" -ne 0 ] && [ "$code" -ne 2 ]; then exit "$code"; fi
export NBFX_MESH_EVIDENCE_DIR=/tmp/nbfx-g4-depth-parallax-batch5-20261001
/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /tmp/NBFXG2DissolveMaskProbe-20260928 -runTests -testPlatform EditMode -testFilter NBFX.Baseline.Tests.G4GraphTextureNoiseTests -testResults /tmp/nbfx-g4-depth-parallax-batch5-20261001.xml -logFile /tmp/nbfx-g4-depth-parallax-batch5-20261001.log
code=$?
echo NBFX_BATCH_RESULT batch=5 exit=$code
if [ "$code" -ne 0 ] && [ "$code" -ne 2 ]; then exit "$code"; fi
