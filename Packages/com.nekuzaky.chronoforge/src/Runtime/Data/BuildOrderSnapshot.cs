using System;

namespace Chronoforge
{
    /// <summary>
    /// An immutable, timestamped copy of the build order's serialized state. Used for the
    /// history panel and planned-vs-actual comparison. <see cref="m_Json"/> holds a full
    /// serializer payload so a snapshot is restorable on its own.
    /// </summary>
    [Serializable]
    public sealed class BuildOrderSnapshot
    {
        public string m_Id = "";
        public string m_Label = "";
        public string m_TimestampUtc = "";
        public string m_Author = "";
        public string m_Json = "";
    }
}
