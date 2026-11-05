using System.Text.Json.Serialization;

namespace HOI4Builder.Core.Projects;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorldMode
{
    StandardMod,
    BlankSlateOverhaul,
    ResetContent,
    KeepCountriesAndBorders,
    RandomizedPoliticalWorld,
}