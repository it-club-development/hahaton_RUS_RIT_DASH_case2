using System.ComponentModel.DataAnnotations;
using WebOtchet.Models;

namespace WebOtchet.DTO.Incidents;

public sealed class IncidentWriteRequest
{
    [Required, MaxLength(450)]
    public string Pw { get; init; } = "";

    [Required, MaxLength(450)]
    public string Direction { get; init; } = "";

    [Required, MaxLength(450)]
    public string TechCenter { get; init; } = "";

    public byte Category { get; init; }

    [Required, MaxLength(450)]
    public string NetworkType { get; init; } = "";

    [MaxLength(450)]
    public string ServiceElement { get; init; } = "";

    [Required, MaxLength(20)]
    public string NumberOfBaseStations { get; init; } = "";

    [MaxLength(450)]
    public string EquipmentType { get; init; } = "";

    [Required, MaxLength(450)]
    public string AlarmType { get; init; } = "";

    [MaxLength(450)]
    public string Description { get; init; } = "";

    public DateTime BeginTime { get; init; }

    public DateTime? EndTime { get; init; }

    public TimeSpan? Akb { get; init; }

    public TimeSpan? GeneratorTime { get; init; }

    public TimeSpan? BaseStationTime { get; init; }

    [MaxLength(450)]
    public string AlarmWho { get; init; } = "";

    [MaxLength(450)]
    public string NoticeDescription { get; init; } = "";

    public Incident ToEntity(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Pw = Pw,
        Direction = Direction,
        TechCenter = TechCenter,
        Category = Category,
        NetworkType = NetworkType,
        ServiceElement = ServiceElement,
        NumberOfBaseStations = NumberOfBaseStations,
        EquipmentType = EquipmentType,
        AlarmType = AlarmType,
        Description = Description,
        BeginTime = BeginTime,
        EndTime = EndTime,
        Akb = Akb,
        GeneratorTime = GeneratorTime,
        BaseStationTime = BaseStationTime,
        AlarmWho = AlarmWho,
        NoticeDescription = NoticeDescription
    };
}
