using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public abstract class SusadminReflector {
	protected static FieldInfo GetFieldImpl(Type type, string name, bool isPublic = false, BindingFlags bindingFlags = BindingFlags.Instance) {
		FieldInfo result;
		while (type != null && type != typeof(object)) {
			result = type.GetField(name, (isPublic ? BindingFlags.Public : BindingFlags.NonPublic) | bindingFlags);
			if (result != null) return result;
			result = type.GetField("<" + name + ">k__BackingField", BindingFlags.NonPublic | bindingFlags);
			if (result != null) return result;
			type = type.BaseType;
		}
		throw new Exception(string.Format("unable to find field {0}", name));
	}

	public static SusadminReflector CreateReflector(KMBombModule module) {
		try {
			switch (module.ModuleType) {
				case "sysadmin": return new SysadminSusadminReflector(module);
				case "WhosOnFirst": return new WhosOnFirstSusadminReflector(module);
				case "MinesweeperModule": return new MinesweeperSusadminReflector(module);
				case "SimonV2": return new SimonStatesSusadminReflector(module);
				case "TwoBits": return new TwoBitsSusadminReflector(module);
				case "logicGates": return new LogicGatesSusadminReflector(module);
				case "SeaShells": return new SeaShellsSusadminReflector(module);
				case "ColoredSwitchesModule": return new ColoredSwitchesSusadminReflector(module);
				case "DoubleOhModule": return new DoubleOhSusadminReflector(module);
				case "CursedDoubleOhModule": return new DoubleOhSusadminReflector(module);
				case "NotMorseCode": return new NotMorseCodeSusadminReflector(module);
				case "ColoredSquaresModule": return new ColoredSquaresSusadminReflector(module, "ColoredSquaresModule");
				case "DecoloredSquaresModule": return new ColoredSquaresSusadminReflector(module, "DecoloredSquaresModule");
				case "UncoloredSquaresModule": return new ColoredSquaresSusadminReflector(module, "UncoloredSquaresModule");
				case "DiscoloredSquaresModule": return new ColoredSquaresSusadminReflector(module, "DiscoloredSquaresModule");
				case "3dTunnels": return new ThreeDTunnelsSusadminReflector(module);
				case "spwiz3DMaze": return new ThreeDMazeSusadminReflector(module);
				case "ThirdBase": return new ThirdBaseSusadminReflector(module);
				case "nonverbalSimon": return new NonverbalSimonSusadminReflector(module);
				default: return null;
			}
		} catch (Exception error) {
			Debug.LogFormat("<SUSadmin> Unable to create reflector for module {0}: {1}", module.ModuleType, error.Message);
			return null;
		}
	}
	private bool exception = false;
	private bool strike = false;
	private string moduleId;
	protected SusadminReflector(string moduleId) { this.moduleId = moduleId; }
	protected abstract bool Exec();
	public bool ShouldStrike() {
		if (strike) return true;
		if (exception) return strike;
		try {
			strike = Exec();
			return strike;
		} catch (Exception error) {
			if (exception) return strike;
			exception = true;
			Debug.LogFormat("<SUSadmin> Unable to reflect module {0}: {1}", moduleId, error.Message);
			return strike;
		}
	}
}

public class SysadminSusadminReflector : SusadminReflector {
	private Component comp;
	private PropertyInfo fldFixedErrorCodes;
	private FieldInfo fldAllocationsCount;
	public SysadminSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		comp = module.GetComponent("SysadminModule");
		Type type = comp.GetType();
		fldFixedErrorCodes = type.GetProperty("fixedErrorCodes", BindingFlags.Public | BindingFlags.Instance);
		fldAllocationsCount = GetFieldImpl(type, "allocationsCount");
	}
	protected override bool Exec() {
		int? allocationsCount = fldAllocationsCount.GetValue(comp) as int?;
		if (allocationsCount == null) throw new Exception("allocations count is null");
		return allocationsCount > 0 || (fldFixedErrorCodes.GetValue(comp, null) as HashSet<string>).Count > 0;
	}
}

public class WhosOnFirstSusadminReflector : SusadminReflector {
	private Component comp;
	private PropertyInfo fldCurrentStage;
	public WhosOnFirstSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		comp = module.GetComponent("WhosOnFirstComponent");
		fldCurrentStage = comp.GetType().GetProperty("CurrentStage", BindingFlags.Public | BindingFlags.Instance);
	}
	protected override bool Exec() { return (fldCurrentStage.GetValue(comp, null) as int?) > 0; }
}

public class MinesweeperSusadminReflector : SusadminReflector {
	private Component comp;
	private FieldInfo fldStartFound;
	public MinesweeperSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		comp = module.GetComponent("MinesweeperModule");
		fldStartFound = GetFieldImpl(comp.GetType(), "StartFound");
	}
	protected override bool Exec() { return (fldStartFound.GetValue(comp) as bool?) == true; }
}

public class SimonStatesSusadminReflector : SusadminReflector {
	private Component comp;
	private FieldInfo fldProgress;
	public SimonStatesSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		comp = module.GetComponent("AdvancedSimon");
		fldProgress = GetFieldImpl(comp.GetType(), "Progress");
	}
	protected override bool Exec() {
		int? progress = fldProgress.GetValue(comp) as int?;
		if (progress == null) throw new Exception("progress is null");
		return progress > 0;
	}
}

public class TwoBitsSusadminReflector : SusadminReflector {
	private Component comp;
	private FieldInfo fldCurrentState;
	public TwoBitsSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		comp = module.GetComponent("TwoBitsModule");
		fldCurrentState = GetFieldImpl(comp.GetType(), "currentState");
	}
	protected override bool Exec() {
		object state = fldCurrentState.GetValue(comp);
		if (state == null) throw new Exception("state is null");
		ulong? stateInt = Convert.ChangeType(state, typeof(ulong)) as ulong?;
		if (stateInt == null) throw new Exception("unable to convert state to int");
		return stateInt > 1;
	}
}

public class LogicGatesSusadminReflector : SusadminReflector {
	private int startInputIndex = -1;
	private Component comp;
	private FieldInfo fldCurrentInputIndex;
	public LogicGatesSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		comp = module.GetComponent("LogicGates");
		fldCurrentInputIndex = GetFieldImpl(comp.GetType(), "_currentInputIndex");
		int? startIndex = fldCurrentInputIndex.GetValue(comp) as int?;
		if (startIndex == null) throw new Exception("start input index is null");
		startInputIndex = startIndex.Value;
	}
	protected override bool Exec() {
		if (startInputIndex == -1) return false;
		int? index = fldCurrentInputIndex.GetValue(comp) as int?;
		if (index == null) throw new Exception("current input index is null");
		return index != startInputIndex;
	}
}

public class SeaShellsSusadminReflector : SusadminReflector {
	private Component comp;
	private FieldInfo fldStage;
	public SeaShellsSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		comp = module.GetComponent("SeaShellsModule");
		fldStage = GetFieldImpl(comp.GetType(), "stage");
	}
	protected override bool Exec() {
		int? stage = fldStage.GetValue(comp) as int?;
		if (stage == null) throw new Exception("stage is null");
		return stage > 0;
	}
}

public class ColoredSwitchesSusadminReflector : SusadminReflector {
	private Component comp;
	private FieldInfo fldNumInitialToggles;
	public ColoredSwitchesSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		comp = module.GetComponent("ColoredSwitchesModule");
		fldNumInitialToggles = GetFieldImpl(comp.GetType(), "_numInitialToggles");
	}
	protected override bool Exec() {
		int? initialTogglesCount = fldNumInitialToggles.GetValue(comp) as int?;
		if (initialTogglesCount == null) throw new Exception("initial toggles count is null");
		return initialTogglesCount > 0;
	}
}

public class DoubleOhSusadminReflector : SusadminReflector {
	private Component comp;
	private FieldInfo fldCurPos;
	private int startPos;
	public DoubleOhSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		comp = module.GetComponent("DoubleOhModule");
		fldCurPos = GetFieldImpl(comp.GetType(), "_curPos");
		int? startPos = fldCurPos.GetValue(comp) as int?;
		if (startPos == null) throw new Exception("start position is null");
		this.startPos = startPos.Value;
	}
	protected override bool Exec() {
		int? curPos = fldCurPos.GetValue(comp) as int?;
		if (curPos == null) throw new Exception("curent position is null");
		return curPos != startPos;
	}
}

public class NotMorseCodeSusadminReflector : SusadminReflector {
	private Component comp;
	private FieldInfo fldChannelIndex;
	public NotMorseCodeSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		comp = module.GetComponent("NotMorseCode");
		fldChannelIndex = GetFieldImpl(comp.GetType(), "channelIndex");
	}
	protected override bool Exec() {
		int? channelIndex = fldChannelIndex.GetValue(comp) as int?;
		if (channelIndex == null) throw new Exception("channel index is null");
		return channelIndex > 0;
	}
}

public class ColoredSquaresSusadminReflector : SusadminReflector {
	private string componentName;
	private Component comp;
	private FieldInfo fldColors;
	public ColoredSquaresSusadminReflector(KMBombModule module, string componentName) : base(module.ModuleType) {
		comp = module.GetComponent(componentName);
		fldColors = GetFieldImpl(comp.GetType(), "_colors");
		this.componentName = componentName;
	}
	protected override bool Exec() {
		int[] colors = fldColors.GetValue(comp) as int[];
		if (colors == null) throw new Exception("colors are null");
		Debug.LogFormat(string.Format("<SUSadmin> {0} colors are: {1}", componentName, colors.Select(i => i.ToString()).Join(",")));
		return colors.Any(i => i == 1);
	}
}

public class ThreeDTunnelsSusadminReflector : SusadminReflector {
	private Component comp;
	private FieldInfo fldLocation;
	private int startLocation;
	public ThreeDTunnelsSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		comp = module.GetComponent("ThreeDTunnels");
		fldLocation = GetFieldImpl(comp.GetType(), "_location");
		int? startLocation = fldLocation.GetValue(comp) as int?;
		if (startLocation == null) throw new Exception("start location is null");
		this.startLocation = startLocation.Value;
	}
	protected override bool Exec() {
		int? location = fldLocation.GetValue(comp) as int?;
		if (location == null) throw new Exception("location is null");
		return location != startLocation;
	}
}

public class ThreeDMazeSusadminReflector : SusadminReflector {
	private object map;
	private FieldInfo fldPlayerX;
	private FieldInfo fldPlayerY;
	private int startPlayerX;
	private int startPlayerY;
	public ThreeDMazeSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		Component comp = module.GetComponent("ThreeDMazeModule");
		map = GetFieldImpl(comp.GetType(), "map").GetValue(comp);
		if (map == null) throw new Exception("map is null");
		Type mapType = map.GetType();
		fldPlayerX = GetFieldImpl(mapType, "pl_x");
		fldPlayerY = GetFieldImpl(mapType, "pl_y");
		int? startPlayerX = fldPlayerX.GetValue(map) as int?;
		if (startPlayerX == null) throw new Exception("start player x is null");
		this.startPlayerX = startPlayerX.Value;
		int? startPlayerY = fldPlayerY.GetValue(map) as int?;
		if (startPlayerY == null) throw new Exception("start player y is null");
		this.startPlayerY = startPlayerY.Value;
	}
	protected override bool Exec() {
		int? playerX = fldPlayerX.GetValue(map) as int?;
		if (playerX == null) throw new Exception("start player x is null");
		int? playerY = fldPlayerY.GetValue(map) as int?;
		if (playerY == null) throw new Exception("start player y is null");
		return playerX != startPlayerX || playerY != startPlayerY;
	}
}

public class ThirdBaseSusadminReflector : SusadminReflector {
	private Component comp;
	private FieldInfo fldStage;
	public ThirdBaseSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		comp = module.GetComponent("ThirdBaseModule");
		fldStage = GetFieldImpl(comp.GetType(), "stage");
	}
	protected override bool Exec() {
		int? stage = fldStage.GetValue(comp) as int?;
		if (stage == null) throw new Exception("stage is null");
		return stage > 0;
	}
}

public class NonverbalSimonSusadminReflector : SusadminReflector {
	private Component comp;
	private FieldInfo fldStagesCompleted;
	public NonverbalSimonSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		comp = module.GetComponent("NonverbalSimonHandler");
		fldStagesCompleted = GetFieldImpl(comp.GetType(), "stagesCompleted");
	}
	protected override bool Exec() {
		int? stagesCompleted = fldStagesCompleted.GetValue(comp) as int?;
		if (stagesCompleted == null) throw new Exception("current pos is null");
		return stagesCompleted > 0;
	}
}
