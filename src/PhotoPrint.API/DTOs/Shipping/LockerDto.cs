namespace PhotoPrint.API.DTOs.Shipping;

public record LockerDto(
    Guid Id,
    string SamedayId,
    string Name,
    string Address,
    string City,
    double Lat,
    double Lng);
