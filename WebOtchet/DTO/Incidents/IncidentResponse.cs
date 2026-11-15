using WebOtchet.Models;

namespace WebOtchet.DTO.Incidents;

public sealed record IncidentResponse(
    Guid Id,
    string Pw,
    string Direction,
    string TechCenter,
    byte Category,
    string NetworkType,
    string ServiceElement,
    string NumberOfBaseStations,
    string EquipmentType,
    string AlarmType,
    string Description,
    DateTime BeginTime,
    DateTime? EndTime,
    TimeSpan? Akb,
    TimeSpan? GeneratorTime,
    string Length,
    TimeSpan? BaseStationTime,
    TimeSpan? CommonStationTime,
    string AlarmWho,
    string NoticeDescription,
    Guid? CreatedByUserId,
    Guid Version)
{
    public static IncidentResponse FromEntity(Incident incident) => new(
        incident.Id,
        incident.Pw,
        incident.Direction,
        incident.TechCenter,
        incident.Category,
        incident.NetworkType,
        incident.ServiceElement,
        incident.NumberOfBaseStations,
        incident.EquipmentType,
        incident.AlarmType,
        incident.Description,
        incident.BeginTime,
        incident.EndTime,
        incident.Akb,
        incident.GeneratorTime,
        incident.Length,
        incident.BaseStationTime,
        incident.CommonStationTime,
        incident.AlarmWho,
        incident.NoticeDescription,
        incident.CreatedByUserId,
        incident.Version
    );
}
