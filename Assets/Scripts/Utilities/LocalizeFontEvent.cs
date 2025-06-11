using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using System;

using TMPro;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Localize Font Event
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Localization/Asset/Localize Font Event")]
public class LocalizedFontEvent : LocalizedAssetEvent<TMP_FontAsset, LocalizedFont, UnityEventFont> { }

[Serializable]
public class LocalizedFont : LocalizedAsset<TMP_FontAsset> { }

[Serializable]
public class UnityEventFont : UnityEvent<TMP_FontAsset> { }
