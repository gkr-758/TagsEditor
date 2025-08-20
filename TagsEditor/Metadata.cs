using System;

namespace TagsEditor
{
    public class Metadata
    {
        public string Artist { get; set; } = "";
        public string RomanisedArtist { get; set; } = "";
        public string Title { get; set; } = "";
        public string RomanisedTitle { get; set; } = "";
        public string Creator { get; set; } = "";
        public string Source { get; set; } = "";
        public string Tags { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string BGFile { get; set; } = "";
        public decimal BGPos { get; set; } = 0;

        // Video関連
        public string VideoFileName { get; set; } = "";
        public decimal VideoStartTime { get; set; } = 0;
        public decimal VideoXOffset { get; set; } = 0;
        public decimal VideoYOffset { get; set; } = 0;

        public Metadata Clone()
        {
            return (Metadata)MemberwiseClone();
        }

        public bool IsSame(Metadata other)
        {
            if (other == null) return false;
            return Artist == other.Artist &&
                   RomanisedArtist == other.RomanisedArtist &&
                   Title == other.Title &&
                   RomanisedTitle == other.RomanisedTitle &&
                   Creator == other.Creator &&
                   Source == other.Source &&
                   Tags == other.Tags &&
                   Difficulty == other.Difficulty &&
                   BGFile == other.BGFile &&
                   BGPos == other.BGPos &&
                   VideoFileName == other.VideoFileName &&
                   VideoStartTime == other.VideoStartTime &&
                   VideoXOffset == other.VideoXOffset &&
                   VideoYOffset == other.VideoYOffset;
        }
    }
}