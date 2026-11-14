
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebOtchet.Models;

namespace WebOtchet.Data.Configurations;

public sealed class IncidentConfiguration
    : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("INCIDENTS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("ID")
            .HasColumnType("char(36)")
            .HasConversion<string>()
            .ValueGeneratedNever();

        builder.Property(x => x.Pw)
            .HasColumnName("PW")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.Direction)
            .HasColumnName("NAPR")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.TechCenter)
            .HasColumnName("TEHC")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasColumnName("KATEG")
            .HasColumnType("tinyint unsigned")
            .IsRequired();

        builder.Property(x => x.NetworkType)
            .HasColumnName("NETW_TYPE")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.ServiceElement)
            .HasColumnName("SERV_ELEM")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.NumberOfBaseStations)
            .HasColumnName("NUMB_RBS")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.EquipmentType)
            .HasColumnName("EQIP_TYPE")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.AlarmType)
            .HasColumnName("ALARM_TYPE")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("DESCR")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.BeginTime)
            .HasColumnName("BEGIN_TIME")
            .HasColumnType("datetime")
            .IsRequired();

        builder.Property(x => x.EndTime)
            .HasColumnName("END_TIME")
            .HasColumnType("datetime")
            .IsRequired(false);

        builder.Property(x => x.Akb)
            .HasColumnName("AKB")
            .HasColumnType("time")
            .IsRequired(false);

        builder.Property(x => x.GeneratorTime)
            .HasColumnName("DGU")
            .HasColumnType("time")
            .IsRequired(false);

        builder.Property(x => x.Length)
            .HasColumnName("LENGHT")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.BaseStationTime)
            .HasColumnName("PROSTOY")
            .HasColumnType("time")
            .IsRequired(false);

        builder.Property(x => x.CommonStationTime)
            .HasColumnName("COMMON_PROSTOY")
            .HasColumnType("time")
            .IsRequired(false);

        builder.Property(x => x.AlarmWho)
            .HasColumnName("ALARM_WHO")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.NoticeDescription)
            .HasColumnName("DESC_NOTICE")
            .HasMaxLength(450)
            .IsRequired();

        builder.HasIndex(x => x.BeginTime)
            .HasDatabaseName("IX_INCIDENTS_BEGIN_TIME");
    }
}

