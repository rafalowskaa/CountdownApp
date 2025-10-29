using System;

namespace CountdownApp
{
    internal class AppSettings
    {
        public DateTime? TargetDate { get; set; }

        public string? MediaPath { get; set; }
        public string? MediaCaption { get; set; }
        public string? EventIcon { get; set; }
    }
}