using System.Runtime.CompilerServices;

// 共享工具基类/AI 任务运行时在工具程序集与聚合程序集间共享：
// 不把 internal 放大成 public，UI API 仍可自由演变。
[assembly: InternalsVisibleTo("GF.Build.Editor")]
[assembly: InternalsVisibleTo("GF.DataTable.Editor")]
[assembly: InternalsVisibleTo("GF.GameData.Editor")]
[assembly: InternalsVisibleTo("GF.Localization.Editor")]
[assembly: InternalsVisibleTo("GF.LanguageExcel.Editor")]
[assembly: InternalsVisibleTo("GF.Compression.Editor")]
[assembly: InternalsVisibleTo("GF.ImageFont.Editor")]
[assembly: InternalsVisibleTo("GF_X.Editor")]
[assembly: InternalsVisibleTo("GF_X.EditModeTests")]
