namespace PhotoPrint.API.Models;

/// <summary>
/// Which storage tier currently holds an upload's bytes (bolt 043 — two-tier model).
/// Set to <see cref="Local"/> on upload; flipped to <see cref="Cloud"/> by the
/// intent-024 promotion job after a paid order's photos are written to cloud and
/// confirmed.
/// </summary>
public enum StorageLocation
{
    Local = 0,
    Cloud = 1,
}
