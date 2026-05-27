using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PhotoPrint.API.Hubs;

[Authorize(Roles = "Admin")]
public class AdminOrderHub : Hub { }
