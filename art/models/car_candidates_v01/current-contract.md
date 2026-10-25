# Current player car contract - static YAML audit

2026-10-25. Read saved Unity YAML only. Unity, Play, import and geometry remeasurement were not performed. No Assets files were modified.

## Asset trace

- Physics: F1/Assets/Prefabs/Race.prefab, GUID 356a5e3b096c28641af798c7539095f8.
- Inherited model: F1/Assets/Model/kenney_car-kit/Models/FBX format/race.fbx, GUID 1a42b610839ba2c47aa6de434f97ab82. Some inherited renderers are disabled by scene overrides.
- Visual: F1/Assets/Art/CoastalFormula/FormulaGameplayVisual.prefab, GUID ee48b362edabaa245b556832a047788e.
- Visual mesh: F1/Assets/Art/CoastalFormula/CoastalFormula.fbx, GUID c098ca4be9a433845a9dfe6306945ea0.

## Common contract in both scenes

- Unity coordinates: X right, Y up, car-local Z forward. Distances are meters.
- Race scale 0.7; Visual local scale 1.4285712; combined scale 0.99999984.
- Visual local position (0,0.1285497,-0.19642855); local quaternion (0,0,0,1).
- Preserve the model origin and individual wheel pivots. Do not recenter from a bounding box. Ground-plane meaning derives from the previous source measurement, not a new runtime test.
- Scene BoxCollider local size (2.7999997,0.9285714,5.407142); center (0,0.5928354,0).
- Scaled collider size in car axes (1.95999979,0.64999998,3.7849994); center (0,0.41498478,0). These are not world-axis AABB dimensions.
- Race.prefab default collider size (1.3,0.60245156,2.56) is replaced by the scene override.

| Wheel | Unity local pivot | Radius |
|---|---|---|
| wheel_front_left | (-0.79,0.35,1.2) | 0.35 |
| wheel_front_right | (0.79,0.35,1.2) | 0.35 |
| wheel_rear_left | (-0.79,0.375,-1.1) | 0.375 |
| wheel_rear_right | (0.79,0.375,-1.1) | 0.375 |

Each wheel is a separate mesh/transform directly under the Visual root, with identity rotation and unit scale. FormulaVehicleVisual.cs:82-84 applies radius multiplied by lossyScale.y, local X spin and front-wheel Y steering.

## Current saved scene pose

| Scene | Race position | Quaternion xyzw |
|---|---|---|
| Proto_CoastCourse | (76.092674,-0.6923548,61.02211) | (0,-0.7071068,0,0.7071068) |
| Map_BayLoop | (-14.002785,3.7981982,-141) | (0,0.7071068,0,0.7071068) |

## Materials

Both scenes override all ten mesh materials. Driver uses F1/Assets/Scenes/Proto_CoastCourse_Mats/FormulaRestored.mat (GUID 728cbd0213ae5124f80a88aaa265db05). The other nine meshes use FormulaWorn.mat in that same folder (GUID 82a77a6a607e3074fbddb0ecd729d15e).

The worn texture is F1/Assets/Art/CoastalFormula/FormulaAtlasWorn.png (GUID 1a64e36d751e11a45869fafa0a35053a). JSON materials contains exact node-to-renderer-to-material mappings. Raw imported FBX materials are not the current scene material assignment. Runtime restoration swaps were not tested.

## Comparison and limitations

- art/models/car_v01/SPEC.md agrees with the current local Visual transform and Unity wheel centers; that candidate is not the current scene model.
- tools/asset-gen/check_car.py:25-33 uses GLB wheel X signs opposite to Unity: left +0.79/right -0.79 in GLB, left -0.79/right +0.79 in Unity. Preserve the existing FBX export convention's X flip.
- art/_review/2026-10-23-car/current-car-measure.json has a stale Coast saved position (1.2,8.275001,5).
- Current CoastalFormula.fbx, FormulaGameplayVisual.prefab and Race.prefab SHA256 hashes match the 10/23 measurement sources. Coast scene hash differs. JSON old_hash_matches records this evidence.
- Previous measured dimensions W1.958909035 x L3.784999967 x H1.072000027m and 4872 triangles were not remeasured in this audit. Identical source hashes support reusing those earlier measurements, with that limitation.
- FBX importer: globalScale=1, useFileUnits=1, useFileScale=1, bakeAxisConversion=0.
- Ground contact, actual rendering, wheel motion, physics, restoration and shader behavior require an editor-token holder's runtime verification.
- Verification: JSON parses; 13 transforms, 10 material mappings per scene, two relevant prefab instances per scene. Both scenes' material mappings match.

## Raw YAML evidence

### F1/Assets/Scenes/Proto_CoastCourse.unity

```yaml
--- !u!1001 &374189954
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {fileID: 0}
    m_Modifications:
    - target: {fileID: 333221348542454333, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Size.x
      value: 2.7999997
      objectReference: {fileID: 0}
    - target: {fileID: 333221348542454333, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Size.y
      value: 0.9285714
      objectReference: {fileID: 0}
    - target: {fileID: 333221348542454333, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Size.z
      value: 5.407142
      objectReference: {fileID: 0}
    - target: {fileID: 333221348542454333, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Center.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 333221348542454333, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Center.y
      value: 0.5928354
      objectReference: {fileID: 0}
    - target: {fileID: 333221348542454333, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Center.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 971973449158092065, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Enabled
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 993988331824062178, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Enabled
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 1811446672057895780, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Enabled
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: airDrag
      value: 0.15
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: steerLag
      value: 0.05
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: accelTime
      value: 2.8
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: brakeDecel
      value: 13
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: maxSpeedKmh
      value: 96
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: throttleLag
      value: 0.03
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: fallRespawnY
      value: -8
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: driftYawRateDeg
      value: 130
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: driftMaxSpeedKmh
      value: 100
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: driftYawResponse
      value: 10
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: maxTurnDegPerSec
      value: 130
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: driftVelocityAlign
      value: 1.6
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: safePointAboveFall
      value: 4
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: centerOfMassOffset.y
      value: -0.1
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: driftLongitudinalDrag
      value: 0.15
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: turnResponseBySpeed.m_Curve.Array.data[1].value
      value: 0.5
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: brakeStrengthBySpeed.m_Curve.Array.data[0].value
      value: 0.85
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: brakeStrengthBySpeed.m_Curve.Array.data[1].value
      value: 1
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: turnResponseBySpeed.m_Curve.Array.data[1].inSlope
      value: -0.5
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: brakeStrengthBySpeed.m_Curve.Array.data[1].inSlope
      value: 0.14999998
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: turnResponseBySpeed.m_Curve.Array.data[0].outSlope
      value: -0.5
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: brakeStrengthBySpeed.m_Curve.Array.data[0].outSlope
      value: 0.14999998
      objectReference: {fileID: 0}
    - target: {fileID: 5522889733767891348, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Enabled
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 6717217228555201927, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Enabled
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 8269071862197276808, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Enabled
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 8380733253417130255, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Name
      value: Race
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalPosition.x
      value: 76.092674
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalPosition.y
      value: -0.6923548
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalPosition.z
      value: 61.02211
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalRotation.w
      value: 0.7071068
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalRotation.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalRotation.y
      value: -0.7071068
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalRotation.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalEulerAnglesHint.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalEulerAnglesHint.y
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalEulerAnglesHint.z
      value: 0
      objectReference: {fileID: 0}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects:
    - targetCorrespondingSourceObject: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      insertIndex: -1
      addedObject: {fileID: 444362462}
    m_AddedComponents: []
  m_SourcePrefab: {fileID: 100100000, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
```

### F1/Assets/Scenes/Proto_CoastCourse.unity

```yaml
--- !u!1001 &444362461
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {fileID: 374189956}
    m_Modifications:
    - target: {fileID: 19627223685778801, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 342297798963348990, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 1060356561120744745, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_Name
      value: FormulaGameplayVisual
      objectReference: {fileID: 0}
    - target: {fileID: 1322134594043405918, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 1566820398963172592, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 2358247056209823478, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalScale.x
      value: 1.4285712
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalScale.y
      value: 1.4285712
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalScale.z
      value: 1.4285712
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalPosition.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalPosition.y
      value: 0.1285497
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalPosition.z
      value: -0.19642855
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalRotation.w
      value: 1
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalRotation.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalRotation.y
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalRotation.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalEulerAnglesHint.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalEulerAnglesHint.y
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalEulerAnglesHint.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 5137917694003315628, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 6825235188804354702, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 7754467182532268283, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 728cbd0213ae5124f80a88aaa265db05, type: 2}
    - target: {fileID: 7841445821114220533, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 8733826432411567053, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {fileID: 100100000, guid: ee48b362edabaa245b556832a047788e, type: 3}
```

### F1/Assets/Scenes/Map_BayLoop.unity

```yaml
--- !u!1001 &374189954
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {fileID: 0}
    m_Modifications:
    - target: {fileID: 333221348542454333, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Size.x
      value: 2.7999997
      objectReference: {fileID: 0}
    - target: {fileID: 333221348542454333, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Size.y
      value: 0.9285714
      objectReference: {fileID: 0}
    - target: {fileID: 333221348542454333, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Size.z
      value: 5.407142
      objectReference: {fileID: 0}
    - target: {fileID: 333221348542454333, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Center.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 333221348542454333, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Center.y
      value: 0.5928354
      objectReference: {fileID: 0}
    - target: {fileID: 333221348542454333, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Center.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 971973449158092065, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Enabled
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 993988331824062178, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Enabled
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 1811446672057895780, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Enabled
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: airDrag
      value: 0.15
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: steerLag
      value: 0.05
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: accelTime
      value: 2.8
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: brakeDecel
      value: 13
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: maxSpeedKmh
      value: 96
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: throttleLag
      value: 0.03
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: fallRespawnY
      value: -3
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: driftYawRateDeg
      value: 130
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: driftMaxSpeedKmh
      value: 100
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: driftYawResponse
      value: 10
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: maxTurnDegPerSec
      value: 130
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: driftVelocityAlign
      value: 1.6
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: safePointAboveFall
      value: 4
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: centerOfMassOffset.y
      value: -0.1
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: driftLongitudinalDrag
      value: 0.15
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: turnResponseBySpeed.m_Curve.Array.data[1].value
      value: 0.5
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: brakeStrengthBySpeed.m_Curve.Array.data[0].value
      value: 0.85
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: brakeStrengthBySpeed.m_Curve.Array.data[1].value
      value: 1
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: turnResponseBySpeed.m_Curve.Array.data[1].inSlope
      value: -0.5
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: brakeStrengthBySpeed.m_Curve.Array.data[1].inSlope
      value: 0.14999998
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: turnResponseBySpeed.m_Curve.Array.data[0].outSlope
      value: -0.5
      objectReference: {fileID: 0}
    - target: {fileID: 5094000627798262811, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: brakeStrengthBySpeed.m_Curve.Array.data[0].outSlope
      value: 0.14999998
      objectReference: {fileID: 0}
    - target: {fileID: 5522889733767891348, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Enabled
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 6717217228555201927, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Enabled
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 8269071862197276808, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Enabled
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 8380733253417130255, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_Name
      value: Race
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalPosition.x
      value: -14.002785
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalPosition.y
      value: 3.7981982
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalPosition.z
      value: -141
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalRotation.w
      value: 0.7071068
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalRotation.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalRotation.y
      value: 0.7071068
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalRotation.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalEulerAnglesHint.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalEulerAnglesHint.y
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      propertyPath: m_LocalEulerAnglesHint.z
      value: 0
      objectReference: {fileID: 0}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects:
    - targetCorrespondingSourceObject: {fileID: 9152872249828246453, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
      insertIndex: -1
      addedObject: {fileID: 444362462}
    m_AddedComponents: []
  m_SourcePrefab: {fileID: 100100000, guid: 356a5e3b096c28641af798c7539095f8, type: 3}
```

### F1/Assets/Scenes/Map_BayLoop.unity

```yaml
--- !u!1001 &444362461
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {fileID: 374189956}
    m_Modifications:
    - target: {fileID: 19627223685778801, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 342297798963348990, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 1060356561120744745, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_Name
      value: FormulaGameplayVisual
      objectReference: {fileID: 0}
    - target: {fileID: 1322134594043405918, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 1566820398963172592, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 2358247056209823478, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalScale.x
      value: 1.4285712
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalScale.y
      value: 1.4285712
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalScale.z
      value: 1.4285712
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalPosition.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalPosition.y
      value: 0.1285497
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalPosition.z
      value: -0.19642855
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalRotation.w
      value: 1
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalRotation.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalRotation.y
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalRotation.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalEulerAnglesHint.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalEulerAnglesHint.y
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 2749762494709477545, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: m_LocalEulerAnglesHint.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 5137917694003315628, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 6825235188804354702, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 7754467182532268283, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 728cbd0213ae5124f80a88aaa265db05, type: 2}
    - target: {fileID: 7841445821114220533, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    - target: {fileID: 8733826432411567053, guid: ee48b362edabaa245b556832a047788e, type: 3}
      propertyPath: 'm_Materials.Array.data[0]'
      value: 
      objectReference: {fileID: 2100000, guid: 82a77a6a607e3074fbddb0ecd729d15e, type: 2}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {fileID: 100100000, guid: ee48b362edabaa245b556832a047788e, type: 3}
```

### F1/Assets/Prefabs/Race.prefab

```yaml
--- !u!1001 &8687216822286967902
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {fileID: 0}
    m_Modifications:
    - target: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      propertyPath: m_LocalScale.x
      value: 0.7
      objectReference: {fileID: 0}
    - target: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      propertyPath: m_LocalScale.y
      value: 0.7
      objectReference: {fileID: 0}
    - target: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      propertyPath: m_LocalScale.z
      value: 0.7
      objectReference: {fileID: 0}
    - target: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      propertyPath: m_LocalPosition.x
      value: 48
      objectReference: {fileID: 0}
    - target: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      propertyPath: m_LocalPosition.y
      value: 0.16929999
      objectReference: {fileID: 0}
    - target: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      propertyPath: m_LocalPosition.z
      value: 36
      objectReference: {fileID: 0}
    - target: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      propertyPath: m_LocalRotation.w
      value: 1
      objectReference: {fileID: 0}
    - target: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      propertyPath: m_LocalRotation.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      propertyPath: m_LocalRotation.y
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      propertyPath: m_LocalRotation.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      propertyPath: m_LocalEulerAnglesHint.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      propertyPath: m_LocalEulerAnglesHint.y
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      propertyPath: m_LocalEulerAnglesHint.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 919132149155446097, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      propertyPath: m_Name
      value: Race
      objectReference: {fileID: 0}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects:
    - targetCorrespondingSourceObject: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      insertIndex: -1
      addedObject: {fileID: 5907064711085574952}
    - targetCorrespondingSourceObject: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      insertIndex: -1
      addedObject: {fileID: 8353648542957339511}
    - targetCorrespondingSourceObject: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      insertIndex: -1
      addedObject: {fileID: 5399186709025801423}
    - targetCorrespondingSourceObject: {fileID: -8679921383154817045, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      insertIndex: -1
      addedObject: {fileID: 7220697468963436059}
    m_AddedComponents:
    - targetCorrespondingSourceObject: {fileID: 919132149155446097, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      insertIndex: -1
      addedObject: {fileID: 2449147657015966861}
    - targetCorrespondingSourceObject: {fileID: 919132149155446097, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      insertIndex: -1
      addedObject: {fileID: 333221348542454333}
    - targetCorrespondingSourceObject: {fileID: 919132149155446097, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      insertIndex: -1
      addedObject: {fileID: 5094000627798262811}
    - targetCorrespondingSourceObject: {fileID: 919132149155446097, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      insertIndex: -1
      addedObject: {fileID: 6246249705684340049}
    - targetCorrespondingSourceObject: {fileID: 919132149155446097, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      insertIndex: -1
      addedObject: {fileID: 4634277122682247217}
    - targetCorrespondingSourceObject: {fileID: 919132149155446097, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      insertIndex: -1
      addedObject: {fileID: 8586498683429853758}
    - targetCorrespondingSourceObject: {fileID: 919132149155446097, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      insertIndex: -1
      addedObject: {fileID: 3897670325595319276}
    - targetCorrespondingSourceObject: {fileID: 919132149155446097, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      insertIndex: -1
      addedObject: {fileID: 934473438500580793}
    - targetCorrespondingSourceObject: {fileID: 919132149155446097, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      insertIndex: -1
      addedObject: {fileID: 3136773584004004698}
    - targetCorrespondingSourceObject: {fileID: 919132149155446097, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      insertIndex: -1
      addedObject: {fileID: 8269071862197276808}
    - targetCorrespondingSourceObject: {fileID: 919132149155446097, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
      insertIndex: -1
      addedObject: {fileID: 7228287588265654062}
  m_SourcePrefab: {fileID: 100100000, guid: 1a42b610839ba2c47aa6de434f97ab82, type: 3}
```

### F1/Assets/Prefabs/Race.prefab

```yaml
--- !u!65 &333221348542454333
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 8380733253417130255}
  m_Material: {fileID: 13400000, guid: af714502d0670f74db16a371dd2761d8, type: 2}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 3
  m_Size: {x: 1.3, y: 0.60245156, z: 2.56}
  m_Center: {x: -3.8805202e-16, y: 0.42977548, z: -2.7674121e-17}
```
