using System.Runtime.InteropServices;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public static class UIBridge {

	// Constants

	public struct Property {
		public byte Data;
	}

	public enum Method : byte {
		OpenScreen,
		Back,
		AddText,
		SetTextValue,
		SetTextPosition,
		SetTextDuration,
		SetTextLayer,
		RemoveText,
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct OpenScreen {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public Screen Screen;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct Back {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct AddText {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public FixedString32Bytes Text;
		[FieldOffset(41)] public float3 Position;
		[FieldOffset(53)] public float Duration;
		[FieldOffset(57)] public int Layer;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct AddTextResult {
		[FieldOffset(00)] public FixedBytes16 Data;
		[FieldOffset(00)] public uint TextID;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SetTextValue {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint TextID;
		[FieldOffset(13)] public FixedString32Bytes Text;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SetTextPosition {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint TextID;
		[FieldOffset(13)] public float3 Position;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SetTextDuration {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint TextID;
		[FieldOffset(13)] public float Duration;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SetTextLayer {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint TextID;
		[FieldOffset(13)] public int Layer;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct RemoveText {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint TextID;
	}
}



public static class UIBridgeExtensions {

	// Methods

	public static void OpenScreen(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, Screen screen) {
		var i = new UIBridge.OpenScreen {
			Method = UIBridge.Method.OpenScreen,
			Entity = entity, Screen = screen,
		};
		method.Enqueue(i.Data);
	}

	public static void Back(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity) {
		var i = new UIBridge.Back {
			Method = UIBridge.Method.Back,
			Entity = entity,
		};
		method.Enqueue(i.Data);
	}

	public static void AddText(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, FixedString32Bytes text, float3 position,
		float duration = 1f, int layer = default) {
		var i = new UIBridge.AddText {
			Method = UIBridge.Method.AddText,
			Entity = entity, Text = text, Position = position,
			Duration = duration, Layer = layer,
		};
		method.Enqueue(i.Data);
	}

	public static bool TryGetAddTextResult(
		this NativeHashMap<Entity, FixedBytes16>.ReadOnly result,
		Entity entity, out uint textID) {
		if (result.TryGetValue(entity, out var data)) {
			var o = new UIBridge.AddTextResult { Data = data };
			textID = o.TextID;
			return true;
		}
		textID = default;
		return false;
	}

	public static void SetTextValue(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, uint textID, FixedString32Bytes text) {
		var i = new UIBridge.SetTextValue {
			Method = UIBridge.Method.SetTextValue,
			Entity = entity, TextID = textID, Text = text,
		};
		method.Enqueue(i.Data);
	}

	public static void SetTextPosition(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, uint textID, float3 position) {
		var i = new UIBridge.SetTextPosition {
			Method = UIBridge.Method.SetTextPosition,
			Entity = entity, TextID = textID, Position = position,
		};
		method.Enqueue(i.Data);
	}

	public static void SetTextDuration(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, uint textID, float duration) {
		var i = new UIBridge.SetTextDuration {
			Method = UIBridge.Method.SetTextDuration,
			Entity = entity, TextID = textID, Duration = duration,
		};
		method.Enqueue(i.Data);
	}

	public static void SetTextLayer(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, uint textID, int layer) {
		var i = new UIBridge.SetTextLayer {
			Method = UIBridge.Method.SetTextLayer,
			Entity = entity, TextID = textID, Layer = layer,
		};
		method.Enqueue(i.Data);
	}

	public static void RemoveText(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, uint textID) {
		var i = new UIBridge.RemoveText {
			Method = UIBridge.Method.RemoveText,
			Entity = entity, TextID = textID,
		};
		method.Enqueue(i.Data);
	}
}




// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class UIBridgeSystem : SystemBase {
	NativeArray<UIBridge.Property> Property;
	NativeQueue<FixedBytes62> Method;
	NativeHashMap<Entity, FixedBytes16> Result;

	public struct Singleton : IComponentData {
		public NativeArray<UIBridge.Property>.ReadOnly Property;
		public NativeQueue<FixedBytes62>.ParallelWriter Method;
		public NativeHashMap<Entity, FixedBytes16>.ReadOnly Result;
	}

	protected override void OnCreate() {
		EntityManager.CreateEntity(ComponentType.ReadOnly<Singleton>());
		SystemAPI.SetSingleton(new Singleton {
			Property = (Property = new(1, Allocator.Persistent)).AsReadOnly(),
			Method = (Method = new(Allocator.Persistent)).AsParallelWriter(),
			Result = (Result = new(64, Allocator.Persistent)).AsReadOnly(),
		});
	}

	protected override void OnUpdate() {
		Dependency.Complete();
		Property[0] = new UIBridge.Property {
		};
		Result.Clear();
		while (Method.TryDequeue(out var data)) {
			switch ((UIBridge.Method)data.offset0000.byte0000) {
				case UIBridge.Method.OpenScreen: {
					var i = new UIBridge.OpenScreen { Data = data };
					UIManager.OpenScreen(i.Screen);
				} break;
				case UIBridge.Method.Back: {
					var i = new UIBridge.Back { Data = data };
					UIManager.Back();
				} break;
				case UIBridge.Method.AddText: {
					var i = new UIBridge.AddText { Data = data };
					uint textID = UIManager.AddText(i.Text.ToString(), i.Position, i.Duration);
					var o = new UIBridge.AddTextResult { TextID = textID };
					Result.TryAdd(i.Entity, o.Data);
				} break;
				case UIBridge.Method.SetTextValue: {
					var i = new UIBridge.SetTextValue { Data = data };
					UIManager.SetTextValue(i.TextID, i.Text.ToString());
				} break;
				case UIBridge.Method.SetTextPosition: {
					var i = new UIBridge.SetTextPosition { Data = data };
					UIManager.SetTextPosition(i.TextID, i.Position);
				} break;
				case UIBridge.Method.SetTextDuration: {
					var i = new UIBridge.SetTextDuration { Data = data };
					UIManager.SetTextDuration(i.TextID, i.Duration);
				} break;
				case UIBridge.Method.SetTextLayer: {
					var i = new UIBridge.SetTextLayer { Data = data };
					UIManager.SetTextLayer(i.TextID, i.Layer);
				} break;
				case UIBridge.Method.RemoveText: {
					var i = new UIBridge.RemoveText { Data = data };
					UIManager.RemoveText(i.TextID);
				} break;
			}
		}
	}

	protected override void OnDestroy() {
		Property.Dispose();
		Method.Dispose();
		Result.Dispose();
	}
}
