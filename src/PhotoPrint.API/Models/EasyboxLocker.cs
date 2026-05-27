namespace PhotoPrint.API.Models;

public class EasyboxLocker
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SamedayId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string City { get; set; } = null!;
    public string County { get; set; } = null!;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public bool IsActive { get; set; } = true;
}
