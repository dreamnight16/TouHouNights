$sourcePath = 'Assets/Scripts/UI/BattleUiRoot.cs'
$source = Get-Content -LiteralPath $sourcePath -Raw
$source = $source.Replace('public sealed class BattleUiRoot', 'public sealed partial class BattleUiRoot')
$buildStart = $source.IndexOf('        private void Build()')
$buildEnd = $source.IndexOf('        private void Update()')
$views = $source.Substring($buildStart, $buildEnd - $buildStart)
$source = $source.Remove($buildStart, $buildEnd - $buildStart)
$helperStart = $source.IndexOf('        private Image Panel(')
$helperEnd = $source.LastIndexOf('    }')
$helpers = $source.Substring($helperStart, $helperEnd - $helperStart)
$source = $source.Remove($helperStart, $helperEnd - $helperStart)
$imports = "using UnityEngine;`nusing UnityEngine.UI;`nusing TowerDefense.Core;`nusing TowerDefense.Data;`n`nnamespace TowerDefense.UI`n{`n    public sealed partial class BattleUiRoot`n    {`n"
Set-Content -LiteralPath $sourcePath -Value $source
Set-Content -LiteralPath 'Assets/Scripts/UI/BattleUiRoot.Views.cs' -Value ($imports + $views + "    }`n}")
Set-Content -LiteralPath 'Assets/Scripts/UI/BattleUiRoot.Primitives.cs' -Value ($imports + $helpers + "    }`n}")
