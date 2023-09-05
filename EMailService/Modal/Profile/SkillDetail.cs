using System;

namespace ModalLayer.Modal.Profile
{
    public class SkillDetail
    {
        public int SkillIndex { get; set; }
        public string Language { get; set; }
        public int Version { get; set; }
        public Nullable<DateTime> LastUsed { get; set; }
        public int ExperienceInYear { get; set; }
        public int ExperienceInMonth { get; set; }
    }
}
