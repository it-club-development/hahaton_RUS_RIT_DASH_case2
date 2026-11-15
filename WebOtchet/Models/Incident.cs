namespace WebOtchet.Models;

public class Incident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid Version { get; set; } = Guid.NewGuid();
    public string Pw { get; set; } = "";
    public string Direction { get; set; } = "";
    public string TechCenter { get; set; } = "";
    public byte Category { get; set; }
    public string NetworkType { get; set; } = "";
    public string ServiceElement { get; set; } = "";
    public string NumberOfBaseStations { get; set; } = "";
    public string EquipmentType { get; set; } = "";
    public string AlarmType { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime BeginTime { get; set; }
    private DateTime? _endTime;
    public DateTime? EndTime
    {
        get => _endTime;
        set => _endTime = value == DateTime.MinValue ? null : value;
    }
    public TimeSpan? Akb { get; set; }
    public TimeSpan? GeneratorTime { get; set; }
    public string Length { get; set; } = "";
    public TimeSpan? BaseStationTime { get; set; }
    public TimeSpan? CommonStationTime { get; set; }
    public string AlarmWho { get; set; } = "";
    public string NoticeDescription { get; set; } = "";

    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
}
