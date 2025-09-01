using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Image Animation Binder
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Image Animation Binder")]
[RequireComponent(typeof(Image), typeof(SpriteRenderer), typeof(Animator))]
public class ImageAnimationBinder : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(ImageAnimationBinder))]
	class ImageAnimationBinderEditor : EditorExtensions {
		ImageAnimationBinder I => target as ImageAnimationBinder;
		public override void OnInspectorGUI() {
			Begin();
			End();
		}
	}
	#endif



	// Fields

	Image m_Image;
	SpriteRenderer m_Renderer; 



	// Properties

	Image Image => !m_Image ?
		m_Image = TryGetComponent(out Image image) ? image : default :
		m_Image;

	SpriteRenderer Renderer => !m_Renderer ?
		m_Renderer = TryGetComponent(out SpriteRenderer renderer) ? renderer : default :
		m_Renderer;



	// Lifecycle

	void LateUpdate() {
		var sprite = Renderer.sprite;
		if (sprite) {
			Image.sprite = sprite;
			Image.SetNativeSize();
			float x = Image.sprite.pivot.x / Image.sprite.rect.width;
			float y = Image.sprite.pivot.y / Image.sprite.rect.height;
			Image.rectTransform.pivot = new Vector2(x, y);
		}
	}
}
