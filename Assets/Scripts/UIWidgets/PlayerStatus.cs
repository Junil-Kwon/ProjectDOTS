using UnityEngine;
using UnityEngine.UI;
using System.Text;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Player Status
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Widget/Player Status")]
public class PlayerStatus : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(PlayerStatus))]
	class PlayerStatusEditor : EditorExtensions {
		PlayerStatus I => target as PlayerStatus;
		public override void OnInspectorGUI() {
			Begin();

			I.ProfileImage     = ObjectField("Profile Image",      I.ProfileImage);
			I.NameTextShadow   = ObjectField("Name Text Shadow",   I.NameTextShadow);
			I.NameText         = ObjectField("Name Text",          I.NameText);
			I.StatusTextShadow = ObjectField("Status Text Shadow", I.StatusTextShadow);
			I.StatusText       = ObjectField("Status Text",        I.StatusText);
			Space();

			I.HealthBorderImage = ObjectField("Health Border Image", I.HealthBorderImage);
			I.HealthBarImage    = ObjectField("Health Bar Image",    I.HealthBarImage);
			I.ShieldBarImage    = ObjectField("Shield Bar Image",    I.ShieldBarImage);
			I.ExcessBarImage    = ObjectField("Excess Bar Image",    I.ExcessBarImage);
			I.EffectBarImage    = ObjectField("Effect Bar Image",    I.EffectBarImage);
			I.EnergyBorderImage = ObjectField("Energy Border Image", I.EnergyBorderImage);
			I.EnergyBarImage    = ObjectField("Energy Bar Image",    I.EnergyBarImage);
			Space();

			I.ReferenceWidth = Slider("Reference Width", I.ReferenceWidth, 1f, 128f);
			Space();

			End();
		}
	}
	#endif



	// Constants

	static readonly int CanvasTiling    = Shader.PropertyToID("_Canvas_Tiling");
	static readonly int CanvasOffset    = Shader.PropertyToID("_Canvas_Offset");
	static readonly int CanvasBaseColor = Shader.PropertyToID("_Canvas_BaseColor");
	static readonly int CanvasMaskColor = Shader.PropertyToID("_Canvas_MaskColor");



	// Fields

	[SerializeField] Image m_ProfileImage;
	[SerializeField] TextMeshProUGUI m_NameTextShadow;
	[SerializeField] TextMeshProUGUI m_NameText;
	[SerializeField] TextMeshProUGUI m_StatusTextShadow;
	[SerializeField] TextMeshProUGUI m_StatusText;
	Canvas m_Profile;
	Color m_ProfileBaseColor;
	Color m_ProfileMaskColor;

	[SerializeField] Image m_HealthBorderImage;
	[SerializeField] Image m_HealthBarImage;
	[SerializeField] Image m_ShieldBarImage;
	[SerializeField] Image m_ExcessBarImage;
	[SerializeField] Image m_EffectBarImage;
	[SerializeField] Image m_EnergyBorderImage;
	[SerializeField] Image m_EnergyBarImage;
	[SerializeField] float m_ReferenceWidth = 1f;
	CharacterStatusBlobData m_StatusBlob;
	CharacterStatusData m_StatusData;



	// Properties

	Image ProfileImage {
		get => m_ProfileImage;
		set => m_ProfileImage = value;
	}
	public Canvas Profile {
		get => m_Profile;
		set {
			if (m_Profile != value) {
				m_Profile = value;
				var hash = new CanvasHash() { Canvas = value, Tick = 0u, };
				var data = DrawManager.GetCanvasData(hash);
				ProfileImage.material.SetVector(CanvasTiling, (Vector2)data.tiling);
				ProfileImage.material.SetVector(CanvasOffset, (Vector2)data.offset);
			}
		}
	}
	public Color ProfileBaseColor {
		get => m_ProfileBaseColor;
		set {
			if (m_ProfileBaseColor != value) {
				m_ProfileBaseColor = value;
				ProfileImage.material.SetColor(CanvasBaseColor, value);
			}
		}
	}
	public Color ProfileMaskColor {
		get => m_ProfileMaskColor;
		set {
			if (m_ProfileMaskColor != value) {
				m_ProfileMaskColor = value;
				ProfileImage.material.SetColor(CanvasMaskColor, value);
			}
		}
	}

	TextMeshProUGUI NameTextShadow {
		get => m_NameTextShadow;
		set => m_NameTextShadow = value;
	}
	TextMeshProUGUI NameText {
		get => m_NameText;
		set => m_NameText = value;
	}
	public string Name {
		get => NameText.text;
		set => NameTextShadow.text = NameText.text = value;
	}

	TextMeshProUGUI StatusTextShadow {
		get => m_StatusTextShadow;
		set => m_StatusTextShadow = value;
	}
	TextMeshProUGUI StatusText {
		get => m_StatusText;
		set => m_StatusText = value;
	}
	public string Status {
		get => StatusText.text;
		set => StatusTextShadow.text = StatusText.text = value;
	}



	Image HealthBorderImage {
		get => m_HealthBorderImage;
		set => m_HealthBorderImage = value;
	}
	Image HealthBarImage {
		get => m_HealthBarImage;
		set => m_HealthBarImage = value;
	}
	Image ShieldBarImage {
		get => m_ShieldBarImage;
		set => m_ShieldBarImage = value;
	}
	Image ExcessBarImage {
		get => m_ExcessBarImage;
		set => m_ExcessBarImage = value;
	}
	Image EffectBarImage {
		get => m_EffectBarImage;
		set => m_EffectBarImage = value;
	}
	Image EnergyBorderImage {
		get => m_EnergyBorderImage;
		set => m_EnergyBorderImage = value;
	}
	Image EnergyBarImage {
		get => m_EnergyBarImage;
		set => m_EnergyBarImage = value;
	}

	float ReferenceWidth {
		get => m_ReferenceWidth;
		set => m_ReferenceWidth = value;
	}

	public CharacterStatusBlobData StatusBlob {
		get => m_StatusBlob;
		set => m_StatusBlob = value;
	}
	public CharacterStatusData StatusData {
		get => m_StatusData;
		set => m_StatusData = value;
	}



	// Methods

	public void SetName(StringBuilder builder) {
		NameTextShadow.SetText(builder);
		NameText.SetText(builder);
	}

	public void SetStatus(StringBuilder builder) {
		StatusTextShadow.SetText(builder);
		StatusText.SetText(builder);
	}



	// Lifecycle

	void LateUpdate() {
		float maxHealth = StatusBlob.MaxHealth + StatusBlob.MaxShield;
		var (health, shield, excess) = StatusData.GetHealthSet(StatusBlob);
		if (0f < excess) excess = maxHealth * (1f - Mathf.Exp(-excess / maxHealth));
		float ratio = ReferenceWidth * 2f / maxHealth;
		float width = Mathf.Max(health + shield + excess, maxHealth) * ratio;

		float healthScale = health * ratio;
		float shieldScale = shield * ratio;
		float excessScale = excess * ratio;
		//float effectScale = 0f;
		float healthPivot = -width * 0.5f + healthScale * 0.5f;
		float shieldPivot = healthPivot + healthScale * 0.5f + shieldScale * 0.5f;
		float excessPivot = shieldPivot + shieldScale * 0.5f + excessScale * 0.5f;
		//float effectPivot = 0f;
	}
}
