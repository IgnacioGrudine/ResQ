namespace ResQ.API.Models.Enums;

public enum OrderStatus : byte
{
    Pending   = 1,
    Paid      = 2,
    PickedUp  = 3,
    Cancelled = 4
}
