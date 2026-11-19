namespace UGF.EditorTools
{
    /// <summary>语言 Excel 行数据模型（框架内通用）。</summary>
    public class LocalizationText
    {
        public string Key;
        public string Value;
        public bool Locked;//true表示禁止移除和修改Key值
    }
}
