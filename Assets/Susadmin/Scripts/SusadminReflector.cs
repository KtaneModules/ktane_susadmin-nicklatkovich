using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public abstract class SusadminReflector {
	public static SusadminReflector CreateReflector(KMBombModule module) {
		switch (module.ModuleType) {
			case "sysadmin": return new SysadminSusadminReflector(module);
			case "WhosOnFirst": return new WhosOnFirstSusadminReflector(module);
			case "MinesweeperModule": return new MinesweeperSusadminReflector(module);
			case "SimonV2": return new SimonStatesSusadminReflector(module);
			case "TwoBits": return new TwoBitsSusadminReflector(module);
			case "logicGates": return new LogicGatesSusadminReflector(module);
			case "SeaShells": return new SeaShellsSusadminReflector(module);
			case "ColoredSwitchesModule": return new ColoredSwitchesSusadminReflector(module);
			default: return null;
		}
	}
	private bool exception = false;
	private bool strike = false;
	private string moduleId;
	protected SusadminReflector(string moduleId) { this.moduleId = moduleId; }
	protected abstract bool Exec();
	public bool ShouldStrike() {
		if (strike) return true;
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
		fldAllocationsCount = type.GetField("allocationsCount", BindingFlags.Instance);
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
		fldStartFound = comp.GetType().GetField("StartFound", BindingFlags.Instance);
	}
	protected override bool Exec() { return (fldStartFound.GetValue(comp) as bool?) == true; }
}

public class SimonStatesSusadminReflector : SusadminReflector {
	private Component comp;
	private FieldInfo fldProgress;
	public SimonStatesSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		comp = module.GetComponent("AdvancedSimon");
		fldProgress = comp.GetType().GetField("Progress", BindingFlags.Instance);
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
		fldCurrentState = comp.GetType().GetField("currentState", BindingFlags.Instance);
	}
	protected override bool Exec() {
		object state = fldCurrentState.GetValue(comp);
		if (state == null) throw new Exception("state is null");
		ulong? stateInt = Convert.ChangeType(state, typeof(ulong)) as ulong?;
		if (stateInt == null) throw new Exception("unable to convert state to int");
		return stateInt > 2;
	}
}

public class LogicGatesSusadminReflector : SusadminReflector {
	private int startInputIndex = -1;
	private Component comp;
	private FieldInfo fldCurrentInputIndex;
	public LogicGatesSusadminReflector(KMBombModule module) : base(module.ModuleType) {
		comp = module.GetComponent("LogicGates");
		fldCurrentInputIndex = comp.GetType().GetField("_currentInputIndex", BindingFlags.Instance);
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
		fldStage = comp.GetType().GetField("stage", BindingFlags.Instance);
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
		fldNumInitialToggles = comp.GetType().GetField("_numInitialToggles", BindingFlags.Instance);
	}
	protected override bool Exec() {
		int? initialTogglesCount = fldNumInitialToggles.GetValue(comp) as int?;
		if (initialTogglesCount == null) throw new Exception("initial toggles count is null");
		return initialTogglesCount > 0;
	}
}
