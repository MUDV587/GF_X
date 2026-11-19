using UnityEngine;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using GameFramework.Fsm;
using System.Globalization;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class LaunchProcedure : ProcedureBase
{
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        this.InitSettings();
        ChangeState(procedureOwner, GF_X.Base.EditorResourceMode ? typeof(LoadHotfixDllProcedure) : typeof(UpdateResourcesProcedure));
    }

    private void InitSettings()
    {
        CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

        GF_X.Debugger.ActiveWindow = AppSettings.Instance.DebugMode;
        GF_X.Debugger.WindowScale = 1.4f;
    }
}
