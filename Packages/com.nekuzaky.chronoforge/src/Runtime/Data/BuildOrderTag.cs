using System;

namespace Chronoforge
{
    /// <summary>Reusable label applied to steps for filtering and grouping.</summary>
    [Serializable]
    public sealed class BuildOrderTag
    {
        public string m_Id = "";
        public string m_Label = "";
        public string m_ColorHex = "#8C9BAB";
    }
}
